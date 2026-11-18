using RentabiciBackend.Models;
using RentabiciBackend.Services.Notificaciones;

namespace RentabiciBackend.Services.Pagos;

public record ResultadoRecaudo(bool Confirmado, string Referencia, string Mensaje);

/// <summary>
/// Subsistema externo de recaudo: el punto de pago (banco, corresponsal, Efecty/PSE) donde
/// el usuario paga la factura con su código. ELEMENTO PROYECTADO: en el MVP se simula.
/// </summary>
public interface ISistemaRecaudo
{
    ResultadoRecaudo RegistrarPago(string codigoFactura, decimal monto);
}

/// <summary>Recaudo simulado: confirma el pago y devuelve un número de comprobante.</summary>
public class RecaudoSimulado : ISistemaRecaudo
{
    public ResultadoRecaudo RegistrarPago(string codigoFactura, decimal monto)
    {
        var referencia = $"COMP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        return new ResultadoRecaudo(true, referencia, $"Pago de la factura {codigoFactura} por {monto:N0} COP confirmado.");
    }
}

/// <summary>
/// PATRÓN FACADE (GoF, estructural): una sola operación ConfirmarPago() oculta el sistema
/// de recaudo externo, el manejo de su caída y el envío del comprobante (RF-12).
/// </summary>
public interface IPaymentFacade
{
    ResultadoRecaudo ConfirmarPago(Usuario usuario, Factura factura);
}

public class PaymentFacade : IPaymentFacade
{
    private readonly ISistemaRecaudo _recaudo;
    private readonly INotificacionService _notificaciones;
    private readonly ILogger<PaymentFacade> _logger;

    public PaymentFacade(ISistemaRecaudo recaudo, INotificacionService notificaciones, ILogger<PaymentFacade> logger)
    {
        _recaudo = recaudo;
        _notificaciones = notificaciones;
        _logger = logger;
    }

    public ResultadoRecaudo ConfirmarPago(Usuario usuario, Factura factura)
    {
        ResultadoRecaudo resultado;
        try
        {
            resultado = _recaudo.RegistrarPago(factura.Codigo, factura.Total);
        }
        catch (Exception ex)
        {
            // Flujo de excepción: el sistema de recaudo no responde. No se cobra y la API no se cae.
            _logger.LogWarning(ex, "Sistema de recaudo no disponible");
            return new ResultadoRecaudo(false, "SIN-REFERENCIA", "El sistema de pagos no está disponible. Intente más tarde.");
        }

        if (resultado.Confirmado)
        {
            // RF-12: comprobante digital (el PDF por correo es ELEMENTO PROYECTADO; aquí se envía el texto).
            var detalle = new List<string> { $"Factura: {factura.Codigo}" };
            if (factura.MontoAlquileres > 0) detalle.Add($"Alquiler: {factura.MontoAlquileres:N0} COP");
            if (factura.MontoRecargos > 0) detalle.Add($"Recargo por demora: {factura.MontoRecargos:N0} COP");
            if (factura.MontoMulta > 0) detalle.Add($"Multa: {factura.MontoMulta:N0} COP");
            detalle.Add($"Total pagado: {factura.Total:N0} COP");
            detalle.Add($"Comprobante: {resultado.Referencia}");
            _notificaciones.Notificar(usuario, "Comprobante de pago", string.Join("\n", detalle));
        }

        return resultado;
    }
}
