using AllByMyshelf.Api.Models.DTOs;

namespace AllByMyshelf.Api.Services;

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
}
