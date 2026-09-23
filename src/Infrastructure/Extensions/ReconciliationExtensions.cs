using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class ReconciliationExtensions
{
    /// <summary>Registers infrastructure services needed for reconciliation and purge jobs.</summary>
    public static IServiceCollection AddReconciliation(this IServiceCollection services) => services;
}
