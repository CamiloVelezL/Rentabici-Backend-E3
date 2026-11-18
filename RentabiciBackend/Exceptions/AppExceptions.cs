namespace RentabiciBackend.Exceptions;

/// <summary>
/// Excepción base controlada. El middleware de errores la convierte en una
/// respuesta JSON { codigo, mensaje } con el código HTTP indicado.
/// </summary>
public abstract class AppException : Exception
{
    public string Codigo { get; }
    public int StatusCode { get; }

    protected AppException(string codigo, string mensaje, int statusCode) : base(mensaje)
    {
        Codigo = codigo;
        StatusCode = statusCode;
    }
}

/// <summary>Violación de una regla de negocio (RN-01…RN-06) o transición de estado inválida. HTTP 409 por defecto.</summary>
public class BusinessRuleException : AppException
{
    public BusinessRuleException(string codigo, string mensaje, int statusCode = 409)
        : base(codigo, mensaje, statusCode) { }
}

/// <summary>Recurso inexistente. HTTP 404.</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string recurso, int id)
        : base("NO_ENCONTRADO", $"{recurso} con id {id} no existe.", 404) { }
}

/// <summary>Datos de entrada inválidos detectados en el servicio. HTTP 400.</summary>
public class ValidacionException : AppException
{
    public ValidacionException(string mensaje) : base("VALIDACION", mensaje, 400) { }
}

/// <summary>Sin sesión o credenciales incorrectas. HTTP 401.</summary>
public class NoAutenticadoException : AppException
{
    public NoAutenticadoException(string codigo = "NO_AUTENTICADO", string mensaje = "Debe iniciar sesión.")
        : base(codigo, mensaje, 401) { }
}

/// <summary>El usuario no puede operar sobre el recurso. HTTP 403.</summary>
public class AccesoDenegadoException : AppException
{
    public AccesoDenegadoException(string mensaje) : base("ACCESO_DENEGADO", mensaje, 403) { }
}
