using RentabiciBackend.Data;
using RentabiciBackend.Exceptions;
using RentabiciBackend.Models;
using RentabiciBackend.Services.Notificaciones;
using RentabiciBackend.Services.Tarifas;

namespace RentabiciBackend.Services;

/// <summary>
/// Sujeto real del PATRÓN PROXY: contiene solo la lógica transaccional del alquiler.
/// Las validaciones de acceso (multas, saldo, límite activo) las hace RentalServiceProxy.
/// Usa el PATRÓN STATE (bici.Alquilar / bici.Devolver) y el PATRÓN STRATEGY (tarifa).
/// </summary>
public class RentalManagerService : IRentalService
{
    private readonly MemoryDatabase _db;
    private readonly ITarifaSelector _tarifas;
    private readonly INotificacionService _notificaciones;
    private readonly IHostEnvironment _entorno;

    public RentalManagerService(MemoryDatabase db, ITarifaSelector tarifas,
                                INotificacionService notificaciones, IHostEnvironment entorno)
    {
        _db = db;
        _tarifas = tarifas;
        _notificaciones = notificaciones;
        _entorno = entorno;
    }

    /// <summary>RF-06: pantalla "Recibir bicicleta" (QR). Convierte la reserva en alquiler.</summary>
    public Alquiler IniciarAlquiler(int usuarioId, int bicicletaId)
    {
        Alquiler alquiler;
        Usuario usuario;
        lock (_db.Lock)
        {
            usuario = _db.Usuarios.First(u => u.Id == usuarioId);
            var bici = _db.Bicicletas.FirstOrDefault(b => b.Id == bicicletaId)
                       ?? throw new NotFoundException("La bicicleta", bicicletaId);

            if (bici.EstacionId is null)
                throw new BusinessRuleException("BICICLETA_FUERA_DE_ESTACION", "La bicicleta no está anclada en una estación.");

            Reserva? reserva = null;
            if (bici.Estado == EstadoBicicleta.Reservada)
            {
                reserva = _db.Reservas.First(r => r.BicicletaId == bici.Id && r.Estado == EstadoReserva.Activa);
                if (reserva.UsuarioId != usuarioId)
                    throw new BusinessRuleException("BICICLETA_RESERVADA", "La bicicleta está reservada por otro usuario.");
            }
            else
            {
                ReglasEstacion.ValidarAbierta(ReglasEstacion.Buscar(_db, bici.EstacionId.Value));
            }

            var origen = bici.EstacionId.Value;
            bici.Alquilar();          // State: Disponible/Reservada → Alquilada; otro estado → TRANSICION_INVALIDA
            bici.EstacionId = null;   // en viaje
            if (reserva is not null) reserva.Estado = EstadoReserva.Completada;

            alquiler = new Alquiler
            {
                Id = MemoryDatabase.SiguienteId(_db.Alquileres, a => a.Id),
                UsuarioId = usuarioId,
                BicicletaId = bici.Id,
                EstacionOrigenId = origen,
                ReservaId = reserva?.Id,
                TiempoReservado = reserva?.TiempoMinutos ?? 0
            };
            _db.Alquileres.Add(alquiler);
        }

        _notificaciones.Notificar(usuario, "Viaje iniciado",
            $"Recibiste la bicicleta {bicicletaId:D4} a las {DateTime.Now:HH:mm}. ¡Buen viaje!", CanalNotificacion.Sms);
        return alquiler;
    }

    /// <summary>RF-07: "Entregar bicicleta". Calcula la tarifa y deja el pago pendiente.</summary>
    public Alquiler Devolver(int usuarioId, int alquilerId, int? estacionDestinoId, int? minutosSimulados = null)
    {
        Alquiler alquiler;
        Usuario usuario;
        lock (_db.Lock)
        {
            usuario = _db.Usuarios.First(u => u.Id == usuarioId);
            alquiler = _db.Alquileres.FirstOrDefault(a => a.Id == alquilerId)
                       ?? throw new NotFoundException("El alquiler", alquilerId);

            if (alquiler.UsuarioId != usuarioId)
                throw new AccesoDenegadoException("El alquiler pertenece a otro usuario.");
            if (alquiler.Estado != EstadoAlquiler.Activo)
                throw new BusinessRuleException("ALQUILER_NO_ACTIVO", "Este alquiler ya fue finalizado.");

            var destino = ReglasEstacion.Buscar(_db, estacionDestinoId ?? alquiler.EstacionOrigenId);
            ReglasEstacion.ValidarAbierta(destino);
            ReglasEstacion.ValidarCapacidad(_db, destino); // RN-03: no se devuelve en estación llena

            var fin = DateTime.UtcNow;
            var minutosUsados = minutosSimulados is not null && _entorno.IsDevelopment()
                ? minutosSimulados.Value
                : Math.Max(1, (int)Math.Ceiling((fin - alquiler.Inicio).TotalMinutes));

            // Se cobra como mínimo el tiempo elegido al reservar
            var minutosCobrados = Math.Max(minutosUsados, alquiler.TiempoReservado);
            var estrategia = _tarifas.Seleccionar(minutosCobrados); // Strategy
            var desglose = estrategia.Calcular(minutosCobrados);

            var bici = _db.Bicicletas.First(b => b.Id == alquiler.BicicletaId);
            bici.Devolver();                // State: → Disponible / EnRevision / EnMantenimiento
            bici.EstacionId = destino.Id;

            alquiler.Fin = fin;
            alquiler.EstacionDestinoId = destino.Id;
            alquiler.MinutosUso = minutosUsados;
            alquiler.MontoAlquiler = desglose.Alquiler;
            alquiler.MontoRecargo = desglose.Recargo;
            alquiler.Costo = desglose.Total;
            alquiler.TarifaAplicada = estrategia.Nombre;
            alquiler.Estado = EstadoAlquiler.PendientePago;

            usuario.SaldoPendiente += desglose.Total; // RN-04: queda bloqueado hasta pagar
        }

        _notificaciones.Notificar(usuario, "Viaje finalizado",
            $"Duración: {alquiler.MinutosUso} min. Valor a pagar: {alquiler.Costo:N0} COP ({alquiler.TarifaAplicada}).");
        return alquiler;
    }
}
