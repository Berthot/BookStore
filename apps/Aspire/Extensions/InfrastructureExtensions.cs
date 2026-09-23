using Aspire.Hosting.ApplicationModel;

namespace BookStore.Aspire.Extensions;

public static class InfrastructureExtensions
{
    public static (
        IResourceBuilder<PostgresServerResource> Postgres,
        IResourceBuilder<PostgresDatabaseResource> Database,
        IResourceBuilder<RabbitMQServerResource> RabbitMq,
        IResourceBuilder<ContainerResource> Prometheus)
        AddBookStoreInfrastructure(this IDistributedApplicationBuilder builder)
    {
        var postgres = builder.AddPostgres("postgres")
            .WithImage("postgres", "17-alpine")
            .WithDataVolume("pg-data");

        // "default" → injects ConnectionStrings__default; GetConnectionString("Default") resolves it
        // case-insensitively, so both BookStoreDbContext and FraudDbContext find their fallback key.
        var db = postgres.AddDatabase("default");

        // "RabbitMQ" → injects ConnectionStrings__RabbitMQ, matching GetConnectionString("RabbitMQ") in
        // MassTransitExtensions.cs (case-sensitive key).
        // 4-management-alpine already includes the management plugin.
        var rabbitmq = builder.AddRabbitMQ("RabbitMQ")
            .WithImage("rabbitmq", "4-management-alpine");

        // Prometheus: OTLP push receiver (--web.enable-otlp-receiver) on port 9090
        var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v2.55.0")
            .WithArgs("--config.file=/etc/prometheus/prometheus.yml", "--web.enable-otlp-receiver")
            .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml")
            .WithHttpEndpoint(port: 9090, targetPort: 9090, name: "ui");

        // Grafana: auto-provisioned datasource + dashboards
        builder.AddContainer("grafana", "grafana/grafana", "11.4.0")
            .WithBindMount("./grafana/provisioning", "/etc/grafana/provisioning")
            .WithBindMount("./grafana/dashboards", "/var/lib/grafana/dashboards")
            .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "ui")
            .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "bookstore")
            .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
            .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Viewer");

        return (postgres, db, rabbitmq, prometheus);
    }
}
