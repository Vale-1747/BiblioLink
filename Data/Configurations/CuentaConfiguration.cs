using BiblioLink.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BiblioLink.Data.Configurations;

public class CuentaConfiguration : IEntityTypeConfiguration<Cuenta>
{
    public void Configure(EntityTypeBuilder<Cuenta> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Username)
            .IsUnique();

        builder.Property(c => c.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.Rol)
            .IsRequired()
            .HasMaxLength(30);
    }
}
