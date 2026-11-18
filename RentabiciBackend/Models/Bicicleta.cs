using System.Text.Json.Serialization;
using RentabiciBackend.Domain.Estados;

namespace RentabiciBackend.Models;

/// <summary>
/// Entidad Bicicleta. Su comportamiento depende del estado actual y se delega
/// a las clases del patrón State (Domain/Estados). Ninguna capa cambia
/// <see cref="Estado"/> directamente: siempre se usa uno de los métodos de transición.
/// </summary>
public class Bicicleta
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Estación donde está anclada. Es null mientras está alquilada (en viaje).</summary>
    public int? EstacionId { get; set; }

    public EstadoBicicleta Estado { get; set; } = EstadoBicicleta.Disponible;

    /// <summary>Usuarios distintos que reportaron falla desde la última liberación (RN-05).</summary>
    [JsonIgnore]
    public HashSet<int> ReportantesRecientes { get; set; } = new();

    public int ReportesConsecutivos => ReportantesRecientes.Count;

    /// <summary>Se reportó una falla durante el viaje; se aplica al devolverla.</summary>
    public bool FallaPendiente { get; set; }
    public bool FallaGravePendiente { get; set; }

    private IBikeState EstadoActual => BikeStateFactory.Obtener(Estado);

    // ---- Transiciones (eventos de la máquina de estados) ----
    public void Reservar() => Estado = EstadoActual.Reservar(this);
    public void CancelarReserva() => Estado = EstadoActual.CancelarReserva(this);
    public void Alquilar() => Estado = EstadoActual.Alquilar(this);
    public void Devolver() => Estado = EstadoActual.Devolver(this);
    public void ReportarFalla(bool requiereMantenimiento) => Estado = EstadoActual.ReportarFalla(this, requiereMantenimiento);
    public void EnviarAMantenimiento() => Estado = EstadoActual.EnviarAMantenimiento(this);
    public void Liberar() => Estado = EstadoActual.Liberar(this);
    public void DarDeBaja() => Estado = EstadoActual.DarDeBaja(this);
}
