namespace api.Exchanges.Services;

public sealed class CredentialRecoveryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CredentialRecoveryService> _logger;

    public CredentialRecoveryService(IServiceScopeFactory scopeFactory, ILogger<CredentialRecoveryService> logger)
        => (_scopeFactory, _logger) = (scopeFactory, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IBybitCredentialSetService>().ReconcileAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Credential recovery reconciliation failed"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
