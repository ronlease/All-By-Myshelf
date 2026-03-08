using AllByMyshelf.Api.Models.DTOs;

namespace AllByMyshelf.Api.Services;

/// <summary>
/// Business logic contract for querying the local releases collection.
/// </summary>
public interface IReleasesService
{
    /// <summary>
    /// Returns a paginated result of releases stored in the local database.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of items per page (capped at 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PagedResult<ReleaseDto>> GetReleasesAsync(
        int page, int pageSize, CancellationToken cancellationToken);
}
