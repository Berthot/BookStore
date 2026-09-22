using Domain.Entities.Catalog;

namespace Domain.Repositories;

/// <summary>Repository for the Book aggregate.</summary>
public interface IBookRepository : IRepository<Book>
{
    /// <summary>Returns all available books ordered by title.</summary>
    Task<IReadOnlyList<Book>> ListAllAsync(CancellationToken cancellationToken = default);
}
