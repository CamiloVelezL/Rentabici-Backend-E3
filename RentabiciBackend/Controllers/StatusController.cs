using Microsoft.AspNetCore.Mvc;
namespace RentabiciBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("API Rentabici funcionando correctamente");
}
