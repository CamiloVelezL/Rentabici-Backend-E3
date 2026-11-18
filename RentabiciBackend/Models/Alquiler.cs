namespace RentabiciBackend.Models;

public class Alquiler
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int BicicletaId { get; set; }
    public int EstacionOrigenId { get; set; }
    public int? EstacionDestinoId { get; set; }
    public int? ReservaId { get; set; }
    public DateTime Inicio { get; set; } = DateTime.UtcNow;
    public DateTime? Fin { get; set; }

    /// <summary>Tiempo elegido al reservar; se cobra como mínimo ese tiempo.</summary>
    public int TiempoReservado { get; set; }
    public int? MinutosUso { get; set; }
    public decimal? MontoAlquiler { get; set; }
    public decimal? MontoRecargo { get; set; }
    public decimal? Costo { get; set; }
    public string? TarifaAplicada { get; set; }
    public EstadoAlquiler Estado { get; set; } = EstadoAlquiler.Activo;
}
