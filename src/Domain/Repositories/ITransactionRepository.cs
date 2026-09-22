using Domain.Entities.FraudAnalysis;

namespace Domain.Repositories;

/// <summary>Repository for the Transaction aggregate.</summary>
public interface ITransactionRepository : IRepository<Transaction>
{
    /// <summary>Returns the transaction with the given correlation id, or null if not found.</summary>
    Task<Transaction?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
}
