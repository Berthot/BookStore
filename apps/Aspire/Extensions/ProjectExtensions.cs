using Aspire.Hosting.ApplicationModel;

namespace BookStore.Aspire.Extensions;

public static class ProjectExtensions
{
    public static IResourceBuilder<ProjectResource> AddWebApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> db,
        IResourceBuilder<PostgresServerResource> postgres,
        IResourceBuilder<RabbitMQServerResource> rabbitmq,
        IResourceBuilder<ContainerResource> prometheus)
    {
        return builder.AddProject<Projects.WebApi>("webapi")
            .WithReference(db)
            .WithReference(rabbitmq)
            .WaitFor(postgres)
            .WaitFor(rabbitmq)
            // Dynamic URL resolved by Aspire from the prometheus container endpoint
            .WithEnvironment("PROMETHEUS_OTLP_ENDPOINT", prometheus.GetEndpoint("ui"))
            .WithHttpHealthCheck("/health");
    }

    public static IResourceBuilder<ProjectResource> AddWorker(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> db,
        IResourceBuilder<PostgresServerResource> postgres,
        IResourceBuilder<RabbitMQServerResource> rabbitmq,
        IResourceBuilder<ProjectResource> webapi,
        IResourceBuilder<ContainerResource> prometheus)
    {
        return builder.AddProject<Projects.Worker>("worker")
            .WithReference(db)
            .WithReference(rabbitmq)
            .WaitFor(postgres)
            .WaitFor(rabbitmq)
            .WaitFor(webapi)
            // Dynamic URL resolved by Aspire from the prometheus container endpoint
            .WithEnvironment("PROMETHEUS_OTLP_ENDPOINT", prometheus.GetEndpoint("ui"));
    }
}
