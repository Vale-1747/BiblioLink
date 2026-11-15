using BiblioLink.DTOs;
using BiblioLink.Exceptions;
using BiblioLink.Interfaces;
using BiblioLink.Models;
using Microsoft.EntityFrameworkCore;

namespace BiblioLink.Services;

public class ApiExternaService : IApiExternaService
{
    private readonly BibliotecaContext _db;
    private readonly ILogger<ApiExternaService> _logger;

    public ApiExternaService(BibliotecaContext db, ILogger<ApiExternaService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<ApiExternaReadDto>> GetTodosAsync()
    {
        var lista = await _db.ApiExternas.AsNoTracking().OrderBy(a => a.Id).ToListAsync();
        return lista.Select(Mapear).ToList();
    }

    public async Task<ApiExternaReadDto> GetPorIdAsync(int id)
    {
        var api = await _db.ApiExternas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new NotFoundException($"No se encontró la configuración con ID {id}.");

        return Mapear(api);
    }

    public async Task<ApiExternaReadDto> CrearAsync(ApiExternaCreateDto dto)
    {
        var nombre = dto.NombreSistema.Trim();

        if (await _db.ApiExternas.AnyAsync(a => a.NombreSistema == nombre))
            throw new ReglaNegocioException($"Ya existe una configuración llamada \"{nombre}\".");

        var api = new ApiExterna
        {
            NombreSistema = nombre,
            UrlBase = dto.UrlBase.Trim(),
            ApiKey = dto.ApiKey
        };

        _db.ApiExternas.Add(api);
        await _db.SaveChangesAsync();

        // Nunca se loguea la ApiKey
        _logger.LogInformation("Configuración de API externa creada {ApiExternaId} ({Nombre})", api.Id, api.NombreSistema);
        return Mapear(api);
    }

    public async Task<ApiExternaReadDto> ActualizarAsync(int id, ApiExternaUpdateDto dto)
    {
        var api = await _db.ApiExternas.FindAsync(id)
            ?? throw new NotFoundException($"No se encontró la configuración con ID {id}.");

        var nombre = dto.NombreSistema.Trim();

        if (await _db.ApiExternas.AnyAsync(a => a.NombreSistema == nombre && a.Id != id))
            throw new ReglaNegocioException($"Ya existe otra configuración llamada \"{nombre}\".");

        api.NombreSistema = nombre;
        api.UrlBase = dto.UrlBase.Trim();

        // La key no se devuelve nunca, así que si no viene se conserva la que ya estaba
        if (!string.IsNullOrWhiteSpace(dto.ApiKey))
            api.ApiKey = dto.ApiKey;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Configuración de API externa actualizada {ApiExternaId}", api.Id);
        return Mapear(api);
    }

    public async Task EliminarAsync(int id)
    {
        var api = await _db.ApiExternas.FindAsync(id)
            ?? throw new NotFoundException($"No se encontró la configuración con ID {id}.");

        _db.ApiExternas.Remove(api);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Configuración de API externa eliminada {ApiExternaId}", id);
    }

    private static ApiExternaReadDto Mapear(ApiExterna a) => new()
    {
        Id = a.Id,
        NombreSistema = a.NombreSistema,
        UrlBase = a.UrlBase,
        ApiKeyConfigurada = !string.IsNullOrEmpty(a.ApiKey)
    };
}