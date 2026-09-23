var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("pg-data")
    .WithPgAdmin();

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

builder.AddProject<Projects.WebApi>("webapi")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WithEnvironment("Database__ApplyMigrationsOnStartup", "true")
    .WithEnvironment("Seeding__Enabled", "true");

builder.AddProject<Projects.Worker>("worker")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq);

builder.Build().Run();
