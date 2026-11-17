using RentabiciBackend.Data;
namespace RentabiciBackend.Services;
public class RentalManagerService
{
    private readonly MemoryDatabase _db;
    public RentalManagerService(MemoryDatabase db) { _db = db; }

    public string ProcesarAlquiler(int usuarioId, int bicicletaId)
    {
        var bici = _db.Bicicletas.FirstOrDefault(b => b.Id == bicicletaId);
        if (bici == null) return "Bicicleta no encontrada";
        if (bici.Estado != "Disponible") return "Bicicleta no disponible";

        bici.Estado = "Alquilada";
        return "Alquiler exitoso";
    }
}