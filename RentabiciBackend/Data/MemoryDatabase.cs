using RentabiciBackend.Models;
using RentabiciBackend.Security;

namespace RentabiciBackend.Data;

/// <summary>
/// Datos del sistema en memoria (singleton) respaldados en un almacenamiento persistente
/// (<see cref="IPersistenciaDatos"/>: archivo JSON en el MVP, SQL Server = ELEMENTO PROYECTADO).
/// Al iniciar se cargan los datos guardados; si no hay, se crean los datos de prueba.
/// Después de cada operación de la API se guardan (PersistenciaFilter).
///
/// <see cref="Lock"/> garantiza RNF-06: dos usuarios no pueden reservar/alquilar la misma
/// bicicleta al mismo tiempo, porque cada operación de escritura se ejecuta en exclusión mutua.
/// </summary>
public class MemoryDatabase
{
    public object Lock { get; } = new();

    public List<Usuario> Usuarios { get; } = new();
    public List<Estacion> Estaciones { get; } = new();
    public List<Bicicleta> Bicicletas { get; } = new();
    public List<Reserva> Reservas { get; } = new();
    public List<Alquiler> Alquileres { get; } = new();
    public List<Incidencia> Incidencias { get; } = new();
    public List<RegistroMantenimiento> Mantenimientos { get; } = new();
    public List<Factura> Facturas { get; } = new();
    public List<Pago> Pagos { get; } = new();
    public List<Notificacion> Notificaciones { get; } = new();

    public static int SiguienteId<T>(List<T> lista, Func<T, int> id) => lista.Count == 0 ? 1 : lista.Max(id) + 1;

    private readonly IPersistenciaDatos _persistencia;

    public MemoryDatabase(IPersistenciaDatos persistencia, ILogger<MemoryDatabase> logger)
    {
        _persistencia = persistencia;
        var datos = persistencia.Cargar();
        if (datos is null)
        {
            Sembrar();
            Guardar();
            logger.LogInformation("Datos de prueba creados en {Archivo}", persistencia.Ubicacion);
        }
        else
        {
            Restaurar(datos);
            logger.LogInformation("Datos cargados desde {Archivo} (guardados {Fecha})", persistencia.Ubicacion, datos.GuardadoEn.ToLocalTime());
        }
    }

    /// <summary>Guarda el estado actual en el almacenamiento persistente.</summary>
    public void Guardar()
    {
        lock (Lock)
        {
            _persistencia.Guardar(new DatosGuardados
            {
                Usuarios = Usuarios.ToList(),
                Estaciones = Estaciones.ToList(),
                Bicicletas = Bicicletas.ToList(),
                Reservas = Reservas.ToList(),
                Alquileres = Alquileres.ToList(),
                Incidencias = Incidencias.ToList(),
                Mantenimientos = Mantenimientos.ToList(),
                Facturas = Facturas.ToList(),
                Pagos = Pagos.ToList(),
                Notificaciones = Notificaciones.ToList(),
                Claves = Usuarios.ToDictionary(u => u.Id, u => u.PasswordHash),
                Reportantes = Bicicletas.Where(b => b.ReportantesRecientes.Count > 0)
                                        .ToDictionary(b => b.Id, b => b.ReportantesRecientes.ToList())
            });
        }
    }

    private void Restaurar(DatosGuardados d)
    {
        Usuarios.AddRange(d.Usuarios);
        Estaciones.AddRange(d.Estaciones);
        Bicicletas.AddRange(d.Bicicletas);
        Reservas.AddRange(d.Reservas);
        Alquileres.AddRange(d.Alquileres);
        Incidencias.AddRange(d.Incidencias);
        Mantenimientos.AddRange(d.Mantenimientos);
        Facturas.AddRange(d.Facturas);
        Pagos.AddRange(d.Pagos);
        Notificaciones.AddRange(d.Notificaciones);

        foreach (var u in Usuarios)
            if (d.Claves.TryGetValue(u.Id, out var hash)) u.PasswordHash = hash;
        foreach (var b in Bicicletas)
            if (d.Reportantes.TryGetValue(b.Id, out var ids)) b.ReportantesRecientes = ids.ToHashSet();
    }

    /// <summary>
    /// Datos de prueba (solo la primera vez). Mismos datos base que tenía el frontend (sam/sam2/sam3, Estación 1-4, bicicletas 1, 2, 4, 5)
    /// más algunos registros para demostrar flujos alternativos.
    /// </summary>
    private void Sembrar()
    {
        var clave = PasswordHasher.Hash("12345");
        Usuarios.AddRange(new[]
        {
            new Usuario { Id = 1, Nombre = "sam",   Apellidos = "",         Email = "sam",   Documento = "1000000001", PasswordHash = clave, Rol = Rol.Administrador },
            new Usuario { Id = 2, Nombre = "sam2",  Apellidos = "",         Email = "sam2",  Documento = "1000000002", PasswordHash = clave, Rol = Rol.Usuario },
            new Usuario { Id = 3, Nombre = "sam3",  Apellidos = "",         Email = "sam3",  Documento = "1000000003", PasswordHash = clave, Rol = Rol.Tecnico },
            // Demostración RN-04: tiene multa y además un viaje en curso (bicicleta 4)
            new Usuario { Id = 4, Nombre = "pedro", Apellidos = "Pérez",    Email = "pedro", Documento = "1000000004", PasswordHash = clave, Rol = Rol.Usuario, TieneMulta = true },
        });

        Estaciones.AddRange(new[]
        {
            new Estacion { Id = 1, Nombre = "Estación 1", Direccion = "Calle x1", Estado = EstadoEstacion.Abierta, Capacidad = 25 },
            new Estacion { Id = 2, Nombre = "Estación 2", Direccion = "Calle x2", Estado = EstadoEstacion.Abierta, Capacidad = 30 },
            new Estacion { Id = 3, Nombre = "Estación 3", Direccion = "Calle x3", Estado = EstadoEstacion.Abierta, Capacidad = 25 },
            new Estacion { Id = 4, Nombre = "Estación 4", Direccion = "Calle x4", Estado = EstadoEstacion.Cerrada, Capacidad = 45 },
        });

        Bicicletas.AddRange(new[]
        {
            new Bicicleta { Id = 1, Codigo = "BIC-0001", Modelo = "Modelo 1", EstacionId = 1 },
            new Bicicleta { Id = 2, Codigo = "BIC-0002", Modelo = "Modelo 1", EstacionId = 1, Estado = EstadoBicicleta.EnMantenimiento },
            new Bicicleta { Id = 4, Codigo = "BIC-0004", Modelo = "Modelo 2", EstacionId = null, Estado = EstadoBicicleta.Alquilada },
            new Bicicleta { Id = 5, Codigo = "BIC-0005", Modelo = "Modelo 1", EstacionId = 1 },
            new Bicicleta { Id = 6, Codigo = "BIC-0006", Modelo = "Modelo 2", EstacionId = 2 },
            new Bicicleta { Id = 7, Codigo = "BIC-0007", Modelo = "Modelo 1", EstacionId = 3, Estado = EstadoBicicleta.EnRevision, ReportantesRecientes = new() { 2 } },
            new Bicicleta { Id = 8, Codigo = "BIC-0008", Modelo = "Modelo 2", EstacionId = 3 },
        });

        // Viaje en curso de pedro con la bicicleta 4 (salió de la Estación 1)
        Alquileres.Add(new Alquiler
        {
            Id = 1, UsuarioId = 4, BicicletaId = 4, EstacionOrigenId = 1,
            TiempoReservado = 30, Inicio = DateTime.UtcNow.AddMinutes(-20)
        });

        Incidencias.AddRange(new[]
        {
            new Incidencia { Id = 1, BicicletaId = 2, UsuarioId = 2, EstacionId = 1, Gravedad = GravedadIncidencia.Mayor,
                             Descripcion = "Mayor", EstadoResultante = EstadoBicicleta.EnMantenimiento },
            new Incidencia { Id = 2, BicicletaId = 7, UsuarioId = 2, EstacionId = 3, Gravedad = GravedadIncidencia.Leve,
                             Descripcion = "Leve", EstadoResultante = EstadoBicicleta.EnRevision },
        });
    }
}
