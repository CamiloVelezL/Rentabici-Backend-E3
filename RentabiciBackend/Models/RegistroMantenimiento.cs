namespace RentabiciBackend.Models;

public class RegistroMantenimiento
{
    public int Id { get; set; }
    public int BicicletaId { get; set; }
    public int IncidenciaId { get; set; }
    public int TecnicoId { get; set; }
    public string Diagnostico { get; set; } = string.Empty;
    public string? PiezasReemplazadas { get; set; }
    public bool Reparable { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public EstadoBicicleta EstadoResultante { get; set; }
}
