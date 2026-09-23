using Application;
using Infrastructure;
using Worker.Jobs;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<ReconciliationJob>();
builder.Services.AddHostedService<IdempotencyPurgeJob>();

var host = builder.Build();
host.Run();
