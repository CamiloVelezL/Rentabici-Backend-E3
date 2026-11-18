using Microsoft.AspNetCore.Mvc;
using RentabiciBackend.Exceptions;
using RentabiciBackend.Models;
using RentabiciBackend.Security;

namespace RentabiciBackend.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Usuario autenticado que dejó el filtro [Autorizar].</summary>
    protected Usuario UsuarioActual =>
        HttpContext.Items[AutorizarAttribute.ClaveUsuario] as Usuario ?? throw new NoAutenticadoException();

    protected string TokenActual =>
        Request.Headers.Authorization.ToString()["Bearer ".Length..].Trim();
}
