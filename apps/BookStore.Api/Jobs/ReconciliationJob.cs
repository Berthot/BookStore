using Application.UseCases.Sales.ReconcilePendingPurchases;
using Cortex.Mediator;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace BookStore.Api.Jobs;

public sealed class ReconciliationJob(
    IServiceScopeFactory scopeFactory,
    IOptions<ReconciliationOptions> options,
    ILogger<ReconciliationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        var interval = TimeSpan.FromSeconds(opts.IntervalSeconds);
        var stalenessThreshold = TimeSpan.FromSeconds(opts.StalenessThresholdSeconds);

        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await mediator.SendCommandAsync(
                    new ReconcilePendingPurchasesRequest(DateTime.UtcNow - stalenessThreshold),
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during purchase reconciliation iteration.");
            }
        }
    }
}
