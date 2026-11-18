using System.ComponentModel.DataAnnotations;
using RentabiciBackend.Models;

namespace RentabiciBackend.Dtos;

// =====================  RESPUESTAS  =====================

/// <summary>Formato único de error que recibe el frontend.</summary>
public record ErrorResponse(string Codigo, string Mensaje, IDictionary<string, string[]>? Errores = null);

/// <summary>Formato único de éxito: mensaje para mostrar + datos.</summary>
public record Respuesta<T>(string Mensaje, T Datos);

public record UsuarioDto(int Id, string Nombre, string Apellidos, string Email, string Documento,
                         int IdRol, Rol Rol, bool TieneMulta, decimal SaldoPendiente, bool Bloqueado)
{
    public static UsuarioDto Desde(Usuario u) =>
        new(u.Id, u.Nombre, u.Apellidos, u.Email, u.Documento, (int)u.Rol, u.Rol,
            u.TieneMulta, u.SaldoPendiente, u.EstaBloqueado);
}

public record RolDto(int IdRol, string NombreRol, string Descripcion);

public record LoginResponse(string Token, UsuarioDto Usuario);

public record EstacionResponse(int Id, string Nombre, string Direccion, EstadoEstacion Estado,
                               int Capacidad, int Ocupacion, int Disponibles, int EspaciosLibres);

public record ItemPendiente(string Tipo, decimal Monto, int? AlquilerId);

public record PendientesResponse(IEnumerable<ItemPendiente> Items, decimal Total, string? CodigoFactura);

public record PagoResultado(decimal TotalPagado, string CodigoFactura, string Comprobante);

public record AtencionResultado(Incidencia Incidencia, bool BicicletaHabilitada);

// =====================  PETICIONES  =====================

public class RegistroRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 60 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre 2 y 80 caracteres.")]
    public string Apellidos { get; set; } = string.Empty;

    /// <summary>RF-01: validación del número de identificación.</summary>
    [Required(ErrorMessage = "El documento es obligatorio.")]
    [RegularExpression(@"^\d{6,12}$", ErrorMessage = "El documento debe tener entre 6 y 12 números.")]
    public string Documento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(64, MinimumLength = 5, ErrorMessage = "La contraseña debe tener al menos 5 caracteres.")]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    /// <summary>Correo o nombre de usuario.</summary>
    [Required(ErrorMessage = "Ingrese su correo o usuario.")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña.")]
    public string Password { get; set; } = string.Empty;
}

public class CambioRolRequest
{
    [Range(1, 3, ErrorMessage = "Rol inválido (1 Administrador, 2 Técnico, 3 Usuario).")]
    public int IdRol { get; set; }
}

public class EstacionRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 80 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La ubicación es obligatoria.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "La ubicación debe tener entre 3 y 120 caracteres.")]
    public string Direccion { get; set; } = string.Empty;

    [Range(1, 200, ErrorMessage = "La cantidad debe ser un número entero entre 1 y 200.")]
    public int Capacidad { get; set; }
}

public class EstadoEstacionRequest
{
    [Required(ErrorMessage = "Indique el estado (Abierta o Cerrada).")]
    public EstadoEstacion? Estado { get; set; }
}

public class BicicletaRequest
{
    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "El modelo debe tener entre 2 y 50 caracteres.")]
    public string Modelo { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Elija una estación.")]
    public int EstacionId { get; set; }
}

public class ReubicarRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Elija una estación válida.")]
    public int EstacionId { get; set; }
}

/// <summary>Pantallas "Alquiler de bicicleta" (paso 1 y 2): el backend elige la bicicleta.</summary>
public class ReservaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Elija la estación.")]
    public int EstacionId { get; set; }

    [Required(ErrorMessage = "Elija el tipo de bicicleta.")]
    public string Modelo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique la fecha de uso.")]
    [RegularExpression(@"^\d{2}/\d{2}/\d{4}$", ErrorMessage = "La fecha debe tener el formato dd/mm/aaaa.")]
    public string FechaUso { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [RegularExpression(@"^\d{7,10}$", ErrorMessage = "El teléfono debe tener entre 7 y 10 números.")]
    public string Telefono { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Elija el tiempo de uso.")]
    public int TiempoMinutos { get; set; }
}

public class IniciarAlquilerRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Indique la bicicleta.")]
    public int BicicletaId { get; set; }
}

public class DevolverRequest
{
    /// <summary>Opcional: si no se envía, se devuelve en la estación de origen.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Estación de devolución inválida.")]
    public int? EstacionDestinoId { get; set; }

    /// <summary>Solo en Development: simula la duración del viaje para demostrar RN-06 (recargo).</summary>
    [Range(1, 1440, ErrorMessage = "Los minutos simulados deben estar entre 1 y 1440.")]
    public int? MinutosSimulados { get; set; }
}

/// <summary>Pantalla de pago: el usuario ingresa el código de su factura.</summary>
public class PagoRequest
{
    [Required(ErrorMessage = "Ingrese el código de la factura.")]
    [RegularExpression(@"^\s*[Rr][Bb]-\d{4,6}\s*$", ErrorMessage = "El código debe tener el formato RB-0000.")]
    public string CodigoFactura { get; set; } = string.Empty;
}

/// <summary>Pantalla "Reporte de fallas".</summary>
public class IncidenciaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Escriba el id de la bicicleta.")]
    public int BicicletaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Indique la estación.")]
    public int EstacionId { get; set; }

    [Required(ErrorMessage = "Elija la gravedad de la falla.")]
    public GravedadIncidencia? Gravedad { get; set; }

    [StringLength(500, ErrorMessage = "La descripción no puede superar 500 caracteres.")]
    public string? Descripcion { get; set; }
}

/// <summary>Pantalla "Fallas reportadas": el técnico marca la falla como solucionada.</summary>
public class AtenderIncidenciaRequest
{
    [StringLength(500, ErrorMessage = "El diagnóstico no puede superar 500 caracteres.")]
    public string? Diagnostico { get; set; }

    [StringLength(300)]
    public string? PiezasReemplazadas { get; set; }
}
