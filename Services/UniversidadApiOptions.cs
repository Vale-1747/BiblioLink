namespace BiblioLink.Services;

//configuracion para hablar con AcademiaCentral
public class UniversidadApiOptions
{
    public const string Seccion = "UniversidadApi";

    public string UrlBase { get; set; } = string.Empty;   // ej.: http://localhost:5111/api
    public string ApiKey { get; set; } = string.Empty;    // simulada; nunca se loguea
    public int TimeoutSegundos { get; set; } = 5;
}