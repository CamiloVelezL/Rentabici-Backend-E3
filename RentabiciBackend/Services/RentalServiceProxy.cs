using RentabiciBackend.Data;
namespace RentabiciBackend.Services;
public class RentalServiceProxy : IRentalService
{
    private readonly RentalManagerService _manager;
    private readonly MemoryDatabase _db;

    public RentalServiceProxy(RentalManagerService manager, MemoryDatabase db)
    {
        _manager = manager; _db = db;
    }

    public string ProcesarAlquiler(int usuarioId, int bicicletaId)
    {
        var user = _db.Usuarios.FirstOrDefault(u => u.Id == usuarioId);
        if (user != null && user.TieneMulta)
        {
            return "ERROR: Usuario con multas pendientes";
        }
        return _manager.ProcesarAlquiler(usuarioId, bicicletaId);
    }
}
