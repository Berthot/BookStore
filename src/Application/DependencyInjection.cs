using Application.Behaviors;
using Cortex.Mediator.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    /// <summary>Registers CQRS mediator (Cortex.Mediator), pipeline behaviors and FluentValidation validators.</summary>
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

        return services;
    }
}
