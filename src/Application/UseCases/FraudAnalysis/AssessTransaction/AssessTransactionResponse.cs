using Domain.Enums;

namespace Application.UseCases.FraudAnalysis.AssessTransaction;

public sealed record AssessTransactionResponse(Guid TransactionId, Outcome Outcome);
