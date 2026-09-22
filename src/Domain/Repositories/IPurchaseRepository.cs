using Domain.Entities.Sales;

namespace Domain.Repositories;

/// <summary>Repository for the Purchase aggregate.</summary>
public interface IPurchaseRepository : IRepository<Purchase>
{
    /// <summary>Returns the purchase with the given correlation id, or null if not found.</summary>
    Task<Purchase?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
}
