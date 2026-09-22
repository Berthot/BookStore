using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class PostgresExtensions
{
    /// <summary>Registers PostgreSQL DbContexts and related persistence services. Filled in TSK-0069.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        return services;
    }
}
