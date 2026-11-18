using Microsoft.AspNetCore.Mvc;
using RentabiciBackend.Dtos;
using RentabiciBackend.Models;
using RentabiciBackend.Security;
using RentabiciBackend.Services;

namespace RentabiciBackend.Controllers;

/// <summary>Épica 5 – Pagos y multas (componente PaymentController del C4).</summary>
[Route("api/pagos")]
[Autorizar]
public class PaymentController : ApiControllerBase
{
    private readonly IPagoService _pagos;
    public PaymentController(IPagoService pagos) => _pagos = pagos;

    /// <summary>Tabla de "Pagos y multas" con el código de factura a pagar.</summary>
    [HttpGet("pendientes")]
    public IActionResult Pendientes() => Ok(new Respuesta<PendientesResponse>("OK", _pagos.Pendientes(UsuarioActual)));

    /// <summary>Pantalla de pago: paga la factura con su código (RB-0000).</summary>
    [HttpPost]
    public IActionResult Pagar([FromBody] PagoRequest r)
    {
        var resultado = _pagos.PagarFactura(UsuarioActual, r.CodigoFactura);
        return Ok(new Respuesta<PagoResultado>(
            $"Factura {resultado.CodigoFactura} pagada por {resultado.TotalPagado:N0} COP. Comprobante: {resultado.Comprobante}.", resultado));
    }

    [HttpGet("historial")]
    public IActionResult Historial() => Ok(new Respuesta<IEnumerable<Pago>>("OK", _pagos.Historial(UsuarioActual.Id)));
}
