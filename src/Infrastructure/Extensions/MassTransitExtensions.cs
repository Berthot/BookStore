using Infrastructure.Persistence.BookStore;
using Infrastructure.Persistence.Fraud;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class MassTransitExtensions
{
    /// <summary>Retry intervals (ms) grow to ride out transient database restarts without flooding the broker.</summary>
    private static readonly int[] RetryIntervalsMs = [5_000, 30_000, 60_000, 300_000];

    /// <summary>Registers MassTransit with RabbitMQ transport, EF Core Bus Outbox and Consumer Inbox for both bounded contexts.</summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(bus =>
        {
            // Bus Outbox — messages enter the same SaveChanges as the entity change
            bus.AddEntityFrameworkOutbox<BookStoreDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.AddEntityFrameworkOutbox<FraudDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration.GetConnectionString("RabbitMQ")
                    ?? "amqp://guest:guest@localhost:5672/";

                cfg.Host(host);

                // Retry with growing intervals; exhausted messages land in the <queue>_error DLQ
                cfg.UseMessageRetry(r => r.Intervals(RetryIntervalsMs));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
