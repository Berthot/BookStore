using Application.Diagnostics;
using Infrastructure.Persistence.BookStore;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Jobs;

public sealed class BookStoreMetricsJob(
    IServiceScopeFactory scopeFactory,
    ILogger<BookStoreMetricsJob> logger) : BackgroundService
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

                var db = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
                var outboxPending = await db.Set<OutboxMessage>().LongCountAsync(stoppingToken);
                BookStoreTelemetry.SetOutboxPending(outboxPending);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error updating BookStore metrics gauges.");
            }
        }
    }
}
