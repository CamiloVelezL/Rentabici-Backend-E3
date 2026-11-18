using Microsoft.AspNetCore.Mvc.Filters;
using RentabiciBackend.Data;

namespace RentabiciBackend.Filters;

/// <summary>
/// Después de cada acción de la API que termina sin error, guarda los datos en el almacenamiento
/// persistente. Si la acción falla (flujo alternativo), no se guarda nada.
/// </summary>
public class PersistenciaFilter : IActionFilter
{
    private readonly MemoryDatabase _db;
    public PersistenciaFilter(MemoryDatabase db) => _db = db;

    public void OnActionExecuting(ActionExecutingContext context) { }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception is null) _db.Guardar();
    }
}
