using Domain.Repositories;

namespace Infrastructure.Persistence.Fraud.UnitOfWork;

internal sealed class FraudUnitOfWork(FraudDbContext context) : IFraudUnitOfWork
{
    /// <summary>Persists all pending changes in the Fraud context. Returns true if at least one row was affected.</summary>
    public async Task<bool> CommitAsync(CancellationToken cancellationToken = default) =>
        await context.SaveChangesAsync(cancellationToken) > 0;
}
