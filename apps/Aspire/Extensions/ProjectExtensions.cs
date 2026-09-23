using Aspire.Hosting.ApplicationModel;

namespace BookStore.Aspire.Extensions;

public static class ProjectExtensions
{
    public static IResourceBuilder<ProjectResource> AddWebApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> db,
        IResourceBuilder<PostgresServerResource> postgres,
        IResourceBuilder<RabbitMQServerResource> rabbitmq)
    {
        return builder.AddProject<Projects.WebApi>("webapi")
            .WithReference(db)
            .WithReference(rabbitmq)
            .WaitFor(postgres)
            .WaitFor(rabbitmq)
            .WithEnvironment("OTEL_SERVICE_NAME", "bookstore-webapi")
            .WithEnvironment("Database__ApplyMigrationsOnStartup", "true")
            .WithEnvironment("Seeding__Enabled", "true")
            .WithEnvironment("Logging__LogLevel__Microsoft.EntityFrameworkCore", "Warning")
            .WithEnvironment("Logging__LogLevel__Npgsql", "Warning")
            .WithHttpHealthCheck("/health");
    }

    public static IResourceBuilder<ProjectResource> AddWorker(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> db,
        IResourceBuilder<PostgresServerResource> postgres,
        IResourceBuilder<RabbitMQServerResource> rabbitmq,
        IResourceBuilder<ProjectResource> webapi)
    {
        return builder.AddProject<Projects.Worker>("worker")
            .WithReference(db)
            .WithReference(rabbitmq)
            .WaitFor(postgres)
            .WaitFor(rabbitmq)
            .WaitFor(webapi)
            .WithEnvironment("OTEL_SERVICE_NAME", "bookstore-worker")
            .WithEnvironment("Logging__LogLevel__MassTransit", "Information")
            .WithEnvironment("Logging__LogLevel__Worker.Consumers", "Information")
            .WithEnvironment("Logging__LogLevel__Microsoft.EntityFrameworkCore", "Warning")
            .WithEnvironment("Logging__LogLevel__Npgsql", "Warning");
    }
}
