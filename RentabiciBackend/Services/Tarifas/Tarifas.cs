using Microsoft.Extensions.Options;

namespace RentabiciBackend.Services.Tarifas;

/// <summary>Valores de RN-06 configurables en appsettings.json → "Tarifas".</summary>
public class TarifaOptions
{
    public decimal ValorBloque { get; set; } = 3000;     // COP por cada 30 minutos o fracción
    public int MinutosBloque { get; set; } = 30;
    public int MinutosLimite { get; set; } = 120;        // tiempo límite permitido (2 horas)
    public decimal RecargoPorMinuto { get; set; } = 200; // COP por minuto de demora
    public decimal ValorMulta { get; set; } = 10000;     // infracción pendiente (RN-04)
}

/// <summary>Resultado del cálculo: lo que se muestra como "Alquiler" y como "Multa" en Pagos.</summary>
public record DesgloseTarifa(decimal Alquiler, decimal Recargo)
{
    public decimal Total => Alquiler + Recargo;
}

/// <summary>
/// PATRÓN STRATEGY (GoF, comportamiento): cada algoritmo de cobro es intercambiable.
/// Para agregar una tarifa (p. ej. membresía) se crea otra clase sin modificar RentalManagerService (OCP).
/// </summary>
public interface ITarifaStrategy
{
    string Nombre { get; }
    DesgloseTarifa Calcular(int minutos);
}

/// <summary>RN-06: bloques de 30 minutos o fracción (mínimo un bloque).</summary>
public class TarifaEstandarStrategy : ITarifaStrategy
{
    private readonly TarifaOptions _o;
    public TarifaEstandarStrategy(IOptions<TarifaOptions> opciones) => _o = opciones.Value;

    public string Nombre => "Estándar";

    public DesgloseTarifa Calcular(int minutos)
    {
        var bloques = Math.Max(1, (int)Math.Ceiling(minutos / (double)_o.MinutosBloque));
        return new DesgloseTarifa(bloques * _o.ValorBloque, 0);
    }
}

/// <summary>RN-06 / RF-11: superado el tiempo límite se suma un recargo por cada minuto de demora.</summary>
public class TarifaConRecargoStrategy : ITarifaStrategy
{
    private readonly TarifaOptions _o;
    private readonly TarifaEstandarStrategy _estandar;

    public TarifaConRecargoStrategy(IOptions<TarifaOptions> opciones, TarifaEstandarStrategy estandar)
    {
        _o = opciones.Value;
        _estandar = estandar;
    }

    public string Nombre => "Estándar + recargo por demora";

    public DesgloseTarifa Calcular(int minutos)
    {
        var baseAlquiler = _estandar.Calcular(minutos).Alquiler;
        var demora = Math.Max(0, minutos - _o.MinutosLimite);
        return new DesgloseTarifa(baseAlquiler, demora * _o.RecargoPorMinuto);
    }
}

/// <summary>Contexto que elige la estrategia en tiempo de ejecución.</summary>
public interface ITarifaSelector
{
    ITarifaStrategy Seleccionar(int minutos);
}

public class TarifaSelector : ITarifaSelector
{
    private readonly TarifaEstandarStrategy _estandar;
    private readonly TarifaConRecargoStrategy _recargo;
    private readonly TarifaOptions _o;

    public TarifaSelector(TarifaEstandarStrategy estandar, TarifaConRecargoStrategy recargo, IOptions<TarifaOptions> opciones)
    {
        _estandar = estandar;
        _recargo = recargo;
        _o = opciones.Value;
    }

    public ITarifaStrategy Seleccionar(int minutos) => minutos > _o.MinutosLimite ? _recargo : _estandar;
}
