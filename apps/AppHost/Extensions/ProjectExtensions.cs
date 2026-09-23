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
            .WithEnvironment("Database__ApplyMigrationsOnStartup", "true")
            .WithEnvironment("Seeding__Enabled", "true");
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
            .WaitFor(webapi);
    }
}
