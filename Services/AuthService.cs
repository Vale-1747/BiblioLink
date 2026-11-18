using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BiblioLink.DTOs;
using BiblioLink.Exceptions;
using BiblioLink.Interfaces;
using BiblioLink.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BiblioLink.Services;

public class AuthService : IAuthService
{
    private readonly BibliotecaContext _db;
    private readonly IPasswordHasher<Cuenta> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(
        BibliotecaContext db,
        IPasswordHasher<Cuenta> passwordHasher,
        IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<AuthResponseDTO> RegisterAsync(RegistroCuentaDTO dto)
    {
        var username = NormalizarUsername(dto.Username);
        var adminConfigurado = _configuration["Auth:BootstrapAdmin:Username"];
        if (adminConfigurado is not null &&
            username == NormalizarUsername(adminConfigurado))
        {
            throw new ReglaNegocioException("Ese nombre de usuario está reservado.");
        }

        if (await _db.Cuentas.AnyAsync(c => c.Username == username))
            throw new ReglaNegocioException("El nombre de usuario ya está registrado.");

        var cuenta = new Cuenta
        {
            Username = username,
            Rol = Roles.Usuario
        };
        cuenta.PasswordHash = _passwordHasher.HashPassword(cuenta, dto.Password);

        _db.Cuentas.Add(cuenta);
        await _db.SaveChangesAsync();

        return CrearToken(cuenta);
    }

    public async Task<AuthResponseDTO?> LoginAsync(LoginDTO dto)
    {
        var username = NormalizarUsername(dto.Username);
        var cuenta = await _db.Cuentas.SingleOrDefaultAsync(c => c.Username == username);
        if (cuenta is null)
            return null;

        var resultado = _passwordHasher.VerifyHashedPassword(cuenta, cuenta.PasswordHash, dto.Password);
        if (resultado == PasswordVerificationResult.Failed)
            return null;

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            cuenta.PasswordHash = _passwordHasher.HashPassword(cuenta, dto.Password);
            await _db.SaveChangesAsync();
        }

        return CrearToken(cuenta);
    }

    private AuthResponseDTO CrearToken(Cuenta cuenta)
    {
        var issuer = _configuration["Auth:Jwt:Issuer"] ?? "BiblioLink";
        var audience = _configuration["Auth:Jwt:Audience"] ?? "BiblioLink";
        var key = _configuration["Auth:Jwt:Key"]
            ?? throw new InvalidOperationException("Falta configurar Auth:Jwt:Key.");
        var minutos = int.TryParse(_configuration["Auth:Jwt:TokenMinutes"], out var valor) && valor > 0
            ? valor
            : 60;
        var expiracion = DateTime.UtcNow.AddMinutes(minutos);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, cuenta.Id.ToString()),
            new Claim(ClaimTypes.Name, cuenta.Username),
            new Claim(ClaimTypes.Role, cuenta.Rol)
        };
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expiracion, signingCredentials: credentials);

        return new AuthResponseDTO
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiracion,
            Username = cuenta.Username,
            Role = cuenta.Rol
        };
    }

    private static string NormalizarUsername(string username) => username.Trim().ToUpperInvariant();
}
