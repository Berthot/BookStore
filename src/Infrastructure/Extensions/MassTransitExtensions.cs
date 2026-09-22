using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class MassTransitExtensions
{
    /// <summary>Registers MassTransit 8.x with RabbitMQ transport, Bus Outbox and Consumer Inbox. Filled in TSK-0071.</summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        return services;
    }
}
