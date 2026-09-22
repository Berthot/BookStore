using Application.Abstractions.Idempotency;
using Domain.Repositories;
using Infrastructure.Options;
using Infrastructure.Persistence.BookStore;
using Infrastructure.Persistence.BookStore.Repositories;
using Infrastructure.Persistence.BookStore.UnitOfWork;
using Infrastructure.Persistence.Fraud;
using Infrastructure.Persistence.Fraud.Repositories;
using Infrastructure.Persistence.Fraud.UnitOfWork;
using Infrastructure.Persistence.Idempotency;
using Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class PostgresExtensions
{
    /// <summary>Registers PostgreSQL DbContexts, repositories, unit-of-work, idempotency stores and startup options for both bounded contexts.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SeedingOptions>()
            .Bind(configuration.GetSection(SeedingOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddPostgresDbContext<BookStoreDbContext>(configuration, "bookstore",
            opts => opts.UseAsyncSeeding(async (ctx, _, ct) =>
                await new CatalogDataSeeder((BookStoreDbContext)ctx).SeedAsync(ct)));

        services.AddPostgresDbContext<FraudDbContext>(configuration, "fraud");

        services.AddScoped<ICatalogSeeder, CatalogDataSeeder>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        services.AddScoped<IBookStoreUnitOfWork, BookStoreUnitOfWork>();
        services.AddScoped<IFraudUnitOfWork, FraudUnitOfWork>();

        services.AddScoped<IBookStoreIdempotencyStore, BookStoreIdempotencyStore>();
        services.AddScoped<IFraudIdempotencyStore, FraudIdempotencyStore>();

        return services;
    }

    /// <summary>Registers a typed DbContext with Npgsql, retry-on-failure and a schema-scoped migrations history table.</summary>
    private static IServiceCollection AddPostgresDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string schema,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(typeof(TContext).Name.Replace("DbContext", string.Empty))
            ?? configuration.GetConnectionString("Default")
            ?? "Host=localhost;Database=bookstore;Username=postgres;Password=postgres";

        services.AddDbContext<TContext>(opts =>
        {
            opts.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure()
                      .MigrationsHistoryTable("__EFMigrationsHistory", schema));
            configureOptions?.Invoke(opts);
        });

        return services;
    }
}
