namespace BiblioLink.Exceptions;

public class ServicioExternoException : Exception
{
    public TipoFallaServicioExterno TipoFalla { get; }

    public ServicioExternoException(string mensaje, TipoFallaServicioExterno tipoFalla, Exception? inner = null)
        : base(mensaje, inner)
    {
        TipoFalla = tipoFalla;
    }
}

public enum TipoFallaServicioExterno
{
    NoDisponible,
    Timeout,
    RespuestaInvalida
}