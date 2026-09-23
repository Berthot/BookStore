using Aspire.Hosting.ApplicationModel;

namespace BookStore.Aspire.Extensions;

public static class InfrastructureExtensions
{
    public static (
        IResourceBuilder<PostgresServerResource> Postgres,
        IResourceBuilder<PostgresDatabaseResource> Database,
        IResourceBuilder<RabbitMQServerResource> RabbitMq)
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

        return (postgres, db, rabbitmq);
    }
}
