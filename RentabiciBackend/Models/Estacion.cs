namespace RentabiciBackend.Models;

public class Estacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public EstadoEstacion Estado { get; set; } = EstadoEstacion.Abierta;

    /// <summary>Capacidad máxima de anclaje (RF-03, RN-03).</summary>
    public int Capacidad { get; set; }
}
