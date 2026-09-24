using Application;
using Fraud.Worker.Jobs;
using Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddFraudApplication()
    .AddFraudInfrastructure(builder.Configuration);

builder.Services.AddHostedService<IdempotencyPurgeJob>();

var host = builder.Build();
host.Run();
