namespace RentabiciBackend.Models;


public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool TieneMulta { get; set; }
}