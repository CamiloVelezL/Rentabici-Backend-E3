using RentabiciBackend.Models;

namespace RentabiciBackend.Services;

/// <summary>
/// Épica 3 – Inicio de viaje y devolución (RF-06, RF-07).
/// Interfaz común del PATRÓN PROXY: la implementan RentalManagerService (sujeto real)
/// y RentalServiceProxy (control de acceso). El controlador solo conoce esta interfaz.
/// </summary>
public interface IRentalService
{
    Alquiler IniciarAlquiler(int usuarioId, int bicicletaId);
    Alquiler Devolver(int usuarioId, int alquilerId, int? estacionDestinoId, int? minutosSimulados = null);
}
