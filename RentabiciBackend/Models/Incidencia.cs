namespace RentabiciBackend.Models;

public class Incidencia
{
    public int Id { get; set; }
    public int BicicletaId { get; set; }
    public int UsuarioId { get; set; }
    public int? EstacionId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public GravedadIncidencia Gravedad { get; set; }
    public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Pendiente;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public EstadoBicicleta EstadoResultante { get; set; }
}
