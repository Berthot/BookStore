using Application.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Infrastructure.Extensions;

public static class TelemetryExtensions
{
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["OTEL_SERVICE_NAME"] ?? "bookstore";
        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName);

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

                // Second exporter → Prometheus OTLP push (only when running under Aspire)
                var prometheusBase = configuration["PROMETHEUS_OTLP_ENDPOINT"];
                if (!string.IsNullOrEmpty(prometheusBase))
                    metrics.AddOtlpExporter(o =>
                    {
                        // OTel SDK appends /v1/metrics to the base path automatically (http/protobuf)
                        o.Endpoint = new Uri(prometheusBase.TrimEnd('/') + "/api/v1/otlp");
                        o.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
            });

        services.AddLogging(logging => logging
            .AddOpenTelemetry(otel =>
            {
                otel.SetResourceBuilder(resourceBuilder);
                otel.IncludeScopes = true;
                otel.IncludeFormattedMessage = true;
                otel.AddOtlpExporter();
            }));

        return services;
    }
}
