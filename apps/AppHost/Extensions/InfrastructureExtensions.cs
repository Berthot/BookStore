using Aspire.Hosting.ApplicationModel;

namespace AppHost.Extensions;

public static class InfrastructureExtensions
{
    public static (
        IResourceBuilder<PostgresServerResource> Postgres,
        IResourceBuilder<PostgresDatabaseResource> Database,
        IResourceBuilder<RabbitMQServerResource> RabbitMq)
        AddBookStoreInfrastructure(this IDistributedApplicationBuilder builder)
    {
        var postgres = builder.AddPostgres("postgres")
            .WithDataVolume("pg-data")
            .WithPgAdmin();

        // "default" → injects ConnectionStrings__default; GetConnectionString("Default") resolves it
        // case-insensitively, so both BookStoreDbContext and FraudDbContext find their fallback key.
        var db = postgres.AddDatabase("default");

        // "RabbitMQ" → injects ConnectionStrings__RabbitMQ, matching GetConnectionString("RabbitMQ") in
        // MassTransitExtensions.cs (case-sensitive key).
        var rabbitmq = builder.AddRabbitMQ("RabbitMQ")
            .WithManagementPlugin();

        return (postgres, db, rabbitmq);
    }
}
