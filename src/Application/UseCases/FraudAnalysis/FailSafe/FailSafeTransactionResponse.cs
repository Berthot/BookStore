using Domain.Enums;

namespace Application.UseCases.FraudAnalysis.FailSafe;

public sealed record FailSafeTransactionResponse(Guid TransactionId, Outcome Outcome);
