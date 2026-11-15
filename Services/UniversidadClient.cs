using System.Net;
using System.Text.Json;
using BiblioLink.Exceptions;
using BiblioLink.Interfaces;
using Microsoft.Extensions.Options;

namespace BiblioLink.Services;

/*
cliente HTTP hacia AcademiaCentral: GET {UrlBase}/alumnos/{matricula}/estado
devuelve true/false si la universidad responde, y lanza ServicioExternoException
(502, 503 o 504) cuando falla la comunicación, para distinguirla de un error interno (500).
*/
public class UniversidadClient : IUniversidadClient
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly UniversidadApiOptions _opts;
    private readonly ILogger<UniversidadClient> _logger;

    public UniversidadClient(HttpClient http, IOptions<UniversidadApiOptions> opts, ILogger<UniversidadClient> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task<bool> EsElegibleAsync(string matricula, CancellationToken ct = default)
    {
        var url = $"{_opts.UrlBase.TrimEnd('/')}/alumnos/{Uri.EscapeDataString(matricula)}/estado";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-API-Key", _opts.ApiKey);
        request.Headers.Accept.ParseAdd("application/json");

        //timeout propio: si vence, cancelamos el pedido. se combina con el token de quien nos llama.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_opts.TimeoutSegundos));

        try
        {
            using var response = await _http.SendAsync(request, timeoutCts.Token);
            var cuerpo = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            return Interpretar(response.StatusCode, cuerpo, matricula);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            //se cancelo por nuestro timeout, no porque el cliente cerro la conexion
            _logger.LogError("La API de la universidad no respondió en {Segundos} segundos", _opts.TimeoutSegundos);
            throw new ServicioExternoException(
                "La API de la universidad tardó demasiado en responder.",
                StatusCodes.Status504GatewayTimeout);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo conectar con la API de la universidad");
            throw new ServicioExternoException(
                "No se pudo conectar con la API de la universidad.",
                StatusCodes.Status503ServiceUnavailable, ex);
        }
    }

    //traduce la respuesta de academia segun el contrato
    private bool Interpretar(HttpStatusCode status, string cuerpo, string matricula)
    {
        //200 -> { "matricula": "...", "esActivo": true|false }
        if ((int)status is >= 200 and < 300)
        {
            var estado = Deserializar<EstadoAlumnoRespuesta>(cuerpo);
            if (estado?.EsActivo is null)
            {
                _logger.LogError("La respuesta de la universidad no tiene el formato esperado");
                throw new ServicioExternoException(
                    "La API de la universidad devolvió una respuesta que no se puede interpretar.",
                    StatusCodes.Status502BadGateway);
            }
            return estado.EsActivo.Value;
        }

        //404 con campo "mensaje" -> la matrucula no existe -> no elegible.
        //un 404 SIN "mensaje" (ej.: UrlBase mal escrita) no significa eso: es una falla.
        if (status == HttpStatusCode.NotFound && Deserializar<MensajeRespuesta>(cuerpo)?.Mensaje is not null)
        {
            _logger.LogInformation("La universidad indica que la matrícula {Matricula} no existe", matricula);
            return false;
        }

        //401, 403, 5xx, 404 sin mensaje...
        _logger.LogError("La API de la universidad respondió con el estado {Estado}", (int)status);
        throw new ServicioExternoException(
            $"La API de la universidad respondió con un error ({(int)status}).",
            StatusCodes.Status502BadGateway);
    }

    private static T? Deserializar<T>(string json) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(json, JsonOpts); }
        catch (JsonException) { return null; }
    }

    //formas de las respuestas de academia (solo transportan datos)
    private sealed class EstadoAlumnoRespuesta { public bool? EsActivo { get; set; } }
    private sealed class MensajeRespuesta { public string? Mensaje { get; set; } }
}