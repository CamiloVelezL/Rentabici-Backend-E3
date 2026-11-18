using Microsoft.AspNetCore.Mvc;
using RentabiciBackend.Dtos;
using RentabiciBackend.Models;
using RentabiciBackend.Security;
using RentabiciBackend.Services;

namespace RentabiciBackend.Controllers;

/// <summary>Épica 1 – Registro, inicio de sesión y gestión de roles.</summary>
[Route("api")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;
    private readonly IUsuarioService _usuarios;

    public AuthController(IAuthService auth, IUsuarioService usuarios)
    {
        _auth = auth;
        _usuarios = usuarios;
    }

    /// <summary>RF-01: registro público (rol Usuario).</summary>
    [HttpPost("auth/registro")]
    public IActionResult Registrar([FromBody] RegistroRequest request)
    {
        var usuario = _auth.Registrar(request);
        return StatusCode(201, new Respuesta<UsuarioDto>("¡Cuenta creada! Ya puede iniciar sesión.", UsuarioDto.Desde(usuario)));
    }

    [HttpPost("auth/login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var resultado = _auth.Login(request);
        return Ok(new Respuesta<LoginResponse>($"Bienvenido, {resultado.Usuario.Nombre}.", resultado));
    }

    [Autorizar]
    [HttpPost("auth/logout")]
    public IActionResult Logout()
    {
        _auth.Logout(TokenActual);
        return Ok(new Respuesta<object?>("Sesión cerrada.", null));
    }

    /// <summary>Datos actualizados del usuario autenticado (multa, saldo, rol).</summary>
    [Autorizar]
    [HttpGet("auth/me")]
    public IActionResult Me() => Ok(new Respuesta<UsuarioDto>("OK", UsuarioDto.Desde(UsuarioActual)));

    [Autorizar]
    [HttpGet("roles")]
    public IActionResult Roles() => Ok(new Respuesta<IEnumerable<RolDto>>("OK", _usuarios.Roles()));

    /// <summary>RF-02: pantalla "Gestión de roles y permisos".</summary>
    [Autorizar(Rol.Administrador)]
    [HttpGet("usuarios")]
    public IActionResult Usuarios() =>
        Ok(new Respuesta<IEnumerable<UsuarioDto>>("OK", _usuarios.Listar().Select(UsuarioDto.Desde).ToList()));

    [Autorizar(Rol.Administrador)]
    [HttpPut("usuarios/{id:int}/rol")]
    public IActionResult CambiarRol(int id, [FromBody] CambioRolRequest r)
    {
        var usuario = _usuarios.CambiarRol(UsuarioActual, id, r.IdRol);
        return Ok(new Respuesta<UsuarioDto>("Rol actualizado.", UsuarioDto.Desde(usuario)));
    }
}
