namespace Application.UseCases.FraudAnalysis.AssessTransaction;

public sealed record AssessTransactionResponse(Guid TransactionId, string Outcome);
