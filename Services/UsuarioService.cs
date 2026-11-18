using BiblioLink.Data;
using BiblioLink.DTOs;
using BiblioLink.Exceptions;
using BiblioLink.Models;
using BiblioLink.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BiblioLink.Services;

public class UsuarioService : IUsuarioService
{
    private readonly BibliotecaContext _context;

    public UsuarioService(BibliotecaContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UsuarioReadDTO>> GetAllAsync()
    {
        return await _context.Usuarios
            .Select(u => new UsuarioReadDTO
            {
                Id = u.Id,
                Matricula = u.Matricula,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Tipo = u.Tipo,
                Email = u.Email
            })
            .ToListAsync();
    }

    public async Task<UsuarioReadDTO?> GetByIdAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return null;

        return new UsuarioReadDTO
        {
            Id = usuario.Id,
            Matricula = usuario.Matricula,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Tipo = usuario.Tipo,
            Email = usuario.Email
        };
    }

    public async Task<UsuarioReadDTO> CreateAsync(UsuarioCreateDTO dto)
    {
        // Regla: Matrícula duplicada da ReglaNegocioException (409)
        var existeMatricula = await _context.Usuarios.AnyAsync(u => u.Matricula == dto.Matricula);
        if (existeMatricula)
        {
            throw new ReglaNegocioException($"Ya existe un usuario registrado con la matrícula '{dto.Matricula}'.");
        }

        var usuario = new Usuario
        {
            Matricula = dto.Matricula,
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Tipo = dto.Tipo,
            Email = dto.Email
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return new UsuarioReadDTO
        {
            Id = usuario.Id,
            Matricula = usuario.Matricula,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Tipo = usuario.Tipo,
            Email = usuario.Email
        };
    }

    public async Task<UsuarioReadDTO?> UpdateAsync(int id, UsuarioUpdateDTO dto)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            throw new NotFoundException($"No se encontró el usuario con ID {id}.");
        }

        // Verificar si la matrícula nueva ya pertenece a otro usuario
        var matriculaOcupada = await _context.Usuarios.AnyAsync(u => u.Matricula == dto.Matricula && u.Id != id);
        if (matriculaOcupada)
        {
            throw new ReglaNegocioException($"La matrícula '{dto.Matricula}' ya está en uso por otro usuario.");
        }

        usuario.Matricula = dto.Matricula;
        usuario.Nombre = dto.Nombre;
        usuario.Apellido = dto.Apellido;
        usuario.Tipo = dto.Tipo;
        usuario.Email = dto.Email;

        await _context.SaveChangesAsync();

        return new UsuarioReadDTO
        {
            Id = usuario.Id,
            Matricula = usuario.Matricula,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Tipo = usuario.Tipo,
            Email = usuario.Email
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            throw new NotFoundException($"No se encontró el usuario con ID {id}.");
        }

        // Regla: No se borra un usuario con préstamos (409)
        var tienePrestamos = await _context.Prestamos.AnyAsync(p => p.UsuarioId == id);
        if (tienePrestamos)
        {
            throw new ReglaNegocioException("No se puede eliminar el usuario porque tiene préstamos asociados.");
        }

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return true;
    }
}