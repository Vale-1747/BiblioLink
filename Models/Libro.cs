namespace BiblioLink.Models;

public class Libro
{
    private int Id {get; set;}
    private string Titulo {get; set;}
    private string Autor {get; set;}
    private string ISBN {get; set;}
    private string Editorial {get; set;}
    private int AnoDePublicacion {get; set;}
    private string UbicacionFisica {get; set;}
    private int CantidadTotal {get; set;}
    private int CantidadDisponible {get; set;}

    public Libro(int id, string titulo, string autor, string isbn, string editorial, int anoDePublicacion, string ubicacionFisica, int cantidadTotal, int cantidadDisponible)
    {
        Id = id;
        Titulo = titulo;
        Autor = autor;
        ISBN = isbn;
        Editorial = editorial;
        AnoDePublicacion = anoDePublicacion;
        UbicacionFisica = ubicacionFisica;
        CantidadTotal = cantidadTotal;
        CantidadDisponible = cantidadDisponible;
    }
}