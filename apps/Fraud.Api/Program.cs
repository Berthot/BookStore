using System.Text.Json;
using System.Text.Json.Serialization;
using Application;
using Fraud.Api.Endpoints.FraudAnalysis;
using Fraud.Api.Infrastructure.Filters.Idempotency;
using Web.ErrorHandling;
using Web.Filters.Idempotency;
using Web.Middleware;
using Fraud.Api.Jobs;
using Infrastructure;
using Infrastructure.Options;
using Infrastructure.Persistence.Fraud;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddFraudApplication()
    .AddFraudInfrastructure(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSingleton<IIdempotencyTelemetry, FraudIdempotencyTelemetry>();

builder.Services.AddHealthChecks();

builder.Services.AddHostedService<FraudMetricsJob>();

builder.Services.AddOpenApi();

var app = builder.Build();

await ApplyDatabaseMigrationsAsync(app);

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();

app.Use(static (ctx, next) =>
{
    ctx.Request.EnableBuffering();
    return next();
});

app.MapOpenApi();
app.UseSwaggerUI(options =>
    options.SwaggerEndpoint("/openapi/v1.json", "Fraud API v1"));

app.MapHealthChecks("/health");

var api = app.MapGroup("/api/v1");

api.MapTransactionEndpoints();
api.MapReviewEndpoints();

app.Run();

static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    var dbOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    if (!dbOptions.ApplyMigrationsOnStartup)
        return;

    await using var scope = app.Services.CreateAsyncScope();

    var fraudDb = scope.ServiceProvider.GetRequiredService<FraudDbContext>();
    await fraudDb.Database.MigrateAsync();
}

namespace Fraud.Api
{
    public partial class Program { }
}
