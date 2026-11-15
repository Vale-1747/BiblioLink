namespace BiblioLink.Services;

//parametros tecnicos de la comunicacion con AcademiaCentral
//la URL base y la API Key no están acá, se guardan en la tabla ApiExterna
public class UniversidadApiOptions
{
    public const string Seccion = "UniversidadApi";

    public int TimeoutSegundos { get; set; } = 5;
}