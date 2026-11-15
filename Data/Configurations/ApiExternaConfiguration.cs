using BiblioLink.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BiblioLink.Data;

public class ApiExternaConfiguration : IEntityTypeConfiguration<ApiExterna>
{
    public void Configure(EntityTypeBuilder<ApiExterna> builder)
    {
        builder.Property(a => a.NombreSistema).IsRequired().HasMaxLength(100);
        builder.Property(a => a.UrlBase).IsRequired().HasMaxLength(300);
        builder.Property(a => a.ApiKey).IsRequired().HasMaxLength(200);
        builder.HasIndex(a => a.NombreSistema).IsUnique(); //no puede haber dos configuraciones con el mismo nombre
    }
}