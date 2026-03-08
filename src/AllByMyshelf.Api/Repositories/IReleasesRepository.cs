using AllByMyshelf.Api.Models.Entities;

namespace AllByMyshelf.Api.Repositories;

/// <summary>
/// Data-access contract for the <see cref="Release"/> entity.
/// </summary>
public interface IReleasesRepository
{
    /// <summary>
    /// Returns a paginated slice of all releases ordered by artist then title.
    /// </summary>
    Task<(IReadOnlyList<Release> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the entire collection with <paramref name="releases"/>.
    /// Existing records matching by <see cref="Release.DiscogsId"/> are updated;
    /// new records are inserted; records no longer present are deleted.
    /// </summary>
    Task UpsertCollectionAsync(IEnumerable<Release> releases, CancellationToken cancellationToken);
}
