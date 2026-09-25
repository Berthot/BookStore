using Application.Diagnostics;
using Domain.Repositories;
using Infrastructure.Persistence.Fraud;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Fraud.Api.Jobs;

public sealed class FraudMetricsJob(
    IServiceScopeFactory scopeFactory,
    ILogger<FraudMetricsJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
                var pendingReviews = await repo.CountPendingReviewAsync(stoppingToken);
                FraudTelemetry.SetReviewsPending(pendingReviews);

                var db = scope.ServiceProvider.GetRequiredService<FraudDbContext>();
                var outboxPending = await db.Set<OutboxMessage>().LongCountAsync(stoppingToken);
                FraudTelemetry.SetOutboxPending(outboxPending);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error updating fraud metrics gauges.");
            }
        }
    }
}
