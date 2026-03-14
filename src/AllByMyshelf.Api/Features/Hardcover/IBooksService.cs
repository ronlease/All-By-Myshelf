using AllByMyshelf.Api.Common;

namespace AllByMyshelf.Api.Features.Hardcover;

/// <summary>
/// Business logic contract for books.
/// </summary>
public interface IBooksService
{
    /// <summary>
    /// Returns a paginated list of books with optional filtering.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filter">Optional filter criteria.</param>
    /// <returns>A paginated result containing book DTOs.</returns>
    Task<PagedResult<BookDto>> GetBooksAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        BookFilter? filter = null);

    /// <summary>
    /// Returns the full detail for a single book by its application-generated ID.
    /// </summary>
    /// <param name="id">Application-generated GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The book detail if found, otherwise null.</returns>
    Task<BookDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a single randomly selected book, or null if no books exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BookDto?> GetRandomAsync(CancellationToken cancellationToken);
}
