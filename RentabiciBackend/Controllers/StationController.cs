using Microsoft.AspNetCore.Mvc;
using RentabiciBackend.Dtos;
using RentabiciBackend.Models;
using RentabiciBackend.Security;
using RentabiciBackend.Services;

namespace RentabiciBackend.Controllers;

/// <summary>Épica 2 – Estaciones y flota (componente StationController del C4).</summary>
[Route("api")]
[Autorizar]
public class StationController : ApiControllerBase
{
    private readonly IEstacionService _estaciones;
    private readonly IBicicletaService _bicicletas;

    public StationController(IEstacionService estaciones, IBicicletaService bicicletas)
    {
        _estaciones = estaciones;
        _bicicletas = bicicletas;
    }

    // ---------- Estaciones ----------

    /// <summary>RU-02: estaciones con disponibilidad en tiempo real.</summary>
    [HttpGet("estaciones")]
    public IActionResult ListarEstaciones() =>
        Ok(new Respuesta<IEnumerable<EstacionResponse>>("OK", _estaciones.Listar()));

    [HttpGet("estaciones/{id:int}")]
    public IActionResult ObtenerEstacion(int id) => Ok(new Respuesta<EstacionResponse>("OK", _estaciones.Obtener(id)));

    /// <summary>RF-03: pantalla "Agregar estación" (solo Administrador).</summary>
    [Autorizar(Rol.Administrador)]
    [HttpPost("estaciones")]
    public IActionResult CrearEstacion([FromBody] EstacionRequest r) =>
        StatusCode(201, new Respuesta<EstacionResponse>("¡Estación agregada!", _estaciones.Crear(r)));

    /// <summary>Pantalla "Control de estaciones": abrir o cerrar.</summary>
    [Autorizar(Rol.Administrador, Rol.Tecnico)]
    [HttpPut("estaciones/{id:int}/estado")]
    public IActionResult CambiarEstado(int id, [FromBody] EstadoEstacionRequest r) =>
        Ok(new Respuesta<EstacionResponse>("Estado actualizado.", _estaciones.CambiarEstado(id, r.Estado!.Value)));

    // ---------- Bicicletas ----------

    [HttpGet("bicicletas")]
    public IActionResult ListarBicicletas([FromQuery] int? estacionId, [FromQuery] EstadoBicicleta? estado) =>
        Ok(new Respuesta<IEnumerable<Bicicleta>>("OK", _bicicletas.Listar(estacionId, estado)));

    [HttpGet("bicicletas/{id:int}")]
    public IActionResult ObtenerBicicleta(int id) => Ok(new Respuesta<Bicicleta>("OK", _bicicletas.Obtener(id)));

    /// <summary>RF-04: pantalla "Agregar bicicleta".</summary>
    [Autorizar(Rol.Administrador, Rol.Tecnico)]
    [HttpPost("bicicletas")]
    public IActionResult CrearBicicleta([FromBody] BicicletaRequest r)
    {
        var b = _bicicletas.Crear(r);
        return StatusCode(201, new Respuesta<Bicicleta>($"¡Bicicleta {b.Id:D4} agregada!", b));
    }

    /// <summary>RU-07: reubicación de bicicletas entre estaciones.</summary>
    [Autorizar(Rol.Administrador, Rol.Tecnico)]
    [HttpPut("bicicletas/{id:int}/estacion")]
    public IActionResult Reubicar(int id, [FromBody] ReubicarRequest r) =>
        Ok(new Respuesta<Bicicleta>("Bicicleta reubicada.", _bicicletas.Reubicar(id, r.EstacionId)));
}
