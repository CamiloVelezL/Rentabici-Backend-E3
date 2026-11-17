using RentabiciBackend.Models;
namespace RentabiciBackend.Data;
public class MemoryDatabase
{
    public List<Bicicleta> Bicicletas { get; set; } = new()
    {
        new() { Id = 1, Estado = "Disponible" },
        new() { Id = 2, Estado = "EnMantenimiento" }
    };
    public List<Usuario> Usuarios { get; set; } = new()
    {
        new() { Id = 1, Nombre = "Juan", TieneMulta = false },
        new() { Id = 2, Nombre = "Pedro", TieneMulta = true }
    };
    public List<Incidencia> Incidencias { get; set; } = new();
}