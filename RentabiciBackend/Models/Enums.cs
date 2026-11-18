namespace RentabiciBackend.Models;

/// <summary>Roles del sistema (RF-02). Los números coinciden con los idRol del frontend.</summary>
public enum Rol
{
    Administrador = 1,
    Tecnico = 2,
    Usuario = 3
}

public enum EstadoEstacion
{
    Abierta,
    Cerrada
}

/// <summary>Estados del ciclo de vida de la bicicleta (máquina de estados UML / patrón State).</summary>
public enum EstadoBicicleta
{
    Disponible,
    Reservada,
    Alquilada,
    EnRevision,
    EnMantenimiento,
    DadaDeBaja
}

public enum EstadoReserva
{
    Activa,
    Completada,   // se recibió la bicicleta y empezó el alquiler
    Cancelada,
    Expirada      // RN-02: 15 minutos sin iniciar viaje
}

public enum EstadoAlquiler
{
    Activo,
    PendientePago,
    Pagado
}

public enum EstadoFactura
{
    Pendiente,
    Pagada
}

/// <summary>Gravedades que ofrece la pantalla "Reporte de fallas".</summary>
public enum GravedadIncidencia
{
    Leve,
    Intermedia,
    Mayor
}

public enum EstadoIncidencia
{
    Pendiente,
    Atendida
}

public enum CanalNotificacion
{
    Email,
    Sms
}
