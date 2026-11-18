using RentabiciBackend.Data;
using RentabiciBackend.Dtos;
using RentabiciBackend.Exceptions;
using RentabiciBackend.Models;
using RentabiciBackend.Security;
using RentabiciBackend.Services.Notificaciones;

namespace RentabiciBackend.Services;

/// <summary>Épica 1 – Gestión de usuarios, roles y autenticación (RF-01, RF-02).</summary>
public interface IAuthService
{
    Usuario Registrar(RegistroRequest request);
    LoginResponse Login(LoginRequest request);
    void Logout(string token);
}

/// <summary>Administración de roles (pantalla "Gestión de roles y permisos").</summary>
public interface IUsuarioService
{
    IEnumerable<RolDto> Roles();
    IEnumerable<Usuario> Listar();
    Usuario CambiarRol(Usuario administrador, int usuarioId, int idRol);
}

public class AuthService : IAuthService
{
    private readonly MemoryDatabase _db;
    private readonly ISessionService _sesiones;
    private readonly INotificacionService _notificaciones;

    public AuthService(MemoryDatabase db, ISessionService sesiones, INotificacionService notificaciones)
    {
        _db = db;
        _sesiones = sesiones;
        _notificaciones = notificaciones;
    }

    public Usuario Registrar(RegistroRequest r)
    {
        Usuario nuevo;
        lock (_db.Lock)
        {
            var email = r.Email.Trim().ToLowerInvariant();

            if (_db.Usuarios.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                throw new BusinessRuleException("EMAIL_DUPLICADO", "Ya existe una cuenta con ese correo electrónico.");

            if (_db.Usuarios.Any(u => u.Documento == r.Documento.Trim()))
                throw new BusinessRuleException("DOCUMENTO_DUPLICADO", "Ya existe una cuenta con ese documento.");

            nuevo = new Usuario
            {
                Id = MemoryDatabase.SiguienteId(_db.Usuarios, u => u.Id),
                Nombre = r.Nombre.Trim(),
                Apellidos = r.Apellidos.Trim(),
                Documento = r.Documento.Trim(),
                Email = email,
                PasswordHash = PasswordHasher.Hash(r.Password),
                Rol = Rol.Usuario // el registro público siempre asigna rol Usuario
            };
            _db.Usuarios.Add(nuevo);
        }

        _notificaciones.Notificar(nuevo, "Bienvenido a RentaBici", $"Hola {nuevo.Nombre}, tu cuenta fue creada correctamente.");
        return nuevo;
    }

    public LoginResponse Login(LoginRequest r)
    {
        var clave = r.Usuario.Trim();
        Usuario? usuario;
        lock (_db.Lock)
        {
            usuario = _db.Usuarios.FirstOrDefault(u =>
                u.Email.Equals(clave, StringComparison.OrdinalIgnoreCase) ||
                u.Nombre.Equals(clave, StringComparison.OrdinalIgnoreCase));
        }

        // Mismo mensaje para usuario inexistente y contraseña errada (no revela qué cuentas existen).
        if (usuario is null || !PasswordHasher.Verificar(r.Password, usuario.PasswordHash))
            throw new NoAutenticadoException("CREDENCIALES_INVALIDAS", "Correo/usuario o contraseña incorrectos.");

        var token = _sesiones.Crear(usuario.Id);
        return new LoginResponse(token, UsuarioDto.Desde(usuario));
    }

    public void Logout(string token) => _sesiones.Cerrar(token);
}

public class UsuarioService : IUsuarioService
{
    private readonly MemoryDatabase _db;
    public UsuarioService(MemoryDatabase db) => _db = db;

    public IEnumerable<RolDto> Roles() => new[]
    {
        new RolDto((int)Rol.Administrador, "Administrador", "Gestiona estaciones, bicicletas y usuarios"),
        new RolDto((int)Rol.Tecnico, "Técnico", "Atiende incidencias y mantenimiento"),
        new RolDto((int)Rol.Usuario, "Usuario", "Alquila bicicletas y reporta fallas"),
    };

    public IEnumerable<Usuario> Listar()
    {
        lock (_db.Lock) return _db.Usuarios.OrderBy(u => u.Id).ToList();
    }

    public Usuario CambiarRol(Usuario administrador, int usuarioId, int idRol)
    {
        if (!Enum.IsDefined(typeof(Rol), idRol))
            throw new ValidacionException("Rol inválido.");

        lock (_db.Lock)
        {
            var usuario = _db.Usuarios.FirstOrDefault(u => u.Id == usuarioId)
                          ?? throw new NotFoundException("El usuario", usuarioId);

            if (usuario.Id == administrador.Id)
                throw new BusinessRuleException("CAMBIO_PROPIO_ROL", "No puede cambiar su propio rol.");

            var nuevoRol = (Rol)idRol;
            if (usuario.Rol == Rol.Administrador && nuevoRol != Rol.Administrador &&
                _db.Usuarios.Count(u => u.Rol == Rol.Administrador) == 1)
                throw new BusinessRuleException("ULTIMO_ADMINISTRADOR", "Debe quedar al menos un administrador.");

            usuario.Rol = nuevoRol;
            return usuario;
        }
    }
}
