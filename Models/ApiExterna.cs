namespace BiblioLink.Models;

public class ApiExterna
{
    public int Id {get; set;}
    public string NombreSistema {get; set;} = string.Empty;
    public string UrlBase {get; set;} = string.Empty;
    public string ApiKey {get; set;} = string.Empty;
}