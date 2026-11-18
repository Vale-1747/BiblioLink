using BiblioLink.DTOs;
using BiblioLink.Exceptions;
using BiblioLink.Interfaces;
using BiblioLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiblioLink.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/configuracion-api-externa")]
public class ConfiguracionApiExternaController : ControllerBase
{
    private readonly IApiExternaService _servicio;

    public ConfiguracionApiExternaController(IApiExternaService servicio)
    {
        _servicio = servicio;
    }

    //GET /api/configuracion-api-externa
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiExternaReadDto>>> GetTodos()
        => Ok(await _servicio.GetTodosAsync());

    //GET /api/configuracion-api-externa/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPorId(int id)
    {
        try { return Ok(await _servicio.GetPorIdAsync(id)); }
        catch (NotFoundException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound); }
    }

    //POST /api/configuracion-api-externa
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] ApiExternaCreateDto dto)
    {
        try
        {
            var creada = await _servicio.CrearAsync(dto);
            return CreatedAtAction(nameof(GetPorId), new { id = creada.Id }, creada);
        }
        catch (ReglaNegocioException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict); }
    }

    //PUT /api/configuracion-api-externa/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ApiExternaUpdateDto dto)
    {
        try { return Ok(await _servicio.ActualizarAsync(id, dto)); }
        catch (NotFoundException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound); }
        catch (ReglaNegocioException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict); }
    }

    //DELETE /api/configuracion-api-externa/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await _servicio.EliminarAsync(id);
            return NoContent();
        }
        catch (NotFoundException ex) { return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound); }
    }
}