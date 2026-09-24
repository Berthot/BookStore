using Application.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Infrastructure.Extensions;

public static class TelemetryExtensions
{
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["OTEL_SERVICE_NAME"] ?? "bookstore";

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(FraudTelemetry.ActivitySourceName)
                    .AddSource("MassTransit")
                    .AddNpgsql()
                    .AddOtlpExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(FraudTelemetry.MeterName)
                    .AddMeter(BookStoreTelemetry.MeterName)
                    .AddMeter("MassTransit")
                    .AddOtlpExporter(); // → Aspire Dashboard

                var prometheusBase = configuration["PROMETHEUS_OTLP_ENDPOINT"];
                if (!string.IsNullOrEmpty(prometheusBase))
                    metrics.AddOtlpExporter((o, reader) =>
                    {
                        // Setting Endpoint sets AppendSignalPathToEndpoint=false internally;
                        // the full path is required so the SDK does not append /v1/metrics again.
                        o.Endpoint = new Uri(prometheusBase.TrimEnd('/') + "/api/v1/otlp/v1/metrics");
                        o.Protocol = OtlpExportProtocol.HttpProtobuf;
                        // 15s for responsive Grafana during demos (default is 60s)
                        reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 15_000;
                    });
            })
            .WithLogging(logging =>
            {
                // Logs share the same resource as tracing + metrics (from ConfigureResource above).
                logging.IncludeScopes = true;
                logging.IncludeFormattedMessage = true;
                logging.AddOtlpExporter();
            });

        return services;
    }
}
