using Microsoft.AspNetCore.Mvc;
using RentabiciBackend.Data;
using RentabiciBackend.Models;


namespace RentabiciBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IncidenceController : ControllerBase
{
    private readonly MemoryDatabase _db;
    public IncidenceController(MemoryDatabase db) { _db = db; }

    [HttpPost("reportar")]
    public IActionResult Reportar(int bicicletaId, string descripcion, string gravedad)
    {
        var bici = _db.Bicicletas.FirstOrDefault(b => b.Id == bicicletaId);
        if (bici == null) return NotFound("Bicicleta no existe");

        _db.Incidencias.Add(new Incidencia { Id = _db.Incidencias.Count + 1, BicicletaId = bicicletaId, Descripcion = descripcion, Gravedad = gravedad });

        if (gravedad.ToLower() == "grave") bici.Estado = "EnMantenimiento";

        return Ok($"Incidencia registrada. Estado de bici: {bici.Estado}");
    }
}
