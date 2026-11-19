using BiblioLink.DTOs;
using BiblioLink.Exceptions;
using BiblioLink.Interfaces;
using BiblioLink.Models;
using BiblioLink.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BiblioLink.Services;

public class PrestamoService : IPrestamoService
{
    private readonly BibliotecaContext _db;
    private readonly IUniversidadClient _universidad;
    private readonly PrestamosOptions _opts;
    private readonly ILogger<PrestamoService> _logger;

    public PrestamoService(
        BibliotecaContext db,
        IUniversidadClient universidad,
        IOptions<PrestamosOptions> opts,
        ILogger<PrestamoService> logger)
    {
        _db = db;
        _universidad = universidad;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task<PrestamoReadDto> CrearAsync(CreatePrestamoDto dto, CancellationToken ct = default)
    {
        // 2. El libro existe
        var libro = await _db.Libros.AsNoTracking().FirstOrDefaultAsync(l => l.Id == dto.LibroId, ct)
            ?? throw new NotFoundException($"No existe el libro con id {dto.LibroId}.");

        // 3. Hay stock (chequeo barato; el definitivo es atómico, en el paso 8)
        if (libro.CantidadDisponible <= 0)
        {
            _logger.LogWarning("Préstamo denegado: sin stock del libro {LibroId}", libro.Id);
            throw new ReglaNegocioException("No hay ejemplares disponibles de este libro.");
        }

        // 4. El usuario existe
        var usuario = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == dto.UsuarioId, ct)
            ?? throw new NotFoundException($"No existe el usuario con id {dto.UsuarioId}.");

        await SincronizarVencidosAsync(ct);

        // 5. Desafío: mora (tiene algún préstamo Vencido)
        var tieneMora = await _db.Prestamos.AnyAsync(p =>
            p.UsuarioId == usuario.Id && p.Estado == EstadoPrestamo.Vencido, ct);
        if (tieneMora)
        {
            _logger.LogWarning("Préstamo denegado: el usuario {UsuarioId} tiene préstamos en mora", usuario.Id);
            throw new NoElegibleException("El usuario tiene libros vencidos sin devolver.");
        }

        // 6. Límite de préstamos activos (todo lo que no está Devuelto)
        var activos = await _db.Prestamos.CountAsync(p =>
            p.UsuarioId == usuario.Id && p.Estado != EstadoPrestamo.Devuelto, ct);
        if (activos >= _opts.LimitePrestamosActivos)
        {
            _logger.LogWarning("Préstamo denegado: el usuario {UsuarioId} alcanzó el límite de {Limite}",
                usuario.Id, _opts.LimitePrestamosActivos);
            throw new NoElegibleException(
                $"El usuario alcanzó el límite de {_opts.LimitePrestamosActivos} préstamos activos.");
        }

        // 7. API externa (la más cara, va al final de las validaciones)
        bool elegible;
        if (EsPersonal(usuario))
        {
            elegible = true;
        }
        else
        {
            try
            {
                elegible = await _universidad.EsElegibleAsync(usuario.Matricula, ct);
            }
            catch (ServicioExternoException ex)
            {
                _logger.LogError(ex, "Falló la API externa al verificar la matrícula {Matricula}", usuario.Matricula);
                throw;
            }
        }
        if (!elegible)
        {
            _logger.LogWarning("Préstamo denegado: matrícula {Matricula} no elegible según la universidad", usuario.Matricula);
            throw new NoElegibleException("La universidad indica que el usuario no está activo/vigente.");
        }

        // 8. Descontar stock de forma atómica y crear el préstamo, todo en una transacción
        var ahora = DateTime.UtcNow;
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var filas = await _db.Libros
            .Where(l => l.Id == libro.Id && l.CantidadDisponible > 0)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.CantidadDisponible, l => l.CantidadDisponible - 1), ct);

        if (filas == 0)
        {
            _logger.LogWarning("Préstamo denegado: sin stock del libro {LibroId} (concurrencia)", libro.Id);
            throw new ReglaNegocioException("No hay ejemplares disponibles de este libro.");
        }

        var prestamo = new Prestamo
        {
            LibroId = libro.Id,
            UsuarioId = usuario.Id,
            FechaPrestamo = ahora,
            FechaDevolucionPrevista = ahora.AddDays(_opts.DiasPrestamo),
            Estado = EstadoPrestamo.Vigente
        };
        _db.Prestamos.Add(prestamo);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation("Préstamo {PrestamoId} creado: libro {LibroId}, usuario {UsuarioId}",
            prestamo.Id, libro.Id, usuario.Id);

        prestamo.Libro = libro;
        prestamo.Usuario = usuario;
        return ToDto(prestamo);
    }

    public async Task<PrestamoReadDto> DevolverAsync(int prestamoId, CancellationToken ct = default)
    {
        var libroId = await _db.Prestamos
            .Where(p => p.Id == prestamoId)
            .Select(p => (int?)p.LibroId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"No existe el préstamo con id {prestamoId}.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var ahora = DateTime.UtcNow;
        var filas = await _db.Prestamos
            .Where(p => p.Id == prestamoId && p.Estado != EstadoPrestamo.Devuelto)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Estado, EstadoPrestamo.Devuelto)
                .SetProperty(p => p.FechaDevolucionReal, ahora), ct);

        if (filas == 0)
            throw new ReglaNegocioException("El préstamo ya fue devuelto.");

        await _db.Libros
            .Where(l => l.Id == libroId && l.CantidadDisponible < l.CantidadTotal)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.CantidadDisponible, l => l.CantidadDisponible + 1), ct);

        await tx.CommitAsync(ct);

        _logger.LogInformation("Préstamo {PrestamoId} devuelto (libro {LibroId})", prestamoId, libroId);

        var prestamo = await _db.Prestamos
            .Include(p => p.Libro).Include(p => p.Usuario)
            .AsNoTracking()
            .FirstAsync(p => p.Id == prestamoId, ct);
        return ToDto(prestamo);
    }

    public async Task<IEnumerable<PrestamoReadDto>> ListarPorUsuarioAsync(int usuarioId, bool soloActivos, CancellationToken ct = default)
    {
        if (!await _db.Usuarios.AnyAsync(u => u.Id == usuarioId, ct))
            throw new NotFoundException($"No existe el usuario con id {usuarioId}.");

        await SincronizarVencidosAsync(ct);

        var query = _db.Prestamos
            .Include(p => p.Libro).Include(p => p.Usuario)
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId);

        if (soloActivos)
            query = query.Where(p => p.Estado != EstadoPrestamo.Devuelto);

        var lista = await query.OrderByDescending(p => p.FechaPrestamo).ToListAsync(ct);
        return lista.Select(ToDto);
    }

    public async Task<IEnumerable<PrestamoReadDto>> HistorialPorLibroAsync(int libroId, CancellationToken ct = default)
    {
        if (!await _db.Libros.AnyAsync(l => l.Id == libroId, ct))
            throw new NotFoundException($"No existe el libro con id {libroId}.");

        await SincronizarVencidosAsync(ct);

        var lista = await _db.Prestamos
            .Include(p => p.Libro).Include(p => p.Usuario)
            .AsNoTracking()
            .Where(p => p.LibroId == libroId)
            .OrderByDescending(p => p.FechaPrestamo)
            .ToListAsync(ct);
        return lista.Select(ToDto);
    }

    // Método de Santi (endpoint para Academia Central). Ahora cuenta Vigente y Vencido.
    public async Task<IEnumerable<PrestamoActivoDTO>> ListarActivosPorMatriculaAsync(string matricula, CancellationToken ct = default)
    {
        await SincronizarVencidosAsync(ct);

        var matriculaLimpia = matricula.Trim();

        var activos = await _db.Prestamos
            .AsNoTracking()
            .Include(p => p.Libro)
            .Where(p => p.Usuario.Matricula == matriculaLimpia && p.Estado != EstadoPrestamo.Devuelto)
            .OrderBy(p => p.FechaDevolucionPrevista)
            .ToListAsync(ct);

        return activos.Select(p => new PrestamoActivoDTO
        {
            PrestamoId = p.Id,
            LibroTitulo = p.Libro.Titulo,
            Estado = p.Estado.ToString()
        }).ToList();
    }

    // Marca como Vencido todo préstamo Vigente cuya fecha prevista ya pasó.
    private async Task SincronizarVencidosAsync(CancellationToken ct)
    {
        var ahora = DateTime.UtcNow;
        var cantidad = await _db.Prestamos
            .Where(p => p.Estado == EstadoPrestamo.Vigente && p.FechaDevolucionPrevista < ahora)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, EstadoPrestamo.Vencido), ct);

        if (cantidad > 0)
            _logger.LogInformation("{Cantidad} préstamos marcados como Vencido", cantidad);
    }

    private static PrestamoReadDto ToDto(Prestamo p) => new()
    {
        Id = p.Id,
        LibroId = p.LibroId,
        TituloLibro = p.Libro.Titulo,
        UsuarioId = p.UsuarioId,
        MatriculaUsuario = p.Usuario.Matricula,
        FechaPrestamo = p.FechaPrestamo,
        FechaDevolucionPrevista = p.FechaDevolucionPrevista,
        FechaDevolucionReal = p.FechaDevolucionReal,
        Estado = p.Estado.ToString()
    };

    private static bool EsPersonal(Usuario usuario) => string.Equals(usuario.Tipo, "Personal", StringComparison.OrdinalIgnoreCase);
}