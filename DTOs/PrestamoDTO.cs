using System.ComponentModel.DataAnnotations;


namespace BiblioLink.DTOs;

public class CreatePrestamoDto
{
    [Required, Range(1, int.MaxValue)]
    public int LibroId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int UsuarioId { get; set; }
}

public class PrestamoReadDto
{
    public int Id { get; set; }
    public int LibroId { get; set; }
    public string TituloLibro { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string MatriculaUsuario { get; set; } = string.Empty;
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaDevolucionPrevista { get; set; }
    public DateTime? FechaDevolucionReal { get; set; }
    public string Estado { get; set; } = string.Empty;
}
public class PrestamoActivoDTO
{
    public int PrestamoId { get; set; }
    public string LibroTitulo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}