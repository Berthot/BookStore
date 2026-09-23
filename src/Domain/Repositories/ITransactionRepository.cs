using Domain.Entities.FraudAnalysis;

namespace Domain.Repositories;

/// <summary>Repository for the Transaction aggregate.</summary>
public interface ITransactionRepository : IRepository<Transaction>
{
    /// <summary>Returns the transaction with the given correlation id, or null if not found.</summary>
    Task<Transaction?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>Returns the count of transactions with the given payment fingerprint occurring on or after <paramref name="since"/>, excluding <paramref name="excludeId"/>.</summary>
    Task<int> CountRecentByFingerprintAsync(string fingerprint, DateTime since, Guid excludeId, CancellationToken cancellationToken = default);

    /// <summary>Returns the total count and average amount of transactions for <paramref name="customerId"/>, excluding <paramref name="excludeId"/>.</summary>
    Task<(int Count, decimal AverageAmount)> GetCustomerStatsAsync(string customerId, Guid excludeId, CancellationToken cancellationToken = default);

    /// <summary>Returns the count of transactions for <paramref name="customerId"/> with amount in [<paramref name="lower"/>, <paramref name="upper"/>) occurring on or after <paramref name="since"/>, excluding <paramref name="excludeId"/>.</summary>
    Task<int> CountJustBelowThresholdAsync(string customerId, decimal lower, decimal upper, DateTime since, Guid excludeId, CancellationToken cancellationToken = default);
}
