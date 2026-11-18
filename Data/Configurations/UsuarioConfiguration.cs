using BiblioLink.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BiblioLink.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.HasKey(u => u.Id);

        // Índice único sobre Matrícula
        builder.HasIndex(u => u.Matricula)
               .IsUnique();

        builder.Property(u => u.Matricula)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(u => u.Nombre)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.Apellido)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.Tipo)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(150);
    }
}