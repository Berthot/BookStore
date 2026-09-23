using Application.Commons;
using Cortex.Mediator.Commands;

namespace Application.UseCases.FraudAnalysis.FailSafe;

public sealed record FailSafeTransactionRequest(Guid TransactionId, string Reason)
    : ICommand<OperationResult<FailSafeTransactionResponse>>;
