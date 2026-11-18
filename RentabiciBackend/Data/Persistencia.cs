using System.Text.Json;
using System.Text.Json.Serialization;
using RentabiciBackend.Models;

namespace RentabiciBackend.Data;

/// <summary>
/// Copia completa de los datos del sistema que se guarda y se carga desde el almacenamiento.
/// Incluye aparte los campos que la API nunca devuelve ([JsonIgnore]): hash de contraseñas
/// y reportantes recientes de cada bicicleta (RN-05).
/// </summary>
public class DatosGuardados
{
    public int Version { get; set; } = 1;
    public DateTime GuardadoEn { get; set; } = DateTime.UtcNow;

    public List<Usuario> Usuarios { get; set; } = new();
    public List<Estacion> Estaciones { get; set; } = new();
    public List<Bicicleta> Bicicletas { get; set; } = new();
    public List<Reserva> Reservas { get; set; } = new();
    public List<Alquiler> Alquileres { get; set; } = new();
    public List<Incidencia> Incidencias { get; set; } = new();
    public List<RegistroMantenimiento> Mantenimientos { get; set; } = new();
    public List<Factura> Facturas { get; set; } = new();
    public List<Pago> Pagos { get; set; } = new();
    public List<Notificacion> Notificaciones { get; set; } = new();

    /// <summary>idUsuario → hash PBKDF2 de la contraseña.</summary>
    public Dictionary<int, string> Claves { get; set; } = new();

    /// <summary>idBicicleta → usuarios que la reportaron desde la última liberación.</summary>
    public Dictionary<int, List<int>> Reportantes { get; set; } = new();
}

/// <summary>
/// Abstracción del almacenamiento persistente (DIP). En el MVP se implementa con un archivo
/// JSON; la base de datos SQL Server del C4 (ELEMENTO PROYECTADO) sería otra implementación
/// de esta interfaz, sin cambiar servicios ni controladores.
/// </summary>
public interface IPersistenciaDatos
{
    string Ubicacion { get; }
    DatosGuardados? Cargar();
    void Guardar(DatosGuardados datos);
}

public class ArchivoJsonPersistencia : IPersistenciaDatos
{
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ILogger<ArchivoJsonPersistencia> _logger;

    public ArchivoJsonPersistencia(IHostEnvironment entorno, IConfiguration configuracion, ILogger<ArchivoJsonPersistencia> logger)
    {
        _logger = logger;
        var relativa = configuracion["Persistencia:Archivo"] ?? "App_Data/rentabici-datos.json";
        Ubicacion = Path.Combine(entorno.ContentRootPath, relativa);
    }

    public string Ubicacion { get; }

    public DatosGuardados? Cargar()
    {
        if (!File.Exists(Ubicacion)) return null;
        try
        {
            return JsonSerializer.Deserialize<DatosGuardados>(File.ReadAllText(Ubicacion), Opciones);
        }
        catch (Exception ex)
        {
            // Archivo dañado: se conserva una copia y se arranca con los datos de prueba
            var respaldo = Ubicacion + ".danado";
            File.Copy(Ubicacion, respaldo, overwrite: true);
            _logger.LogWarning(ex, "No se pudo leer {Archivo}; se guardó una copia en {Respaldo}", Ubicacion, respaldo);
            return null;
        }
    }

    public void Guardar(DatosGuardados datos)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ubicacion)!);
        // Se escribe primero un temporal y luego se reemplaza: si el proceso se corta, el archivo no queda a medias
        var temporal = Ubicacion + ".tmp";
        File.WriteAllText(temporal, JsonSerializer.Serialize(datos, Opciones));
        File.Move(temporal, Ubicacion, overwrite: true);
    }
}
