using BiblioLink.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BiblioLink.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
                throw;

            var (status, title, detail) = Traducir(exception);
            _logger.LogError(exception, "Error procesando {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;
            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                problem,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                context.RequestAborted);
        }
    }

    private static (int Status, string Title, string Detail) Traducir(Exception exception) => exception switch
    {
        NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado", exception.Message),
        ReglaNegocioException => (StatusCodes.Status409Conflict, "Conflicto", exception.Message),
        NoElegibleException => (StatusCodes.Status403Forbidden, "Operación no permitida", exception.Message),
        ServicioExternoException externo => (TraducirFallaExterna(externo.TipoFalla), "Error del servicio externo", externo.Message),
        _ => (StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado.")
    };

    private static int TraducirFallaExterna(TipoFallaServicioExterno tipoFalla) => tipoFalla switch
    {
        TipoFallaServicioExterno.NoDisponible => StatusCodes.Status503ServiceUnavailable,
        TipoFallaServicioExterno.Timeout => StatusCodes.Status504GatewayTimeout,
        TipoFallaServicioExterno.RespuestaInvalida => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status502BadGateway
    };
}
