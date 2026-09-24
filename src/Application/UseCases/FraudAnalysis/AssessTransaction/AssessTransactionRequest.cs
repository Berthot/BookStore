using Application.Commons;
using Cortex.Mediator.Commands;

namespace Application.UseCases.FraudAnalysis.AssessTransaction;

public sealed record AssessTransactionRequest(Guid TransactionId, int DelaySeconds = 0)
    : ICommand<OperationResult<AssessTransactionResponse>>;
