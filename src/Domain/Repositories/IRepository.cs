using Domain.Bases;

namespace Domain.Repositories;

/// <summary>Generic repository providing basic persistence operations for aggregate roots. Never calls SaveChanges — commits are the Unit of Work's responsibility.</summary>
public interface IRepository<TEntity> where TEntity : Entity
{
    /// <summary>Returns the entity with the given id, or null if not found.</summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Attaches the entity to the context for insertion. Does not commit.</summary>
    void Add(TEntity entity);
}
