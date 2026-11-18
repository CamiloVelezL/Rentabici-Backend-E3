using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RentabiciBackend.Data;
using RentabiciBackend.Dtos;
using RentabiciBackend.Models;

namespace RentabiciBackend.Security;

/// <summary>RNF-02: hash PBKDF2-SHA256 con sal aleatoria (incluido en .NET, sin paquetes externos).</summary>
public static class PasswordHasher
{
    private const int Iteraciones = 100_000;
    private const int TamanoHash = 32;

    public static string Hash(string password)
    {
        var sal = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
        return $"{Convert.ToBase64String(sal)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string password, string almacenado)
    {
        var partes = almacenado.Split(':');
        if (partes.Length != 2) return false;
        var sal = Convert.FromBase64String(partes[0]);
        var esperado = Convert.FromBase64String(partes[1]);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
        return CryptographicOperations.FixedTimeEquals(hash, esperado);
    }
}

/// <summary>
/// Sesiones con token opaco (Bearer). En C4 se proyecta JWT; esta interfaz permite
/// reemplazar la implementación por un generador JWT sin tocar controladores (DIP).
/// </summary>
public interface ISessionService
{
    string Crear(int usuarioId);
    int? Validar(string token);
    void Cerrar(string token);
}

public class SessionService : ISessionService
{
    private static readonly TimeSpan Duracion = TimeSpan.FromHours(8);
    private readonly ConcurrentDictionary<string, (int UsuarioId, DateTime Expira)> _sesiones = new();

    public string Crear(int usuarioId)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _sesiones[token] = (usuarioId, DateTime.UtcNow.Add(Duracion));
        return token;
    }

    public int? Validar(string token)
    {
        if (_sesiones.TryGetValue(token, out var s))
        {
            if (s.Expira > DateTime.UtcNow) return s.UsuarioId;
            _sesiones.TryRemove(token, out _);
        }
        return null;
    }

    public void Cerrar(string token) => _sesiones.TryRemove(token, out _);
}

/// <summary>
/// RF-02: restringe el acceso por rol. [Autorizar] = cualquier usuario autenticado;
/// [Autorizar(Rol.Administrador)] = solo administradores.
/// Flujos alternativos: 401 sin token o token vencido, 403 rol sin permiso.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AutorizarAttribute : Attribute, IAuthorizationFilter
{
    public const string ClaveUsuario = "UsuarioActual";
    private readonly Rol[] _roles;

    public AutorizarAttribute(params Rol[] roles) => _roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var header = context.HttpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = Error(401, "NO_AUTENTICADO", "Debe iniciar sesión para usar este servicio.");
            return;
        }

        var token = header["Bearer ".Length..].Trim();
        var sesiones = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();
        var db = context.HttpContext.RequestServices.GetRequiredService<MemoryDatabase>();

        var usuarioId = sesiones.Validar(token);
        var usuario = usuarioId is null ? null : db.Usuarios.FirstOrDefault(u => u.Id == usuarioId);
        if (usuario is null)
        {
            context.Result = Error(401, "SESION_INVALIDA", "La sesión expiró o no es válida. Inicie sesión de nuevo.");
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(usuario.Rol))
        {
            context.Result = Error(403, "ACCESO_DENEGADO", $"El rol '{usuario.Rol}' no tiene permiso para esta operación.");
            return;
        }

        context.HttpContext.Items[ClaveUsuario] = usuario;
    }

    private static ObjectResult Error(int status, string codigo, string mensaje) =>
        new(new ErrorResponse(codigo, mensaje)) { StatusCode = status };
}
