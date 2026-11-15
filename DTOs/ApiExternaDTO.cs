using System.ComponentModel.DataAnnotations;

namespace BiblioLink.DTOs;

public class ApiExternaCreateDto
{
    [Required(ErrorMessage = "El nombre del sistema es obligatorio.")]
    [StringLength(100, MinimumLength = 2)]
    public string NombreSistema { get; set; } = string.Empty;

    [Required(ErrorMessage = "La URL base es obligatoria.")]
    [Url(ErrorMessage = "La URL base no es válida (ej.: http://localhost:5111/api).")]
    [StringLength(300)]
    public string UrlBase { get; set; } = string.Empty;

    [Required(ErrorMessage = "La API Key es obligatoria.")]
    [StringLength(200, MinimumLength = 1)]
    public string ApiKey { get; set; } = string.Empty;
}

public class ApiExternaUpdateDto
{
    [Required(ErrorMessage = "El nombre del sistema es obligatorio.")]
    [StringLength(100, MinimumLength = 2)]
    public string NombreSistema { get; set; } = string.Empty;

    [Required(ErrorMessage = "La URL base es obligatoria.")]
    [Url(ErrorMessage = "La URL base no es válida (ej.: http://localhost:5111/api).")]
    [StringLength(300)]
    public string UrlBase { get; set; } = string.Empty;

    // Opcional: como la API Key nunca se devuelve, si no se envía se conserva la actual.
    [StringLength(200)]
    public string? ApiKey { get; set; }
}

public class ApiExternaReadDto
{
    public int Id { get; set; }
    public string NombreSistema { get; set; } = string.Empty;
    public string UrlBase { get; set; } = string.Empty;

    //la API Key nunca se devuelve, solo se informa si hay una cargada
    public bool ApiKeyConfigurada { get; set; }
}