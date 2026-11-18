using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RentabiciBackend.Data;
using RentabiciBackend.Dtos;
using RentabiciBackend.Models;
using RentabiciBackend.Security;
using RentabiciBackend.Services;

namespace RentabiciBackend.Controllers;

/// <summary>Épica 4 – Incidencias y mantenimiento (componente IncidenceController del C4).</summary>
[Route("api/incidencias")]
[Autorizar]
public class IncidenceController : ApiControllerBase
{
    private readonly IReportadorIncidencias _reportes;     // ISP: lo que usa cualquier usuario
    private readonly IGestorMantenimiento _mantenimiento;  // ISP: lo que usa el técnico

    public IncidenceController(IReportadorIncidencias reportes, IGestorMantenimiento mantenimiento)
    {
        _reportes = reportes;
        _mantenimiento = mantenimiento;
    }

    /// <summary>Pantalla "Reporte de fallas" (los 3 roles).</summary>
    [HttpPost]
    public IActionResult Reportar([FromBody] IncidenciaRequest r)
    {
        var (incidencia, mensaje) = _reportes.Reportar(UsuarioActual, r);
        return StatusCode(201, new Respuesta<Incidencia>(mensaje, incidencia));
    }

    /// <summary>Pantalla "Fallas reportadas" (?estado=Pendiente).</summary>
    [Autorizar(Rol.Tecnico, Rol.Administrador)]
    [HttpGet]
    public IActionResult Listar([FromQuery] EstadoIncidencia? estado) =>
        Ok(new Respuesta<IEnumerable<Incidencia>>("OK", _mantenimiento.Listar(estado)));

    /// <summary>RF-09: marcar la falla como "Solucionado".</summary>
    [Autorizar(Rol.Tecnico, Rol.Administrador)]
    [HttpPost("{id:int}/atender")]
    public IActionResult Atender(int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AtenderIncidenciaRequest? r)
    {
        var resultado = _mantenimiento.Atender(UsuarioActual, id, r ?? new AtenderIncidenciaRequest());
        var mensaje = resultado.BicicletaHabilitada
            ? $"Falla solucionada. La bicicleta {resultado.Incidencia.BicicletaId:D4} volvió a estar disponible."
            : "Falla solucionada.";
        return Ok(new Respuesta<AtencionResultado>(mensaje, resultado));
    }

    /// <summary>RF-09: la bicicleta no tiene reparación → "Dar de baja" (estado final).</summary>
    [Autorizar(Rol.Tecnico, Rol.Administrador)]
    [HttpPost("{id:int}/baja")]
    public IActionResult DarDeBaja(int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AtenderIncidenciaRequest? r)
    {
        var resultado = _mantenimiento.DarDeBaja(UsuarioActual, id, r ?? new AtenderIncidenciaRequest());
        return Ok(new Respuesta<AtencionResultado>(
            $"La bicicleta {resultado.Incidencia.BicicletaId:D4} fue dada de baja.", resultado));
    }
}

/// <summary>Notificaciones simuladas del usuario (evidencia del Factory Method).</summary>
[Route("api/notificaciones")]
[Autorizar]
public class NotificationController : ApiControllerBase
{
    private readonly MemoryDatabase _db;
    public NotificationController(MemoryDatabase db) => _db = db;

    [HttpGet]
    public IActionResult Mias()
    {
        lock (_db.Lock)
            return Ok(new Respuesta<IEnumerable<Notificacion>>("OK",
                _db.Notificaciones.Where(n => n.UsuarioId == UsuarioActual.Id).OrderByDescending(n => n.Fecha).Take(30).ToList()));
    }


    if (string.IsNullOrWhiteSpace(descripcion)) 
    return BadRequest("La descripción no puede estar vacía");


}
