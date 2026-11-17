namespace RentabiciBackend.Models;
public class Incidencia
{
    public int Id { get; set; }
    public int BicicletaId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Gravedad { get; set; } = string.Empty;
}