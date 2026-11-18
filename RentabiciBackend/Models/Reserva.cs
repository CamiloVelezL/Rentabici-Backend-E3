namespace RentabiciBackend.Models;

public class Reserva
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int BicicletaId { get; set; }
    public int EstacionId { get; set; }
    public string Modelo { get; set; } = string.Empty;
    public string FechaUso { get; set; } = string.Empty;   // dd/MM/yyyy
    public string Telefono { get; set; } = string.Empty;
    public int TiempoMinutos { get; set; }
    public decimal CostoEstimado { get; set; }
    public DateTime Creada { get; set; } = DateTime.UtcNow;
    public DateTime Expira { get; set; }
    public EstadoReserva Estado { get; set; } = EstadoReserva.Activa;
}
