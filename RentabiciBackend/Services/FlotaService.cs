using RentabiciBackend.Data;
using RentabiciBackend.Dtos;
using RentabiciBackend.Exceptions;
using RentabiciBackend.Models;

namespace RentabiciBackend.Services;

/// <summary>Épica 2 – Estaciones (RF-03).</summary>
public interface IEstacionService
{
    IEnumerable<EstacionResponse> Listar();
    EstacionResponse Obtener(int id);
    EstacionResponse Crear(EstacionRequest r);
    EstacionResponse CambiarEstado(int id, EstadoEstacion estado);
}

/// <summary>Épica 2 – Inventario de bicicletas (RF-04).</summary>
public interface IBicicletaService
{
    IEnumerable<Bicicleta> Listar(int? estacionId, EstadoBicicleta? estado);
    Bicicleta Obtener(int id);
    Bicicleta Crear(BicicletaRequest r);
    Bicicleta Reubicar(int id, int estacionId);
}

/// <summary>Reglas de estación compartidas por varios servicios (RN-03 y estación cerrada).</summary>
public static class ReglasEstacion
{
    public static int Ancladas(MemoryDatabase db, int estacionId) =>
        db.Bicicletas.Count(b => b.EstacionId == estacionId && b.Estado != EstadoBicicleta.DadaDeBaja);

    /// <summary>RN-03: no se ancla una bicicleta en una estación llena.</summary>
    public static void ValidarCapacidad(MemoryDatabase db, Estacion estacion)
    {
        if (Ancladas(db, estacion.Id) < estacion.Capacidad) return;

        var alternativas = db.Estaciones
            .Where(e => e.Id != estacion.Id && e.Estado == EstadoEstacion.Abierta && Ancladas(db, e.Id) < e.Capacidad)
            .Select(e => e.Nombre)
            .ToList();
        var sugerencia = alternativas.Count > 0 ? $" Estaciones con espacio: {string.Join(", ", alternativas)}." : string.Empty;
        throw new BusinessRuleException("ESTACION_LLENA",
            $"{estacion.Nombre} está llena (capacidad {estacion.Capacidad}).{sugerencia}");
    }

    public static void ValidarAbierta(Estacion estacion)
    {
        if (estacion.Estado == EstadoEstacion.Cerrada)
            throw new BusinessRuleException("ESTACION_CERRADA", $"{estacion.Nombre} está cerrada en este momento.");
    }

    public static Estacion Buscar(MemoryDatabase db, int id) =>
        db.Estaciones.FirstOrDefault(e => e.Id == id) ?? throw new NotFoundException("La estación", id);
}

public class EstacionService : IEstacionService
{
    private readonly MemoryDatabase _db;
    public EstacionService(MemoryDatabase db) => _db = db;

    public IEnumerable<EstacionResponse> Listar()
    {
        lock (_db.Lock) return _db.Estaciones.Select(Mapear).ToList();
    }

    public EstacionResponse Obtener(int id)
    {
        lock (_db.Lock) return Mapear(ReglasEstacion.Buscar(_db, id));
    }

    public EstacionResponse Crear(EstacionRequest r)
    {
        lock (_db.Lock)
        {
            var nombre = r.Nombre.Trim();
            if (_db.Estaciones.Any(e => e.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase)))
                throw new BusinessRuleException("ESTACION_DUPLICADA", "Ya existe una estación con ese nombre.");

            var e = new Estacion
            {
                Id = MemoryDatabase.SiguienteId(_db.Estaciones, x => x.Id),
                Nombre = nombre,
                Direccion = r.Direccion.Trim(),
                Capacidad = r.Capacidad,
                Estado = EstadoEstacion.Abierta // las estaciones nuevas nacen abiertas
            };
            _db.Estaciones.Add(e);
            return Mapear(e);
        }
    }

    public EstacionResponse CambiarEstado(int id, EstadoEstacion estado)
    {
        lock (_db.Lock)
        {
            var e = ReglasEstacion.Buscar(_db, id);
            if (estado == EstadoEstacion.Cerrada &&
                _db.Reservas.Any(x => x.EstacionId == id && x.Estado == EstadoReserva.Activa))
                throw new BusinessRuleException("ESTACION_CON_RESERVAS",
                    $"{e.Nombre} tiene reservas activas; no se puede cerrar hasta que terminen.");

            e.Estado = estado;
            return Mapear(e);
        }
    }

    private EstacionResponse Mapear(Estacion e)
    {
        var ancladas = ReglasEstacion.Ancladas(_db, e.Id);
        var disponibles = _db.Bicicletas.Count(b => b.EstacionId == e.Id && b.Estado == EstadoBicicleta.Disponible);
        return new EstacionResponse(e.Id, e.Nombre, e.Direccion, e.Estado, e.Capacidad, ancladas, disponibles, e.Capacidad - ancladas);
    }
}

public class BicicletaService : IBicicletaService
{
    private readonly MemoryDatabase _db;
    public BicicletaService(MemoryDatabase db) => _db = db;

    public IEnumerable<Bicicleta> Listar(int? estacionId, EstadoBicicleta? estado)
    {
        lock (_db.Lock)
        {
            return _db.Bicicletas
                .Where(b => estacionId is null || b.EstacionId == estacionId)
                .Where(b => estado is null || b.Estado == estado)
                .OrderBy(b => b.Id)
                .ToList();
        }
    }

    public Bicicleta Obtener(int id)
    {
        lock (_db.Lock) return Buscar(id);
    }

    public Bicicleta Crear(BicicletaRequest r)
    {
        lock (_db.Lock)
        {
            var estacion = ReglasEstacion.Buscar(_db, r.EstacionId);
            ReglasEstacion.ValidarCapacidad(_db, estacion); // RN-03

            var id = MemoryDatabase.SiguienteId(_db.Bicicletas, x => x.Id);
            var b = new Bicicleta
            {
                Id = id,
                Codigo = $"BIC-{id:D4}",
                Modelo = r.Modelo.Trim(),
                EstacionId = estacion.Id
            };
            _db.Bicicletas.Add(b);
            return b;
        }
    }

    public Bicicleta Reubicar(int id, int estacionId)
    {
        lock (_db.Lock)
        {
            var b = Buscar(id);
            if (b.Estado is EstadoBicicleta.Alquilada or EstadoBicicleta.Reservada or EstadoBicicleta.DadaDeBaja)
                throw new BusinessRuleException("REUBICACION_NO_PERMITIDA",
                    $"No se puede reubicar la bicicleta {b.Codigo} en estado '{b.Estado}'.");
            if (b.EstacionId == estacionId)
                throw new BusinessRuleException("MISMA_ESTACION", "La bicicleta ya está en esa estación.");

            var destino = ReglasEstacion.Buscar(_db, estacionId);
            ReglasEstacion.ValidarCapacidad(_db, destino); // RN-03
            b.EstacionId = destino.Id;
            return b;
        }
    }

    private Bicicleta Buscar(int id) =>
        _db.Bicicletas.FirstOrDefault(b => b.Id == id) ?? throw new NotFoundException("La bicicleta", id);
}
