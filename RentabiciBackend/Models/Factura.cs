namespace RentabiciBackend.Models;

/// <summary>
/// Factura que agrupa lo que el usuario debe (alquileres sin pagar y multa).
/// El usuario la paga ingresando su código (p. ej. RB-0001) en "Pagos y multas".
/// Cada usuario tiene como máximo una factura Pendiente, que se actualiza si aparecen nuevos cobros.
/// </summary>
public class Factura
{
    public int Id { get; set; }

    /// <summary>Código que el usuario ingresa para pagar (RB-0001).</summary>
    public string Codigo { get; set; } = string.Empty;

    public int UsuarioId { get; set; }
    public List<int> AlquilerIds { get; set; } = new();
    public bool IncluyeMulta { get; set; }
    public decimal MontoAlquileres { get; set; }
    public decimal MontoRecargos { get; set; }
    public decimal MontoMulta { get; set; }
    public decimal Total => MontoAlquileres + MontoRecargos + MontoMulta;
    public EstadoFactura Estado { get; set; } = EstadoFactura.Pendiente;
    public DateTime Creada { get; set; } = DateTime.UtcNow;
    public DateTime? FechaPago { get; set; }
}
