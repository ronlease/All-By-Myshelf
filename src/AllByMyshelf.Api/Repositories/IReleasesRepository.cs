using AllByMyshelf.Api.Models.DTOs;
using AllByMyshelf.Api.Models.Entities;

namespace AllByMyshelf.Api.Repositories;

/// <summary>
/// Data-access contract for the <see cref="Release"/> entity.
/// </summary>
public interface IReleasesRepository
{
    /// <summary>
    /// Returns the release with the specified application ID, or null if not found.
    /// </summary>
    /// <param name="id">The application-generated GUID for the release.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Release?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a paginated slice of releases ordered by artist then title,
    /// optionally filtered by the criteria in <paramref name="filter"/>.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filter">Optional filter criteria; null means no filtering.</param>
    Task<(IReadOnlyList<Release> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken, ReleaseFilter? filter = null);

    /// <summary>
    /// Replaces the entire collection with <paramref name="releases"/>.
    /// Existing records matching by <see cref="Release.DiscogsId"/> are updated;
    /// new records are inserted; records no longer present are deleted.
    /// </summary>
    Task UpsertCollectionAsync(IEnumerable<Release> releases, CancellationToken cancellationToken);
}
