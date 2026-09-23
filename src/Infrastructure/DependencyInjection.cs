using Application.Abstractions.Messaging;
using Application.UseCases.Sales.Ports;
using Infrastructure.Adapters;
using Infrastructure.Extensions;
using Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers all infrastructure services: persistence, messaging and telemetry.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddPersistence(configuration)
            .AddMessaging(configuration)
            .AddTelemetry(configuration);

        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddScoped<IFraudCheckGateway, FraudCheckGateway>();

        return services;
    }
}
