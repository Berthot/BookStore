using Application.UseCases.Idempotency.PurgeExpiredKeys;
using Cortex.Mediator;

namespace Fraud.Worker.Jobs;

public sealed class IdempotencyPurgeJob(
    IServiceScopeFactory scopeFactory,
    ILogger<IdempotencyPurgeJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan ExpiryAge = TimeSpan.FromHours(24);

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
                    new PurgeExpiredKeysRequest(DateTime.UtcNow - ExpiryAge),
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during idempotency key purge iteration.");
            }
        }
    }
}
