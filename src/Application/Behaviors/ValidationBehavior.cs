using Application.Commons;
using Cortex.Mediator.Commands;
using Cortex.Mediator.Queries;
using FluentValidation;

namespace Application.Behaviors;

/// <summary>Runs FluentValidation validators for commands; returns Fail(Validation) on failures — never throws.</summary>
public sealed class ValidationCommandBehavior<TCommand, TResult>(
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
    where TResult : OperationResult, new()
{
    public async Task<TResult> Handle(TCommand command, CommandHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(command, cancellationToken);
            if (!result.IsValid)
                errors.AddRange(result.Errors.Select(e => e.ErrorMessage));
        }

        if (errors.Count > 0)
            return new TResult { IsSuccess = false, ErrorCode = ErrorCode.Validation, Errors = errors.AsReadOnly() };

        return await next();
    }
}

/// <summary>Runs FluentValidation validators for queries; returns Fail(Validation) on failures — never throws.</summary>
public sealed class ValidationQueryBehavior<TQuery, TResult>(
    IEnumerable<IValidator<TQuery>> validators)
    : IQueryPipelineBehavior<TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : OperationResult, new()
{
    public async Task<TResult> Handle(TQuery query, QueryHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(query, cancellationToken);
            if (!result.IsValid)
                errors.AddRange(result.Errors.Select(e => e.ErrorMessage));
        }

        if (errors.Count > 0)
            return new TResult { IsSuccess = false, ErrorCode = ErrorCode.Validation, Errors = errors.AsReadOnly() };

        return await next();
    }
}
