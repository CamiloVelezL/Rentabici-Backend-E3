using RentabiciBackend.Data;
using RentabiciBackend.Models;

namespace RentabiciBackend.Services.Notificaciones;

// PATRÓN FACTORY METHOD (GoF, creacional)
// Product: INotificacion  → ConcreteProducts: EmailNotificacion, SmsNotificacion
// Creator: NotificadorCreator (declara el factory method CrearNotificacion)
// ConcreteCreators: EmailNotificadorCreator, SmsNotificadorCreator
// En el MVP el envío es simulado (se registra en la tabla Notificaciones y en el log).
// SendGrid/Twilio son ELEMENTOS PROYECTADOS: bastaría con otra ConcreteProduct.

public interface INotificacion
{
    CanalNotificacion Canal { get; }
    void Enviar(Usuario destinatario, string asunto, string mensaje);
}

public abstract class NotificacionSimulada : INotificacion
{
    private readonly MemoryDatabase _db;
    private readonly ILogger _logger;

    protected NotificacionSimulada(MemoryDatabase db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public abstract CanalNotificacion Canal { get; }
    protected abstract string Destino(Usuario u);

    public void Enviar(Usuario destinatario, string asunto, string mensaje)
    {
        lock (_db.Lock)
        {
            _db.Notificaciones.Add(new Notificacion
            {
                Id = MemoryDatabase.SiguienteId(_db.Notificaciones, n => n.Id),
                UsuarioId = destinatario.Id,
                Canal = Canal,
                Asunto = asunto,
                Mensaje = mensaje
            });
        }
        _logger.LogInformation("[{Canal}] → {Destino}: {Asunto}", Canal, Destino(destinatario), asunto);
    }
}

public class EmailNotificacion : NotificacionSimulada
{
    public EmailNotificacion(MemoryDatabase db, ILogger<EmailNotificacion> logger) : base(db, logger) { }
    public override CanalNotificacion Canal => CanalNotificacion.Email;
    protected override string Destino(Usuario u) => u.Email;
}

public class SmsNotificacion : NotificacionSimulada
{
    public SmsNotificacion(MemoryDatabase db, ILogger<SmsNotificacion> logger) : base(db, logger) { }
    public override CanalNotificacion Canal => CanalNotificacion.Sms;
    protected override string Destino(Usuario u) => $"celular de {u.Nombre}";
}

/// <summary>Creator: la lógica de notificar no conoce la clase concreta que se instancia.</summary>
public abstract class NotificadorCreator
{
    protected abstract INotificacion CrearNotificacion();   // ← Factory Method

    public void Notificar(Usuario destinatario, string asunto, string mensaje)
    {
        var notificacion = CrearNotificacion();
        notificacion.Enviar(destinatario, asunto, mensaje);
    }
}

public class EmailNotificadorCreator : NotificadorCreator
{
    private readonly MemoryDatabase _db;
    private readonly ILoggerFactory _lf;
    public EmailNotificadorCreator(MemoryDatabase db, ILoggerFactory lf) { _db = db; _lf = lf; }
    protected override INotificacion CrearNotificacion() => new EmailNotificacion(_db, _lf.CreateLogger<EmailNotificacion>());
}

public class SmsNotificadorCreator : NotificadorCreator
{
    private readonly MemoryDatabase _db;
    private readonly ILoggerFactory _lf;
    public SmsNotificadorCreator(MemoryDatabase db, ILoggerFactory lf) { _db = db; _lf = lf; }
    protected override INotificacion CrearNotificacion() => new SmsNotificacion(_db, _lf.CreateLogger<SmsNotificacion>());
}

/// <summary>Punto de entrada usado por los servicios de negocio (dependen de la abstracción, DIP).</summary>
public interface INotificacionService
{
    void Notificar(Usuario destinatario, string asunto, string mensaje, CanalNotificacion canal = CanalNotificacion.Email);
}

public class NotificacionService : INotificacionService
{
    private readonly MemoryDatabase _db;
    private readonly ILoggerFactory _lf;

    public NotificacionService(MemoryDatabase db, ILoggerFactory lf)
    {
        _db = db;
        _lf = lf;
    }

    public void Notificar(Usuario destinatario, string asunto, string mensaje, CanalNotificacion canal = CanalNotificacion.Email)
    {
        NotificadorCreator creator = canal switch
        {
            CanalNotificacion.Sms => new SmsNotificadorCreator(_db, _lf),
            _ => new EmailNotificadorCreator(_db, _lf)
        };
        creator.Notificar(destinatario, asunto, mensaje);
    }
}
