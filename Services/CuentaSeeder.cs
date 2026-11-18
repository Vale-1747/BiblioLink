using BiblioLink.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BiblioLink.Services;

public static class CuentaSeeder
{
    public static async Task CrearAdminInicialAsync(IServiceProvider services, IConfiguration configuration)
    {
        var usernameConfigurado = configuration["Auth:BootstrapAdmin:Username"];
        var passwordConfigurado = configuration["Auth:BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(usernameConfigurado) && string.IsNullOrWhiteSpace(passwordConfigurado))
            return;
        if (string.IsNullOrWhiteSpace(usernameConfigurado) || string.IsNullOrWhiteSpace(passwordConfigurado))
            throw new InvalidOperationException("Debe configurar tanto el usuario como la contraseña de BootstrapAdmin.");
        if (usernameConfigurado.Trim().Length is < 3 or > 100)
            throw new InvalidOperationException("El usuario de BootstrapAdmin debe tener entre 3 y 100 caracteres.");
        if (passwordConfigurado.Length is < 12 or > 100)
            throw new InvalidOperationException("La contraseña de BootstrapAdmin debe tener entre 12 y 100 caracteres.");

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BibliotecaContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Cuenta>>();
        var username = usernameConfigurado.Trim().ToUpperInvariant();
        var cuenta = await db.Cuentas.SingleOrDefaultAsync(c => c.Username == username);
        if (cuenta is not null)
        {
            if (cuenta.Rol != Roles.Admin)
                throw new InvalidOperationException("El usuario configurado para BootstrapAdmin ya existe con otro rol.");
            return;
        }

        cuenta = new Cuenta
        {
            Username = username,
            Rol = Roles.Admin
        };
        cuenta.PasswordHash = hasher.HashPassword(cuenta, passwordConfigurado);
        db.Cuentas.Add(cuenta);
        await db.SaveChangesAsync();
    }
}
