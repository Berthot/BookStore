using Domain.Entities.FraudAnalysis;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Fraud.Repositories;

internal sealed class TransactionRepository(FraudDbContext context) : ITransactionRepository
{
    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Transactions
            .Include(t => t.Assessments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public void Add(Transaction entity) => context.Transactions.Add(entity);

    public void AddAssessment(Assessment assessment) => context.Assessments.Add(assessment);

    public async Task<Transaction?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default) =>
        await context.Transactions
            .Include(t => t.Assessments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.CorrelationId == correlationId, cancellationToken);

    public async Task<int> CountRecentByFingerprintAsync(string fingerprint, DateTime since, Guid excludeId, CancellationToken cancellationToken = default) =>
        await context.Transactions
            .Where(t => t.PaymentFingerprint == fingerprint && t.OccurredAt >= since && t.Id != excludeId)
            .CountAsync(cancellationToken);

    public async Task<(int Count, decimal AverageAmount)> GetCustomerStatsAsync(string customerId, Guid excludeId, CancellationToken cancellationToken = default)
    {
        var amounts = await context.Transactions
            .Where(t => t.CustomerId == customerId && t.Id != excludeId)
            .Select(t => t.Amount.Value)
            .ToListAsync(cancellationToken);

        return (amounts.Count, amounts.Count > 0 ? amounts.Average() : 0m);
    }

    public async Task<int> CountJustBelowThresholdAsync(string customerId, decimal lower, decimal upper, DateTime since, Guid excludeId, CancellationToken cancellationToken = default) =>
        await context.Transactions
            .Where(t => t.CustomerId == customerId
                     && t.Amount.Value >= lower
                     && t.Amount.Value < upper
                     && t.OccurredAt >= since
                     && t.Id != excludeId)
            .CountAsync(cancellationToken);
}
