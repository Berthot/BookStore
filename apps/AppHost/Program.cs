var builder = DistributedApplication.CreateBuilder(args);

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

builder.AddProject<Projects.WebApi>("webapi")
    .WithReference(db)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WithEnvironment("Database__ApplyMigrationsOnStartup", "true")
    .WithEnvironment("Seeding__Enabled", "true");

builder.AddProject<Projects.Worker>("worker")
    .WithReference(db)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq);

builder.Build().Run();
