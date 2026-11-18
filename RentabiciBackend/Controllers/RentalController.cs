using Microsoft.AspNetCore.Mvc;
using RentabiciBackend.Dtos;
using RentabiciBackend.Models;
using RentabiciBackend.Security;
using RentabiciBackend.Services;

namespace RentabiciBackend.Controllers;

/// <summary>Épica 3 – Reserva, recibir y entregar bicicleta (componente RentalController del C4).</summary>
[Route("api")]
[Autorizar]
public class RentalController : ApiControllerBase
{
    private readonly IRentalService _alquiler;          // resuelto como RentalServiceProxy
    private readonly IAlquilerConsultaService _consultas;
    private readonly IReservaService _reservas;

    public RentalController(IRentalService alquiler, IAlquilerConsultaService consultas, IReservaService reservas)
    {
        _alquiler = alquiler;
        _consultas = consultas;
        _reservas = reservas;
    }

    // ---------- Reservas (RF-05) ----------

    /// <summary>Pantallas "Alquiler de bicicleta" paso 1 y 2.</summary>
    [HttpPost("reservas")]
    public IActionResult Reservar([FromBody] ReservaRequest r) =>
        StatusCode(201, new Respuesta<Reserva>("¡Reserva creada!", _reservas.Reservar(UsuarioActual, r)));

    [HttpGet("reservas/activa")]
    public IActionResult ReservaActiva() =>
        Ok(new Respuesta<Reserva?>("OK", _reservas.ObtenerActiva(UsuarioActual.Id)));

    [HttpDelete("reservas/{id:int}")]
    public IActionResult CancelarReserva(int id) =>
        Ok(new Respuesta<Reserva>("Reserva cancelada.", _reservas.Cancelar(UsuarioActual, id)));

    // ---------- Alquileres (RF-06, RF-07) ----------

    /// <summary>Pantalla "Recibir bicicleta" (QR).</summary>
    [HttpPost("alquileres")]
    public IActionResult Iniciar([FromBody] IniciarAlquilerRequest r)
    {
        try
        {
            var alquiler = _alquiler.IniciarAlquiler(UsuarioActual.Id, r.BicicletaId);
            return StatusCode(201, new Respuesta<Alquiler>("Bicicleta recibida. ¡Buen viaje!", alquiler));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    /// <summary>Botón "Entregar" de "Administrar reservas".</summary>
    [HttpPost("alquileres/{id:int}/devolucion")]
    public IActionResult Devolver(int id, [FromBody] DevolverRequest r)
    {
        var alquiler = _alquiler.Devolver(UsuarioActual.Id, id, r.EstacionDestinoId, r.MinutosSimulados);
        return Ok(new Respuesta<Alquiler>($"Bicicleta entregada. Costo del viaje: {alquiler.Costo:N0} COP.", alquiler));
    }

    [HttpGet("alquileres/activo")]
    public IActionResult Activo() => Ok(new Respuesta<Alquiler?>("OK", _consultas.Activo(UsuarioActual.Id)));

    [HttpGet("alquileres/historial")]
    public IActionResult Historial() => Ok(new Respuesta<IEnumerable<Alquiler>>("OK", _consultas.Historial(UsuarioActual.Id)));
}
