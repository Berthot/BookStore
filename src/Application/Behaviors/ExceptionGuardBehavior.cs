using Application.Commons;
using Cortex.Mediator.Commands;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;

namespace Application.Behaviors;

/// <summary>Converts unexpected exceptions to Internal OperationResult; re-throws OperationCanceledException.</summary>
public sealed class ExceptionGuardCommandBehavior<TCommand, TResult>(
    ILogger<ExceptionGuardCommandBehavior<TCommand, TResult>> logger)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
    where TResult : OperationResult, new()
{
    public async Task<TResult> Handle(TCommand command, CommandHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception handling command {Command}", typeof(TCommand).Name);
            return new TResult { IsSuccess = false, ErrorCode = ErrorCode.Internal, Errors = ["An unexpected error occurred."] };
        }
    }
}

/// <summary>Converts unexpected exceptions to Internal OperationResult; re-throws OperationCanceledException.</summary>
public sealed class ExceptionGuardQueryBehavior<TQuery, TResult>(
    ILogger<ExceptionGuardQueryBehavior<TQuery, TResult>> logger)
    : IQueryPipelineBehavior<TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : OperationResult, new()
{
    public async Task<TResult> Handle(TQuery query, QueryHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception handling query {Query}", typeof(TQuery).Name);
            return new TResult { IsSuccess = false, ErrorCode = ErrorCode.Internal, Errors = ["An unexpected error occurred."] };
        }
    }
}
