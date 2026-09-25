using System.Reflection;
using Infrastructure.Messaging;
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

    /// <summary>Registers MassTransit for BookStore.Api: BookStoreDbContext outbox with UseBusOutbox, consumers from entry assembly.</summary>
    public static IServiceCollection AddBookStoreMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(bus =>
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly is not null)
                bus.AddConsumers(entryAssembly);

            bus.AddEntityFrameworkOutbox<BookStoreDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMQ")
                    ?? throw new InvalidOperationException("ConnectionStrings__RabbitMQ is required."));
                cfg.UseMessageRetry(r => r.Intervals(RetryIntervalsMs));
                cfg.UseConsumeFilter(typeof(LoggingScopeConsumeFilter<>), context);
                // UseEntityFrameworkOutbox is applied to every receive endpoint automatically
                // by the IBusObserver registered when UseBusOutbox() is set above.
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>Registers MassTransit for Fraud.Api and Fraud.Worker: FraudDbContext outbox with UseBusOutbox, consumers from entry assembly.</summary>
    public static IServiceCollection AddFraudMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(bus =>
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly is not null)
                bus.AddConsumers(entryAssembly);

            bus.AddEntityFrameworkOutbox<FraudDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMQ")
                    ?? throw new InvalidOperationException("ConnectionStrings__RabbitMQ is required."));
                cfg.UseMessageRetry(r => r.Intervals(RetryIntervalsMs));
                cfg.UseConsumeFilter(typeof(LoggingScopeConsumeFilter<>), context);
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>Registers MassTransit with both outboxes — kept for tests/scenarios that host both contexts in one process.</summary>
    internal static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(bus =>
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly is not null)
                bus.AddConsumers(entryAssembly);

            // Each outbox is registered without UseBusOutbox here because both
            // contexts share one bus instance; the per-process split methods above
            // enable UseBusOutbox safely (one outbox per bus).
            bus.AddEntityFrameworkOutbox<BookStoreDbContext>(outbox => { outbox.UsePostgres(); });
            bus.AddEntityFrameworkOutbox<FraudDbContext>(outbox => { outbox.UsePostgres(); });

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMQ")
                    ?? throw new InvalidOperationException("ConnectionStrings__RabbitMQ is required."));
                cfg.UseMessageRetry(r => r.Intervals(RetryIntervalsMs));
                cfg.UseConsumeFilter(typeof(LoggingScopeConsumeFilter<>), context);
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
