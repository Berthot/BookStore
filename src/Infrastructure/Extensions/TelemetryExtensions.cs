using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class TelemetryExtensions
{
    /// <summary>Registers OpenTelemetry traces, metrics and logs via OTLP. Filled in TSK-0063.</summary>
    public static IServiceCollection AddTelemetry(this IServiceCollection services)
    {
        return services;
    }
}
