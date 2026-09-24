using System.Text.Json;
using System.Text.Json.Serialization;
using Application;
using BookStore.Api.Infrastructure.ErrorHandling;
using BookStore.Api.Endpoints.Catalog;
using BookStore.Api.Endpoints.Sales;
using BookStore.Api.Jobs;
using BookStore.Api.Infrastructure.Middleware;
using Infrastructure;
using Infrastructure.Options;
using Infrastructure.Persistence.BookStore;
using Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddBookStoreInfrastructure(builder.Configuration);

builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http => http.AddServiceDiscovery());

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHealthChecks();

builder.Services.AddOpenApi();

builder.Services.AddHostedService<ReconciliationJob>();

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
    options.SwaggerEndpoint("/openapi/v1.json", "BookStore API v1"));

app.MapHealthChecks("/health");

var api = app.MapGroup("/api/v1");

api.MapBookEndpoints();
api.MapPurchaseEndpoints();

app.Run();

static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    var dbOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    if (!dbOptions.ApplyMigrationsOnStartup)
        return;

    var seedingOptions = app.Services.GetRequiredService<IOptions<SeedingOptions>>().Value;

    await using var scope = app.Services.CreateAsyncScope();

    var bookStoreDb = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
    await bookStoreDb.Database.MigrateAsync();

    if (seedingOptions.Enabled)
    {
        var seeder = scope.ServiceProvider.GetRequiredService<ICatalogSeeder>();
        await seeder.SeedAsync();
    }
}

public partial class Program { }
