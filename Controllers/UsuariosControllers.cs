using BiblioLink.DTOs;
using BiblioLink.Exceptions;
using BiblioLink.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BiblioLink.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioReadDTO>>> GetAll()
    {
        var usuarios = await _usuarioService.GetAllAsync();
        return Ok(usuarios);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioReadDTO>> GetById(int id)
    {
        try
        {
            var usuario = await _usuarioService.GetByIdAsync(id);
            if (usuario == null) return NotFound();
            return Ok(usuario);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioReadDTO>> Create([FromBody] UsuarioCreateDTO usuarioDto)
    {
        try
        {
            var nuevoUsuario = await _usuarioService.CreateAsync(usuarioDto);
            return CreatedAtAction(nameof(GetById), new { id = nuevoUsuario.Id }, nuevoUsuario);
        }
        catch (ReglaNegocioException ex)
        {
            return Conflict(new { mensaje = ex.Message }); // 409 Conflict
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioReadDTO>> Update(int id, [FromBody] UsuarioUpdateDTO usuarioDto)
    {
        try
        {
            var actualizado = await _usuarioService.UpdateAsync(id, usuarioDto);
            return Ok(actualizado);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message }); // 404 Not Found
        }
        catch (ReglaNegocioException ex)
        {
            return Conflict(new { mensaje = ex.Message }); // 409 Conflict
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _usuarioService.DeleteAsync(id);
            return NoContent(); // 204 No Content
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ReglaNegocioException ex)
        {
            return Conflict(new { mensaje = ex.Message }); // 409 Conflict
        }
    }
}