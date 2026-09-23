using Application.UseCases.Sales.ReconcilePendingPurchases;
using Cortex.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Worker.Jobs;

public sealed class ReconciliationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ReconciliationJob> logger) : BackgroundService
{
    /// <summary>How often to scan for stale purchases.</summary>
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    /// <summary>Purchases stuck in PendingFraudCheck beyond this age are re-submitted.</summary>
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await mediator.SendCommandAsync(
                    new ReconcilePendingPurchasesRequest(DateTime.UtcNow - StalenessThreshold),
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during purchase reconciliation iteration.");
            }
        }
    }
}
