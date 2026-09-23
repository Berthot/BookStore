using Application.Behaviors;
using Cortex.Mediator.DependencyInjection;
using Domain.Rules;
using Domain.Rules.Discrepancy;
using Domain.Rules.Transactional;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    /// <summary>Registers CQRS mediator (Cortex.Mediator), pipeline behaviors, FluentValidation validators and fraud rules.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
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

        return services;
    }
}
