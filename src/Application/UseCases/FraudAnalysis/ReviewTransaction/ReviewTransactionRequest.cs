using Application.Commons;
using Application.UseCases.FraudAnalysis.GetTransaction;
using Cortex.Mediator.Commands;

namespace Application.UseCases.FraudAnalysis.ReviewTransaction;

public sealed record ReviewTransactionRequest(
    Guid TransactionId,
    string Outcome,
    string Justification,
    string? ReviewerId)
    : ICommand<OperationResult<GetTransactionResponse>>;
