using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BiblioLink.Filters;

public class ApiKeyAttribute : Attribute, IActionFilter
{
    private const string NombreHeader = "X-API-Key";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        //la configuracion se resuelve desde el contenedor de dependencias
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var keyEsperada = config["ApiExterna:ApiKey"];

        var tieneHeader = context.HttpContext.Request.Headers.TryGetValue(NombreHeader, out var keyRecibida);

        //si no hay clave condigurada se rechaza todo (segudo por defecto)
        if(!tieneHeader || string.IsNullOrEmpty(keyEsperada) || keyRecibida.ToString() != keyEsperada)
        {
            context.Result = new UnauthorizedObjectResult(new { mensaje = "API Key faltante o invalida"}); //401
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) {}
}