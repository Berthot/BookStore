using Domain.Entities.Sales;

namespace Domain.Repositories;

/// <summary>Repository for the Purchase aggregate.</summary>
public interface IPurchaseRepository : IRepository<Purchase>
{
    /// <summary>Returns the purchase with the given correlation id, or null if not found.</summary>
    Task<Purchase?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>Returns the purchase linked to the given fraud transaction id, or null if not found.</summary>
    Task<Purchase?> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default);

    /// <summary>Returns purchases still in PendingFraudCheck created before the given threshold.</summary>
    Task<IReadOnlyList<Purchase>> ListPendingFraudCheckAsync(DateTime before, CancellationToken cancellationToken = default);
}
