using Domain.Repositories;
using Infrastructure.Persistence.BookStore;
using Infrastructure.Persistence.BookStore.Repositories;
using Infrastructure.Persistence.BookStore.UnitOfWork;
using Infrastructure.Persistence.Fraud;
using Infrastructure.Persistence.Fraud.Repositories;
using Infrastructure.Persistence.Fraud.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class PostgresExtensions
{
    /// <summary>Registers PostgreSQL DbContexts, repositories and unit-of-work services for both bounded contexts.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresDbContext<BookStoreDbContext>(configuration, "bookstore");
        services.AddPostgresDbContext<FraudDbContext>(configuration, "fraud");

        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        services.AddScoped<IBookStoreUnitOfWork, BookStoreUnitOfWork>();
        services.AddScoped<IFraudUnitOfWork, FraudUnitOfWork>();

        return services;
    }

    /// <summary>Registers a typed DbContext with Npgsql, retry-on-failure and a schema-scoped migrations history table.</summary>
    private static IServiceCollection AddPostgresDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string schema)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(typeof(TContext).Name.Replace("DbContext", string.Empty))
            ?? configuration.GetConnectionString("Default")
            ?? "Host=localhost;Database=bookstore;Username=postgres;Password=postgres";

        services.AddDbContext<TContext>(opts =>
            opts.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure()
                      .MigrationsHistoryTable("__EFMigrationsHistory", schema)));

        return services;
    }
}
