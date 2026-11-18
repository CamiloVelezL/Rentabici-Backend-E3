using RentabiciBackend.Dtos;
using RentabiciBackend.Exceptions;

namespace RentabiciBackend.Middleware;

/// <summary>
/// Manejo controlado de errores: toda excepción se convierte en JSON { codigo, mensaje }.
/// Las AppException son flujos alternativos esperados (4xx); cualquier otra es un 500
/// genérico que no expone detalles internos al cliente.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            _logger.LogWarning("Flujo alternativo {Codigo} en {Ruta}: {Mensaje}", ex.Codigo, context.Request.Path, ex.Message);
            await EscribirAsync(context, ex.StatusCode, new ErrorResponse(ex.Codigo, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Ruta}", context.Request.Path);
            await EscribirAsync(context, 500, new ErrorResponse("ERROR_INTERNO", "Ocurrió un error inesperado. Intente nuevamente."));
        }
    }

    private static Task EscribirAsync(HttpContext context, int status, ErrorResponse error)
    {
        if (context.Response.HasStarted) return Task.CompletedTask;
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(error);
    }
}
