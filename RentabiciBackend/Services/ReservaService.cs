using System.Globalization;
using Microsoft.Extensions.Options;
using RentabiciBackend.Data;
using RentabiciBackend.Dtos;
using RentabiciBackend.Exceptions;
using RentabiciBackend.Models;
using RentabiciBackend.Services.Notificaciones;
using RentabiciBackend.Services.Tarifas;

namespace RentabiciBackend.Services;

public class ReservaOptions
{
    /// <summary>RN-02 / RF-05: minutos de validez de una reserva.</summary>
    public int MinutosExpiracion { get; set; } = 15;
}

/// <summary>Épica 3 – Reserva temporal (RF-05, RN-01, RN-02, RN-04).</summary>
public interface IReservaService
{
    Reserva Reservar(Usuario usuario, ReservaRequest r);
    Reserva Cancelar(Usuario usuario, int reservaId);
    Reserva? ObtenerActiva(int usuarioId);
    int ExpirarVencidas();
}

public class ReservaService : IReservaService
{
    private readonly MemoryDatabase _db;
    private readonly INotificacionService _notificaciones;
    private readonly ITarifaSelector _tarifas;
    private readonly ReservaOptions _opciones;
    private readonly TarifaOptions _opcionesTarifa;

    public ReservaService(MemoryDatabase db, INotificacionService notificaciones, ITarifaSelector tarifas,
                          IOptions<ReservaOptions> opciones, IOptions<TarifaOptions> opcionesTarifa)
    {
        _db = db;
        _notificaciones = notificaciones;
        _tarifas = tarifas;
        _opciones = opciones.Value;
        _opcionesTarifa = opcionesTarifa.Value;
    }

    public Reserva Reservar(Usuario usuario, ReservaRequest r)
    {
        ValidarFecha(r.FechaUso);
        ValidarTiempo(r.TiempoMinutos);

        Reserva reserva;
        lock (_db.Lock) // RNF-06: dos usuarios no pueden reservar la misma bici
        {
            ReglasUsuario.ValidarNoBloqueado(usuario);                 // RN-04
            ReglasUsuario.ValidarSinOperacionActiva(_db, usuario.Id);  // RN-01

            var estacion = ReglasEstacion.Buscar(_db, r.EstacionId);
            ReglasEstacion.ValidarAbierta(estacion);

            var modelo = r.Modelo.Trim();
            var bici = _db.Bicicletas.FirstOrDefault(b =>
                           b.EstacionId == estacion.Id &&
                           b.Estado == EstadoBicicleta.Disponible &&
                           b.Modelo.Trim().Equals(modelo, StringComparison.OrdinalIgnoreCase))
                       ?? throw new BusinessRuleException("SIN_BICICLETAS",
                           $"Ya no hay bicicletas '{modelo}' disponibles en {estacion.Nombre}. Elija otra opción.");

            bici.Reservar(); // State: Disponible → Reservada

            reserva = new Reserva
            {
                Id = MemoryDatabase.SiguienteId(_db.Reservas, x => x.Id),
                UsuarioId = usuario.Id,
                BicicletaId = bici.Id,
                EstacionId = estacion.Id,
                Modelo = bici.Modelo,
                FechaUso = r.FechaUso,
                Telefono = r.Telefono,
                TiempoMinutos = r.TiempoMinutos,
                CostoEstimado = _tarifas.Seleccionar(r.TiempoMinutos).Calcular(r.TiempoMinutos).Total, // Strategy
                Expira = DateTime.UtcNow.AddMinutes(_opciones.MinutosExpiracion)
            };
            _db.Reservas.Add(reserva);
        }

        _notificaciones.Notificar(usuario, "Reserva confirmada",
            $"Reservaste la bicicleta {reserva.BicicletaId:D4}. Tienes {_opciones.MinutosExpiracion} minutos para recibirla.",
            CanalNotificacion.Sms);
        return reserva;
    }

    public Reserva Cancelar(Usuario usuario, int reservaId)
    {
        lock (_db.Lock)
        {
            var reserva = _db.Reservas.FirstOrDefault(r => r.Id == reservaId)
                          ?? throw new NotFoundException("La reserva", reservaId);
            if (reserva.UsuarioId != usuario.Id)
                throw new AccesoDenegadoException("La reserva pertenece a otro usuario.");
            if (reserva.Estado != EstadoReserva.Activa)
                throw new BusinessRuleException("RESERVA_NO_ACTIVA", $"La reserva ya está en estado '{reserva.Estado}'.");

            var bici = _db.Bicicletas.First(b => b.Id == reserva.BicicletaId);
            if (bici.Estado == EstadoBicicleta.Reservada) bici.CancelarReserva();
            reserva.Estado = EstadoReserva.Cancelada;
            return reserva;
        }
    }

    public Reserva? ObtenerActiva(int usuarioId)
    {
        ExpirarVencidas(); // garantiza RN-02 aunque el job aún no haya corrido
        lock (_db.Lock)
            return _db.Reservas.FirstOrDefault(r => r.UsuarioId == usuarioId && r.Estado == EstadoReserva.Activa);
    }

    /// <summary>RN-02: ejecutado periódicamente por ReservaExpirationService.</summary>
    public int ExpirarVencidas()
    {
        var expiradas = new List<(Usuario Usuario, Reserva Reserva)>();
        lock (_db.Lock)
        {
            var ahora = DateTime.UtcNow;
            foreach (var r in _db.Reservas.Where(r => r.Estado == EstadoReserva.Activa && r.Expira <= ahora))
            {
                var bici = _db.Bicicletas.First(b => b.Id == r.BicicletaId);
                if (bici.Estado == EstadoBicicleta.Reservada) bici.CancelarReserva(); // libera la unidad
                r.Estado = EstadoReserva.Expirada;
                expiradas.Add((_db.Usuarios.First(u => u.Id == r.UsuarioId), r));
            }
        }

        if (expiradas.Count > 0) _db.Guardar(); // el job corre fuera de una petición HTTP

        foreach (var (u, r) in expiradas)
            _notificaciones.Notificar(u, "Reserva expirada",
                $"Tu reserva de la bicicleta {r.BicicletaId:D4} expiró porque no la recibiste a tiempo.", CanalNotificacion.Sms);

        return expiradas.Count;
    }

    private static void ValidarFecha(string texto)
    {
        if (!DateTime.TryParseExact(texto, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            throw new ValidacionException("La fecha no existe. Revise el día, el mes y el año.");
        if (fecha.Date < DateTime.Today)
            throw new ValidacionException("La fecha no puede ser anterior a hoy.");
    }

    private void ValidarTiempo(int minutos)
    {
        if (minutos % _opcionesTarifa.MinutosBloque != 0 || minutos > _opcionesTarifa.MinutosLimite)
            throw new ValidacionException(
                $"El tiempo debe ser múltiplo de {_opcionesTarifa.MinutosBloque} minutos y máximo {_opcionesTarifa.MinutosLimite}.");
    }
}
