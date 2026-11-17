using Microsoft.AspNetCore.Mvc;
using RentabiciBackend.Services;


namespace RentabiciBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RentalController : ControllerBase
{
    private readonly IRentalService _service;
    public RentalController(IRentalService service) { _service = service; }

    [HttpPost("alquilar")]
    public IActionResult Alquilar(int usuarioId, int bicicletaId)
    {
        var res = _service.ProcesarAlquiler(usuarioId, bicicletaId);
        if (res.StartsWith("ERROR")) return BadRequest(res);
        return Ok(res);
    }
}
