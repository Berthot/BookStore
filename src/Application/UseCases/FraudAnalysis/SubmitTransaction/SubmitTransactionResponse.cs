namespace Application.UseCases.FraudAnalysis.SubmitTransaction;

public sealed record SubmitTransactionResponse(Guid TransactionId, string Status);
