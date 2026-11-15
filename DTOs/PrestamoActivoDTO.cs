namespace BiblioLink.DTOs;

//formato acordado con AcademiaCentral
public class PrestamoActivoDTO
{
    public int PrestamoId { get; set; }
    public string LibroTitulo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;   //"Vigente" o "Vencido"
}