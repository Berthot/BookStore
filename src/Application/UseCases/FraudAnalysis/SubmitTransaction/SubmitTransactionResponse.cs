using Domain.Enums;

namespace Application.UseCases.FraudAnalysis.SubmitTransaction;

public sealed record SubmitTransactionResponse(Guid TransactionId, TransactionStatus Status);
