using AllByMyshelf.Api.Models.Entities;

namespace AllByMyshelf.Api.Features.Hardcover;

/// <summary>
/// Data access contract for books.
/// </summary>
public interface IBooksRepository
{
    /// <summary>
    /// Returns a paginated list of books with optional filtering.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filter">Optional filter criteria.</param>
    /// <returns>A tuple containing the list of books and the total count.</returns>
    Task<(IReadOnlyList<Book> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        BookFilter? filter = null);

    /// <summary>
    /// Returns a single randomly selected book. Returns null if no books exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Book?> GetRandomAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Inserts or updates the provided books, removing any books not present in the incoming collection.
    /// </summary>
    /// <param name="books">Books to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpsertCollectionAsync(IEnumerable<Book> books, CancellationToken cancellationToken);
}
