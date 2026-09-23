using Aspire.Hosting.ApplicationModel;

namespace AppHost.Extensions;

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
            // Suppress EF Core SQL command logs — too noisy for the Dashboard
            .WithEnvironment("Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command", "Warning")
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
            // Promote MassTransit consumer logs so they appear in Dashboard
            .WithEnvironment("Logging__LogLevel__MassTransit", "Information")
            .WithEnvironment("Logging__LogLevel__Worker.Consumers", "Information");
    }
}
