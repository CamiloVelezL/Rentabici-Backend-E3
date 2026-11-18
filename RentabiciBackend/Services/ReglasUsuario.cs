using RentabiciBackend.Data;
using RentabiciBackend.Exceptions;
using RentabiciBackend.Models;

namespace RentabiciBackend.Services;

/// <summary>Reglas de negocio sobre el usuario, compartidas por reservas y alquileres.</summary>
public static class ReglasUsuario
{
    /// <summary>RN-04: bloqueo por mora o infracción.</summary>
    public static void ValidarNoBloqueado(Usuario u)
    {
        if (u.TieneMulta)
            throw new BusinessRuleException("USUARIO_CON_MULTA",
                "Tiene una multa pendiente. Páguela para poder reservar o alquilar.", 403);

        if (u.SaldoPendiente > 0)
            throw new BusinessRuleException("USUARIO_CON_SALDO",
                $"Tiene un saldo pendiente de {u.SaldoPendiente:N0} COP. Pague su último alquiler para continuar.", 403);
    }

    /// <summary>
    /// RN-01: una sola bicicleta alquilada o reservada a la vez.
    /// <paramref name="bicicletaPermitida"/>: si el usuario tiene reservada justo esa bicicleta, puede alquilarla.
    /// </summary>
    public static void ValidarSinOperacionActiva(MemoryDatabase db, int usuarioId, int? bicicletaPermitida = null)
    {
        if (db.Alquileres.Any(a => a.UsuarioId == usuarioId && a.Estado == EstadoAlquiler.Activo))
            throw new BusinessRuleException("LIMITE_ACTIVO", "Ya tiene un viaje en curso. Devuelva la bicicleta antes de tomar otra.");

        var reserva = db.Reservas.FirstOrDefault(r => r.UsuarioId == usuarioId && r.Estado == EstadoReserva.Activa);
        if (reserva is not null && reserva.BicicletaId != bicicletaPermitida)
            throw new BusinessRuleException("LIMITE_ACTIVO",
                $"Ya tiene una reserva activa (bicicleta {reserva.BicicletaId}). Cancélela o úsela primero.");
    }
}
