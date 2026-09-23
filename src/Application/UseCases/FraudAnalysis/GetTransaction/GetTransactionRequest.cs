using Application.Commons;
using Cortex.Mediator.Queries;

namespace Application.UseCases.FraudAnalysis.GetTransaction;

public sealed record GetTransactionRequest(Guid TransactionId)
    : IQuery<OperationResult<GetTransactionResponse>>;
