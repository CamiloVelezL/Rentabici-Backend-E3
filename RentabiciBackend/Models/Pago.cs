namespace RentabiciBackend.Models;

public class Pago
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int? AlquilerId { get; set; }
    public string CodigoFactura { get; set; } = string.Empty;
    public string Concepto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Metodo { get; set; } = string.Empty;
    public string Referencia { get; set; } = string.Empty;
    public bool Aprobado { get; set; }
    public string MensajePasarela { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
