using RentabiciBackend.Exceptions;
using RentabiciBackend.Models;

namespace RentabiciBackend.Domain.Estados;

/// <summary>
/// PATRÓN STATE (GoF, comportamiento).
/// Cada estado de la bicicleta decide qué eventos acepta y a qué estado pasa.
/// Un evento no permitido lanza BusinessRuleException("TRANSICION_INVALIDA"),
/// que es uno de los flujos alternativos del MVP (p. ej. alquilar una bici en mantenimiento).
/// Esta clase es la implementación directa de la máquina de estados UML del Entregable 3.
/// </summary>
public interface IBikeState
{
    EstadoBicicleta Nombre { get; }
    EstadoBicicleta Reservar(Bicicleta b);
    EstadoBicicleta CancelarReserva(Bicicleta b);
    EstadoBicicleta Alquilar(Bicicleta b);
    EstadoBicicleta Devolver(Bicicleta b);
    EstadoBicicleta ReportarFalla(Bicicleta b, bool requiereMantenimiento);
    EstadoBicicleta EnviarAMantenimiento(Bicicleta b);
    EstadoBicicleta Liberar(Bicicleta b);
    EstadoBicicleta DarDeBaja(Bicicleta b);
}

/// <summary>Por defecto toda transición es inválida; cada estado concreto habilita solo las suyas (OCP).</summary>
public abstract class BikeStateBase : IBikeState
{
    public abstract EstadoBicicleta Nombre { get; }

    public virtual EstadoBicicleta Reservar(Bicicleta b) => Invalida(b, "reservar");
    public virtual EstadoBicicleta CancelarReserva(Bicicleta b) => Invalida(b, "cancelar la reserva de");
    public virtual EstadoBicicleta Alquilar(Bicicleta b) => Invalida(b, "alquilar");
    public virtual EstadoBicicleta Devolver(Bicicleta b) => Invalida(b, "devolver");
    public virtual EstadoBicicleta ReportarFalla(Bicicleta b, bool requiereMantenimiento) => Invalida(b, "reportar una falla en");
    public virtual EstadoBicicleta EnviarAMantenimiento(Bicicleta b) => Invalida(b, "enviar a mantenimiento");
    public virtual EstadoBicicleta Liberar(Bicicleta b) => Invalida(b, "liberar");
    public virtual EstadoBicicleta DarDeBaja(Bicicleta b) => Invalida(b, "dar de baja");

    protected EstadoBicicleta Invalida(Bicicleta b, string accion) =>
        throw new BusinessRuleException(
            "TRANSICION_INVALIDA",
            $"No se puede {accion} la bicicleta {b.Codigo}: su estado actual es '{Nombre}'.");

    /// <summary>Guarda [requiereMantenimiento]: grave o RN-05 (2+ reportes) → EnMantenimiento; si no → EnRevision.</summary>
    protected static EstadoBicicleta DestinoPorFalla(bool requiereMantenimiento) =>
        requiereMantenimiento ? EstadoBicicleta.EnMantenimiento : EstadoBicicleta.EnRevision;
}

public sealed class DisponibleState : BikeStateBase
{
    public override EstadoBicicleta Nombre => EstadoBicicleta.Disponible;
    public override EstadoBicicleta Reservar(Bicicleta b) => EstadoBicicleta.Reservada;
    public override EstadoBicicleta Alquilar(Bicicleta b) => EstadoBicicleta.Alquilada;
    public override EstadoBicicleta ReportarFalla(Bicicleta b, bool requiereMantenimiento) => DestinoPorFalla(requiereMantenimiento);
    public override EstadoBicicleta EnviarAMantenimiento(Bicicleta b) => EstadoBicicleta.EnMantenimiento;
    public override EstadoBicicleta DarDeBaja(Bicicleta b) => EstadoBicicleta.DadaDeBaja;
}

public sealed class ReservadaState : BikeStateBase
{
    public override EstadoBicicleta Nombre => EstadoBicicleta.Reservada;
    public override EstadoBicicleta CancelarReserva(Bicicleta b) => EstadoBicicleta.Disponible;   // cancelación o RN-02 (expiración)
    public override EstadoBicicleta Alquilar(Bicicleta b) => EstadoBicicleta.Alquilada;
    public override EstadoBicicleta ReportarFalla(Bicicleta b, bool requiereMantenimiento) => DestinoPorFalla(requiereMantenimiento);
}

public sealed class AlquiladaState : BikeStateBase
{
    public override EstadoBicicleta Nombre => EstadoBicicleta.Alquilada;

    /// <summary>
    /// Al devolver: si durante el viaje se reportó falla, la bici no vuelve a Disponible.
    /// Guardas: [falla grave] → EnMantenimiento, [falla] → EnRevision, [sin falla] → Disponible.
    /// </summary>
    public override EstadoBicicleta Devolver(Bicicleta b)
    {
        var destino = b.FallaGravePendiente ? EstadoBicicleta.EnMantenimiento
                    : b.FallaPendiente ? EstadoBicicleta.EnRevision
                    : EstadoBicicleta.Disponible;

        if (destino == EstadoBicicleta.Disponible)
            b.ReportantesRecientes.Clear(); // un viaje sin fallas reinicia los reportes "consecutivos" (RN-05)

        b.FallaPendiente = false;
        b.FallaGravePendiente = false;
        return destino;
    }

    /// <summary>Reporte en pleno viaje: la bici sigue Alquilada y la falla se aplica al devolverla.</summary>
    public override EstadoBicicleta ReportarFalla(Bicicleta b, bool requiereMantenimiento)
    {
        b.FallaPendiente = true;
        if (requiereMantenimiento) b.FallaGravePendiente = true;
        return EstadoBicicleta.Alquilada;
    }
}

public sealed class EnRevisionState : BikeStateBase
{
    public override EstadoBicicleta Nombre => EstadoBicicleta.EnRevision;
    public override EstadoBicicleta ReportarFalla(Bicicleta b, bool requiereMantenimiento) => DestinoPorFalla(requiereMantenimiento);
    public override EstadoBicicleta EnviarAMantenimiento(Bicicleta b) => EstadoBicicleta.EnMantenimiento;

    /// <summary>El técnico revisa y no encuentra falla.</summary>
    public override EstadoBicicleta Liberar(Bicicleta b)
    {
        b.ReportantesRecientes.Clear();
        return EstadoBicicleta.Disponible;
    }

    public override EstadoBicicleta DarDeBaja(Bicicleta b) => EstadoBicicleta.DadaDeBaja;
}

public sealed class EnMantenimientoState : BikeStateBase
{
    public override EstadoBicicleta Nombre => EstadoBicicleta.EnMantenimiento;
    public override EstadoBicicleta ReportarFalla(Bicicleta b, bool requiereMantenimiento) => EstadoBicicleta.EnMantenimiento;

    /// <summary>RF-09: el técnico registra la reparación y da el alta operativa.</summary>
    public override EstadoBicicleta Liberar(Bicicleta b)
    {
        b.ReportantesRecientes.Clear();
        return EstadoBicicleta.Disponible;
    }

    public override EstadoBicicleta DarDeBaja(Bicicleta b) => EstadoBicicleta.DadaDeBaja;
}

/// <summary>Estado final: no acepta ningún evento.</summary>
public sealed class DadaDeBajaState : BikeStateBase
{
    public override EstadoBicicleta Nombre => EstadoBicicleta.DadaDeBaja;
}

public static class BikeStateFactory
{
    private static readonly Dictionary<EstadoBicicleta, IBikeState> Estados = new()
    {
        [EstadoBicicleta.Disponible] = new DisponibleState(),
        [EstadoBicicleta.Reservada] = new ReservadaState(),
        [EstadoBicicleta.Alquilada] = new AlquiladaState(),
        [EstadoBicicleta.EnRevision] = new EnRevisionState(),
        [EstadoBicicleta.EnMantenimiento] = new EnMantenimientoState(),
        [EstadoBicicleta.DadaDeBaja] = new DadaDeBajaState(),
    };

    public static IBikeState Obtener(EstadoBicicleta estado) => Estados[estado];
}
