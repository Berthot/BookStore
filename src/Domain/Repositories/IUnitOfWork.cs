namespace Domain.Repositories;

/// <summary>Abstracts transactional commit. Handlers call CommitAsync exactly once at the end of the success path.</summary>
public interface IUnitOfWork
{
    /// <summary>Persists all pending changes in the current unit of work. Returns true if at least one row was affected.</summary>
    Task<bool> CommitAsync(CancellationToken cancellationToken = default);
}
