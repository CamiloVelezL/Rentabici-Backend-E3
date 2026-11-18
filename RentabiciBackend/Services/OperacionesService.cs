using Microsoft.Extensions.Options;
using RentabiciBackend.Data;
using RentabiciBackend.Dtos;
using RentabiciBackend.Exceptions;
using RentabiciBackend.Models;
using RentabiciBackend.Services.Notificaciones;
using RentabiciBackend.Services.Pagos;
using RentabiciBackend.Services.Tarifas;

namespace RentabiciBackend.Services;

// ---------------------------------------------------------------------------
// Consultas de alquiler: separadas de IRentalService (ISP) para que el Proxy solo
// intercepte operaciones que cambian estado.
// ---------------------------------------------------------------------------
public interface IAlquilerConsultaService
{
    Alquiler? Activo(int usuarioId);
    IEnumerable<Alquiler> Historial(int usuarioId);
}

public class AlquilerConsultaService : IAlquilerConsultaService
{
    private readonly MemoryDatabase _db;
    public AlquilerConsultaService(MemoryDatabase db) => _db = db;

    public Alquiler? Activo(int usuarioId)
    {
        lock (_db.Lock) return _db.Alquileres.FirstOrDefault(a => a.UsuarioId == usuarioId && a.Estado == EstadoAlquiler.Activo);
    }

    public IEnumerable<Alquiler> Historial(int usuarioId)
    {
        lock (_db.Lock) return _db.Alquileres.Where(a => a.UsuarioId == usuarioId).OrderByDescending(a => a.Inicio).ToList();
    }
}

// ---------------------------------------------------------------------------
// Épica 5 – Pagos y multas con código de factura (RF-10, RF-11, RF-12, RN-04)
// ---------------------------------------------------------------------------
public interface IPagoService
{
    PendientesResponse Pendientes(Usuario usuario);
    PagoResultado PagarFactura(Usuario usuario, string codigoFactura);
    IEnumerable<Pago> Historial(int usuarioId);
}

public class PagoService : IPagoService
{
    private readonly MemoryDatabase _db;
    private readonly IPaymentFacade _pagos;
    private readonly TarifaOptions _tarifas;

    public PagoService(MemoryDatabase db, IPaymentFacade pagos, IOptions<TarifaOptions> tarifas)
    {
        _db = db;
        _pagos = pagos;
        _tarifas = tarifas.Value;
    }

    /// <summary>Filas de "Pagos y multas" + el código de la factura que el usuario debe ingresar para pagar.</summary>
    public PendientesResponse Pendientes(Usuario usuario)
    {
        lock (_db.Lock)
        {
            var factura = ActualizarFacturaAbierta(usuario);
            if (factura is null) return new PendientesResponse(Array.Empty<ItemPendiente>(), 0, null);

            var items = new List<ItemPendiente>();
            foreach (var a in _db.Alquileres.Where(a => factura.AlquilerIds.Contains(a.Id)))
            {
                items.Add(new ItemPendiente("Alquiler", a.MontoAlquiler ?? 0, a.Id));
                if ((a.MontoRecargo ?? 0) > 0) items.Add(new ItemPendiente("Multa", a.MontoRecargo!.Value, a.Id));
            }
            if (factura.IncluyeMulta) items.Add(new ItemPendiente("Multa", factura.MontoMulta, null));

            return new PendientesResponse(items, factura.Total, factura.Codigo);
        }
    }

    public PagoResultado PagarFactura(Usuario usuario, string codigoFactura)
    {
        var codigo = codigoFactura.Trim().ToUpperInvariant();

        lock (_db.Lock)
        {
            ActualizarFacturaAbierta(usuario); // la factura abierta siempre refleja la deuda actual

            var factura = _db.Facturas.FirstOrDefault(f => f.Codigo == codigo)
                          ?? throw new BusinessRuleException("FACTURA_NO_EXISTE",
                              $"No existe una factura con el código {codigo}. Revísalo en \"Pagos y multas\".", 404);

            if (factura.UsuarioId != usuario.Id)
                throw new AccesoDenegadoException($"La factura {codigo} pertenece a otro usuario.");

            if (factura.Estado == EstadoFactura.Pagada)
                throw new BusinessRuleException("FACTURA_YA_PAGADA",
                    $"La factura {codigo} ya fue pagada el {factura.FechaPago!.Value.ToLocalTime():dd/MM/yyyy HH:mm}.");

            var resultado = _pagos.ConfirmarPago(usuario, factura); // Facade
            if (!resultado.Confirmado)
                throw new BusinessRuleException("PAGO_NO_PROCESADO", $"{resultado.Mensaje} Su saldo sigue pendiente.", 503);

            var pago = new Pago
            {
                Id = MemoryDatabase.SiguienteId(_db.Pagos, p => p.Id),
                UsuarioId = usuario.Id,
                AlquilerId = factura.AlquilerIds.Count == 1 ? factura.AlquilerIds[0] : null,
                CodigoFactura = factura.Codigo,
                Concepto = $"Factura {factura.Codigo}",
                Monto = factura.Total,
                Metodo = "Código de factura",
                Referencia = resultado.Referencia,
                Aprobado = true,
                MensajePasarela = resultado.Mensaje
            };
            _db.Pagos.Add(pago);

            foreach (var a in _db.Alquileres.Where(a => factura.AlquilerIds.Contains(a.Id)))
                a.Estado = EstadoAlquiler.Pagado;
            if (factura.IncluyeMulta) usuario.TieneMulta = false;
            usuario.SaldoPendiente = 0;   // RN-04: queda desbloqueado

            factura.Estado = EstadoFactura.Pagada;
            factura.FechaPago = DateTime.UtcNow;

            return new PagoResultado(factura.Total, factura.Codigo, resultado.Referencia);
        }
    }

    public IEnumerable<Pago> Historial(int usuarioId)
    {
        lock (_db.Lock) return _db.Pagos.Where(p => p.UsuarioId == usuarioId).OrderByDescending(p => p.Fecha).ToList();
    }

    /// <summary>
    /// Crea o actualiza la única factura Pendiente del usuario con lo que debe ahora
    /// (alquileres entregados sin pagar + multa). Devuelve null si no debe nada.
    /// Se ejecuta siempre dentro de _db.Lock.
    /// </summary>
    private Factura? ActualizarFacturaAbierta(Usuario usuario)
    {
        var alquileres = _db.Alquileres
            .Where(a => a.UsuarioId == usuario.Id && a.Estado == EstadoAlquiler.PendientePago)
            .ToList();
        var factura = _db.Facturas.FirstOrDefault(f => f.UsuarioId == usuario.Id && f.Estado == EstadoFactura.Pendiente);

        if (alquileres.Count == 0 && !usuario.TieneMulta)
        {
            if (factura is not null) _db.Facturas.Remove(factura); // ya no debe nada: la factura abierta sobra
            return null;
        }

        if (factura is null)
        {
            var id = MemoryDatabase.SiguienteId(_db.Facturas, f => f.Id);
            factura = new Factura { Id = id, Codigo = $"RB-{id:D4}", UsuarioId = usuario.Id };
            _db.Facturas.Add(factura);
        }

        factura.AlquilerIds = alquileres.Select(a => a.Id).ToList();
        factura.MontoAlquileres = alquileres.Sum(a => a.MontoAlquiler ?? 0);
        factura.MontoRecargos = alquileres.Sum(a => a.MontoRecargo ?? 0);
        factura.IncluyeMulta = usuario.TieneMulta;
        factura.MontoMulta = usuario.TieneMulta ? _tarifas.ValorMulta : 0;
        return factura;
    }
}

// ---------------------------------------------------------------------------
// Épica 4 – Incidencias y mantenimiento (RF-08, RF-09, RN-05)
// ---------------------------------------------------------------------------
// ISP (análisis SOLID del E2): quien reporta una falla no depende de las operaciones de
// reparación, y el técnico no depende del reporte. Una sola clase implementa ambas interfaces.

/// <summary>Lo que usa cualquier usuario: reportar una falla (RF-08).</summary>
public interface IReportadorIncidencias
{
    (Incidencia Incidencia, string Mensaje) Reportar(Usuario usuario, IncidenciaRequest r);
}

/// <summary>Lo que usa el técnico / administrador: listar, solucionar o dar de baja (RF-09).</summary>
public interface IGestorMantenimiento
{
    IEnumerable<Incidencia> Listar(EstadoIncidencia? estado);
    AtencionResultado Atender(Usuario tecnico, int incidenciaId, AtenderIncidenciaRequest r);
    AtencionResultado DarDeBaja(Usuario tecnico, int incidenciaId, AtenderIncidenciaRequest r);
}

public class IncidenciaService : IReportadorIncidencias, IGestorMantenimiento
{
    private readonly MemoryDatabase _db;
    private readonly INotificacionService _notificaciones;

    public IncidenciaService(MemoryDatabase db, INotificacionService notificaciones)
    {
        _db = db;
        _notificaciones = notificaciones;
    }

    public (Incidencia Incidencia, string Mensaje) Reportar(Usuario usuario, IncidenciaRequest r)
    {
        Incidencia incidencia;
        Bicicleta bici;
        Usuario? reservanteAfectado = null;
        bool requiereMantenimiento;

        lock (_db.Lock)
        {
            bici = _db.Bicicletas.FirstOrDefault(b => b.Id == r.BicicletaId)
                   ?? throw new BusinessRuleException("BICICLETA_NO_EXISTE", "No existe una bicicleta con ese id.", 404);
            var estacion = ReglasEstacion.Buscar(_db, r.EstacionId);

            // Si no está en viaje, debe estar físicamente en la estación indicada
            if (bici.Estado != EstadoBicicleta.Alquilada && bici.EstacionId != estacion.Id)
                throw new BusinessRuleException("BICICLETA_NO_ESTA_EN_ESTACION",
                    $"La bicicleta {bici.Id:D4} no está en {estacion.Nombre}. Verifique el id o la estación.");

            if (_db.Incidencias.Any(i => i.BicicletaId == bici.Id && i.UsuarioId == usuario.Id && i.Estado == EstadoIncidencia.Pendiente))
                throw new BusinessRuleException("REPORTE_DUPLICADO",
                    $"Ya reportó una falla de la bicicleta {bici.Id:D4} que aún no se ha solucionado.");

            var gravedad = r.Gravedad!.Value;
            var reportesDistintos = bici.ReportantesRecientes.Count + (bici.ReportantesRecientes.Contains(usuario.Id) ? 0 : 1);

            // RN-05: 2 o más reportes de usuarios distintos → mantenimiento. Una falla Mayor también.
            requiereMantenimiento = gravedad == GravedadIncidencia.Mayor || reportesDistintos >= 2;

            // Si estaba reservada, la reserva se cancela para no entregar una bici averiada.
            if (bici.Estado == EstadoBicicleta.Reservada)
            {
                var reserva = _db.Reservas.First(x => x.BicicletaId == bici.Id && x.Estado == EstadoReserva.Activa);
                reserva.Estado = EstadoReserva.Cancelada;
                reservanteAfectado = _db.Usuarios.First(u => u.Id == reserva.UsuarioId);
            }

            bici.ReportarFalla(requiereMantenimiento); // State (DadaDeBaja → TRANSICION_INVALIDA)
            bici.ReportantesRecientes.Add(usuario.Id);

            incidencia = new Incidencia
            {
                Id = MemoryDatabase.SiguienteId(_db.Incidencias, i => i.Id),
                BicicletaId = bici.Id,
                UsuarioId = usuario.Id,
                EstacionId = estacion.Id,
                Descripcion = string.IsNullOrWhiteSpace(r.Descripcion) ? gravedad.ToString() : r.Descripcion.Trim(),
                Gravedad = gravedad,
                EstadoResultante = bici.Estado
            };
            _db.Incidencias.Add(incidencia);
        }

        if (reservanteAfectado is not null)
            _notificaciones.Notificar(reservanteAfectado, "Reserva cancelada",
                $"La bicicleta {bici.Id:D4} fue reportada con una falla y tu reserva se canceló. Reserva otra.",
                CanalNotificacion.Sms);

        if (requiereMantenimiento)
            foreach (var tecnico in _db.Usuarios.Where(u => u.Rol == Rol.Tecnico).ToList())
                _notificaciones.Notificar(tecnico, "Bicicleta a mantenimiento",
                    $"La bicicleta {bici.Id:D4} requiere mantenimiento ({incidencia.Gravedad}).", CanalNotificacion.Sms);

        var mensaje = bici.Estado switch
        {
            EstadoBicicleta.Alquilada => $"La falla de la bicicleta {bici.Id:D4} quedó registrada. Al entregarla pasará a revisión.",
            EstadoBicicleta.EnMantenimiento => $"La bicicleta {bici.Id:D4} pasó a Reparación y un técnico la revisará. Gracias por avisar.",
            _ => $"La bicicleta {bici.Id:D4} quedó en revisión y un técnico la revisará. Gracias por avisar."
        };
        return (incidencia, mensaje);
    }

    public IEnumerable<Incidencia> Listar(EstadoIncidencia? estado)
    {
        lock (_db.Lock)
            return _db.Incidencias.Where(i => estado is null || i.Estado == estado)
                                  .OrderBy(i => i.Fecha).ToList();
    }

    /// <summary>RF-09: el técnico soluciona la falla; si la bici no tiene más fallas pendientes vuelve a Disponible.</summary>
    public AtencionResultado Atender(Usuario tecnico, int incidenciaId, AtenderIncidenciaRequest r)
    {
        lock (_db.Lock)
        {
            var incidencia = _db.Incidencias.FirstOrDefault(i => i.Id == incidenciaId)
                             ?? throw new NotFoundException("La incidencia", incidenciaId);
            if (incidencia.Estado == EstadoIncidencia.Atendida)
                throw new BusinessRuleException("INCIDENCIA_YA_ATENDIDA", "Esta falla ya fue solucionada.");

            var bici = _db.Bicicletas.First(b => b.Id == incidencia.BicicletaId);
            if (bici.Estado == EstadoBicicleta.Alquilada)
                throw new BusinessRuleException("BICICLETA_EN_USO",
                    $"La bicicleta {bici.Id:D4} está en un viaje. Podrá atenderse cuando sea entregada.");

            incidencia.Estado = EstadoIncidencia.Atendida;

            var quedanPendientes = _db.Incidencias.Any(i =>
                i.BicicletaId == bici.Id && i.Estado == EstadoIncidencia.Pendiente);

            var habilitada = false;
            if (!quedanPendientes && bici.Estado is EstadoBicicleta.EnRevision or EstadoBicicleta.EnMantenimiento)
            {
                bici.Liberar(); // State: → Disponible
                habilitada = true;
            }

            _db.Mantenimientos.Add(new RegistroMantenimiento
            {
                Id = MemoryDatabase.SiguienteId(_db.Mantenimientos, m => m.Id),
                BicicletaId = bici.Id,
                IncidenciaId = incidencia.Id,
                TecnicoId = tecnico.Id,
                Diagnostico = string.IsNullOrWhiteSpace(r.Diagnostico) ? $"Falla {incidencia.Gravedad} solucionada" : r.Diagnostico.Trim(),
                PiezasReemplazadas = r.PiezasReemplazadas?.Trim(),
                Reparable = true,
                EstadoResultante = bici.Estado
            });

            return new AtencionResultado(incidencia, habilitada);
        }
    }
    /// <summary>
    /// RF-09: la bicicleta no se puede reparar → estado final DadaDeBaja (patrón State).
    /// Se cierran todas sus fallas pendientes y queda fuera de cualquier estación.
    /// </summary>
    public AtencionResultado DarDeBaja(Usuario tecnico, int incidenciaId, AtenderIncidenciaRequest r)
    {
        lock (_db.Lock)
        {
            var incidencia = _db.Incidencias.FirstOrDefault(i => i.Id == incidenciaId)
                             ?? throw new NotFoundException("La incidencia", incidenciaId);
            if (incidencia.Estado == EstadoIncidencia.Atendida)
                throw new BusinessRuleException("INCIDENCIA_YA_ATENDIDA", "Esta falla ya fue atendida.");

            var bici = _db.Bicicletas.First(b => b.Id == incidencia.BicicletaId);
            if (bici.Estado == EstadoBicicleta.Alquilada)
                throw new BusinessRuleException("BICICLETA_EN_USO",
                    $"La bicicleta {bici.Id:D4} está en un viaje. Podrá darse de baja cuando sea entregada.");

            bici.DarDeBaja();       // State: EnRevision / EnMantenimiento → DadaDeBaja (estado final)
            bici.EstacionId = null; // libera el espacio de anclaje

            foreach (var i in _db.Incidencias.Where(i => i.BicicletaId == bici.Id && i.Estado == EstadoIncidencia.Pendiente))
                i.Estado = EstadoIncidencia.Atendida;

            _db.Mantenimientos.Add(new RegistroMantenimiento
            {
                Id = MemoryDatabase.SiguienteId(_db.Mantenimientos, m => m.Id),
                BicicletaId = bici.Id,
                IncidenciaId = incidencia.Id,
                TecnicoId = tecnico.Id,
                Diagnostico = string.IsNullOrWhiteSpace(r.Diagnostico) ? "No reparable: bicicleta dada de baja" : r.Diagnostico.Trim(),
                PiezasReemplazadas = r.PiezasReemplazadas?.Trim(),
                Reparable = false,
                EstadoResultante = bici.Estado
            });

            return new AtencionResultado(incidencia, false);
        }
    }
}
