namespace BookStore.Aspire.Extensions;

public static class ProjectExtensions
{
    public static IResourceBuilder<ProjectResource> AddBookStoreApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> db,
        IResourceBuilder<PostgresServerResource> postgres,
        IResourceBuilder<RabbitMQServerResource> rabbitmq,
        IResourceBuilder<ProjectResource> fraudApi,
        IResourceBuilder<ContainerResource> prometheus)
    {
        return builder.AddProject<Projects.BookStore_Api>("bookstore-api")
            .WithReference(db)
            .WithReference(rabbitmq)
            .WithReference(fraudApi)
            .WaitFor(postgres)
            .WaitFor(rabbitmq)
            .WaitFor(fraudApi)
            .WithEnvironment("Reconciliation__IntervalSeconds", "30")
            .WithEnvironment("Reconciliation__StalenessThresholdSeconds", "60")
            .WithEnvironment("PROMETHEUS_OTLP_ENDPOINT", prometheus.GetEndpoint("ui"))
            .WithHttpHealthCheck("/health");
    }

    public static IResourceBuilder<ProjectResource> AddFraudApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> db,
        IResourceBuilder<PostgresServerResource> postgres,
        IResourceBuilder<RabbitMQServerResource> rabbitmq,
        IResourceBuilder<ContainerResource> prometheus)
    {
        return builder.AddProject<Projects.Fraud_Api>("fraud-api")
            .WithReference(db)
            .WithReference(rabbitmq)
            .WaitFor(postgres)
            .WaitFor(rabbitmq)
            .WithEnvironment("PROMETHEUS_OTLP_ENDPOINT", prometheus.GetEndpoint("ui"))
            .WithHttpHealthCheck("/health");
    }

    public static IResourceBuilder<ProjectResource> AddFraudWorker(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> db,
        IResourceBuilder<PostgresServerResource> postgres,
        IResourceBuilder<RabbitMQServerResource> rabbitmq,
        IResourceBuilder<ProjectResource> fraudApi,
        IResourceBuilder<ContainerResource> prometheus)
    {
        return builder.AddProject<Projects.Fraud_Worker>("fraud-worker")
            .WithReference(db)
            .WithReference(rabbitmq)
            .WaitFor(postgres)
            .WaitFor(rabbitmq)
            .WaitFor(fraudApi)
            .WithEnvironment("PROMETHEUS_OTLP_ENDPOINT", prometheus.GetEndpoint("ui"));
    }
}
