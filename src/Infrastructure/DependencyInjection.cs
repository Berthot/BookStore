using Application.Abstractions.Messaging;
using Application.UseCases.Sales.Ports;
using Infrastructure.Adapters;
using Infrastructure.Extensions;
using Infrastructure.Messaging;
using Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers all BookStore.Api infrastructure: BookStore persistence, HTTP fraud gateway, BookStore MassTransit bus and telemetry.</summary>
    public static IServiceCollection AddBookStoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddBookStorePersistence(configuration)
            .AddBookStoreMassTransit(configuration)
            .AddTelemetry(configuration);

        services.AddScoped<IEventPublisher, EventPublisher>();

        services.AddOptions<ReconciliationOptions>()
            .BindConfiguration(ReconciliationOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // HttpFraudCheckGateway: typed client with resilience; base address is resolved via
        // Aspire service discovery (http+https://fraud-api/) or the Services__ env vars in compose.
        services.AddHttpClient<IFraudCheckGateway, HttpFraudCheckGateway>(client =>
            client.BaseAddress = new Uri("http+https://fraud-api/"))
            .AddStandardResilienceHandler();

        return services;
    }

    /// <summary>Registers all Fraud.Api / Fraud.Worker infrastructure: Fraud persistence, Fraud MassTransit bus and telemetry.</summary>
    public static IServiceCollection AddFraudInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddFraudPersistence(configuration)
            .AddFraudMassTransit(configuration)
            .AddTelemetry(configuration);

        services.AddScoped<IEventPublisher, EventPublisher>();

        services.AddOptions<DemoOptions>()
            .BindConfiguration(DemoOptions.SectionName);

        return services;
    }

    /// <summary>Registers all infrastructure services for both bounded contexts — used by the legacy monolith and shared integration tests.</summary>
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
