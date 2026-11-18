using RentabiciBackend.Data;
using RentabiciBackend.Exceptions;
using RentabiciBackend.Models;

namespace RentabiciBackend.Services;

/// <summary>
/// PATRÓN PROXY (GoF, estructural) – proxy de protección.
/// Implementa IRentalService igual que el sujeto real e intercepta cada solicitud para
/// validar las condiciones de acceso antes de delegar:
///   - el usuario existe
///   - RN-04: no tiene multa ni saldo pendiente (solo para iniciar; para devolver NO se bloquea,
///     porque impedir la devolución dejaría la bicicleta fuera del sistema)
///   - RN-01: no tiene otro viaje o reserva activa
/// Todo se ejecuta dentro del mismo lock que el sujeto real (RNF-06), así la validación y
/// la operación son atómicas.
/// </summary>
public class RentalServiceProxy : IRentalService
{
    private readonly RentalManagerService _real;
    private readonly MemoryDatabase _db;
    private readonly ILogger<RentalServiceProxy> _logger;

    public RentalServiceProxy(RentalManagerService real, MemoryDatabase db, ILogger<RentalServiceProxy> logger)
    {
        _real = real;
        _db = db;
        _logger = logger;
    }

    public Alquiler IniciarAlquiler(int usuarioId, int bicicletaId)
    {
        lock (_db.Lock)
        {
            var usuario = ObtenerUsuario(usuarioId);
            ReglasUsuario.ValidarNoBloqueado(usuario);                                       // RN-04
            ReglasUsuario.ValidarSinOperacionActiva(_db, usuarioId, bicicletaPermitida: bicicletaId); // RN-01

            _logger.LogInformation("Proxy: usuario {UsuarioId} autorizado para alquilar bicicleta {BicicletaId}", usuarioId, bicicletaId);
            return _real.IniciarAlquiler(usuarioId, bicicletaId);
        }
    }

    public Alquiler Devolver(int usuarioId, int alquilerId, int? estacionDestinoId, int? minutosSimulados = null)
    {
        lock (_db.Lock)
        {
            ObtenerUsuario(usuarioId);
            return _real.Devolver(usuarioId, alquilerId, estacionDestinoId, minutosSimulados);
        }
    }

    private Usuario ObtenerUsuario(int usuarioId) =>
        _db.Usuarios.FirstOrDefault(u => u.Id == usuarioId) ?? throw new NotFoundException("El usuario", usuarioId);
}
