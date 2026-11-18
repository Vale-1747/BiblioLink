using BiblioLink.DTOs;
using BiblioLink.Exceptions;
using BiblioLink.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiblioLink.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("registro")]
    public async Task<ActionResult<AuthResponseDTO>> Registrar(RegistroCuentaDTO dto)
    {
        try
        {
            return Ok(await _authService.RegisterAsync(dto));
        }
        catch (ReglaNegocioException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDTO>> Login(LoginDTO dto)
    {
        var respuesta = await _authService.LoginAsync(dto);
        return respuesta is null
            ? Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." })
            : Ok(respuesta);
    }
}
