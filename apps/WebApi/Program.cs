using System.Text.Json;
using System.Text.Json.Serialization;
using Application;
using Infrastructure;
using Infrastructure.Options;
using Infrastructure.Persistence.BookStore;
using Infrastructure.Persistence.Fraud;
using Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApi.Endpoints.FraudAnalysis;
using WebApi.ErrorHandling;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Apply pending migrations and optionally seed catalogue data
await ApplyDatabaseMigrationsAsync(app);

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "BookStore API v1"));
}

var api = app.MapGroup("/api/v1");

app.MapTransactionEndpoints();
app.MapReviewEndpoints();

app.Run();

static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    var dbOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    if (!dbOptions.ApplyMigrationsOnStartup)
        return;

    var seedingOptions = app.Services.GetRequiredService<IOptions<SeedingOptions>>().Value;

    await using var scope = app.Services.CreateAsyncScope();

    // Never wrap MigrateAsync in an explicit transaction — EF Core 9+ manages its own locks
    var bookStoreDb = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
    await bookStoreDb.Database.MigrateAsync();

    var fraudDb = scope.ServiceProvider.GetRequiredService<FraudDbContext>();
    await fraudDb.Database.MigrateAsync();

    if (seedingOptions.Enabled)
    {
        var seeder = scope.ServiceProvider.GetRequiredService<ICatalogSeeder>();
        await seeder.SeedAsync();
    }
}
