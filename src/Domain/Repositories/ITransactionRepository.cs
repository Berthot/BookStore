using Domain.Entities.FraudAnalysis;

namespace Domain.Repositories;

/// <summary>Repository for the Transaction aggregate.</summary>
public interface ITransactionRepository : IRepository<Transaction>
{
    /// <summary>Returns the transaction with the given correlation id, or null if not found.</summary>
    Task<Transaction?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>Returns the count of transactions with the given payment fingerprint in [<paramref name="since"/>, <paramref name="until"/>), excluding <paramref name="excludeId"/>.</summary>
    Task<int> CountRecentByFingerprintAsync(string fingerprint, DateTime since, DateTime until, Guid excludeId, CancellationToken cancellationToken = default);

    /// <summary>Returns the total count and average amount of transactions for <paramref name="customerId"/> that occurred strictly before <paramref name="until"/>, excluding <paramref name="excludeId"/>.</summary>
    Task<(int Count, decimal AverageAmount)> GetCustomerStatsAsync(string customerId, DateTime until, Guid excludeId, CancellationToken cancellationToken = default);

    /// <summary>Returns the count of transactions for <paramref name="customerId"/> with amount in [<paramref name="lower"/>, <paramref name="upper"/>) in [<paramref name="since"/>, <paramref name="until"/>), excluding <paramref name="excludeId"/>.</summary>
    Task<int> CountJustBelowThresholdAsync(string customerId, decimal lower, decimal upper, DateTime since, DateTime until, Guid excludeId, CancellationToken cancellationToken = default);

    /// <summary>Returns the count of transactions where the latest assessment is an Engine Review (awaiting human decision).</summary>
    Task<long> CountPendingReviewAsync(CancellationToken cancellationToken = default);

    /// <summary>Registers a newly created assessment so EF Core tracks it as Added (INSERT) rather than Modified (UPDATE).</summary>
    void AddAssessment(Assessment assessment);
}
