using RentabiciBackend.Services;

namespace RentabiciBackend.BackgroundJobs;

/// <summary>RN-02: cada 30 s expira las reservas con más de 15 minutos y libera la bicicleta.</summary>
public class ReservaExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ReservaExpirationService> _logger;

    public ReservaExpirationService(IServiceScopeFactory scopes, ILogger<ReservaExpirationService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var reservas = scope.ServiceProvider.GetRequiredService<IReservaService>();
                var expiradas = reservas.ExpirarVencidas();
                if (expiradas > 0)
                    _logger.LogInformation("RN-02: {Cantidad} reserva(s) expirada(s) y bicicletas liberadas.", expiradas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al expirar reservas");
            }
        }
    }
}
