namespace Application.UseCases.FraudAnalysis.FailSafe;

public sealed record FailSafeTransactionResponse(Guid TransactionId, string Outcome);
