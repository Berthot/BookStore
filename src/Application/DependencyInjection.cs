using Application.Behaviors;
using Application.UseCases.Idempotency.PurgeExpiredKeys;
using Application.UseCases.Idempotency.PurgeFraudExpiredKeys;
using Cortex.Mediator.DependencyInjection;
using Domain.Rules;
using Domain.Rules.Discrepancy;
using Domain.Rules.Transactional;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    /// <summary>Registers only BookStore-bounded-context handlers: Sales, Catalog, and shared behaviors/validators/rules.
    /// Removes FraudAnalysis handlers and cross-domain idempotency handler to pass DI validation in single-context processes.</summary>
    public static IServiceCollection AddBookStoreApplication(this IServiceCollection services)
    {
        AddMediatorCore(services);

        // Remove Fraud-domain handlers — ITransactionRepository not registered in BookStore.Api.
        // Remove Idempotency handlers — PurgeExpiredKeysHandler needs both stores; BookStore.Api purges directly.
        RemoveHandlersWhere(services, t =>
            t.Namespace?.StartsWith("Application.UseCases.FraudAnalysis", StringComparison.Ordinal) == true ||
            t.Namespace?.StartsWith("Application.UseCases.Idempotency", StringComparison.Ordinal) == true);

        return services;
    }

    /// <summary>Registers only Fraud-bounded-context handlers: FraudAnalysis, PurgeFraudExpiredKeys, and shared behaviors/validators/rules.
    /// Removes BookStore handlers and the cross-domain PurgeExpiredKeysHandler to pass DI validation in Fraud.Api / Fraud.Worker.</summary>
    public static IServiceCollection AddFraudApplication(this IServiceCollection services)
    {
        AddMediatorCore(services);

        // Remove BookStore-domain handlers — IPurchaseRepository / IBookRepository not registered in Fraud processes.
        // Remove the cross-domain PurgeExpiredKeysHandler — needs both idempotency stores; Fraud processes use PurgeFraudExpiredKeysHandler.
        RemoveHandlersWhere(services, t =>
            t.Namespace?.StartsWith("Application.UseCases.Sales", StringComparison.Ordinal) == true ||
            t.Namespace?.StartsWith("Application.UseCases.Catalog", StringComparison.Ordinal) == true ||
            t == typeof(PurgeExpiredKeysHandler));

        return services;
    }

    /// <summary>Registers ALL handlers — used by the monolith and integration tests that host both bounded contexts.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        AddMediatorCore(services);
        return services;
    }

    private static void AddMediatorCore(IServiceCollection services)
    {
        services.AddCortexMediator(
            [typeof(DependencyInjection)],
            options =>
            {
                // Order: exception guard (outermost) → validation (inner) → handler
                options.AddOpenCommandPipelineBehavior(typeof(ExceptionGuardCommandBehavior<,>));
                options.AddOpenQueryPipelineBehavior(typeof(ExceptionGuardQueryBehavior<,>));
                options.AddOpenCommandPipelineBehavior(typeof(ValidationCommandBehavior<,>));
                options.AddOpenQueryPipelineBehavior(typeof(ValidationQueryBehavior<,>));
            });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddTransient<IFraudRule, HighValueDigitalRule>();
        services.AddTransient<IFraudRule, NewCustomerHighAmountRule>();
        services.AddTransient<IFraudRule, BulkQuantityRule>();
        services.AddTransient<IFraudRule, CardVelocityRule>();
        services.AddTransient<IFraudRule, AmountDeviationRule>();
        services.AddTransient<IFraudRule, StructuringRule>();
        services.AddTransient<FraudRuleSet>();
    }

    private static void RemoveHandlersWhere(IServiceCollection services, Func<Type, bool> predicate)
    {
        var toRemove = services
            .Where(sd => sd.ImplementationType is { } t && predicate(t))
            .ToList();
        foreach (var sd in toRemove)
            services.Remove(sd);
    }
}
