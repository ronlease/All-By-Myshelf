using AllByMyshelf.Api.Models.Entities;

namespace AllByMyshelf.Api.Features.Wantlist;

/// <summary>
/// Data-access contract for the <see cref="WantlistRelease"/> entity.
/// </summary>
public interface IWantlistRepository
{
    /// <summary>
    /// Returns a paginated slice of wantlist releases ordered by artist then title.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<(IReadOnlyList<WantlistRelease> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Removes all wantlist releases whose Discogs ID is not present in the active set.
    /// </summary>
    /// <param name="activeDiscogsIds">The set of Discogs IDs currently in the wantlist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAbsentAsync(IReadOnlySet<int> activeDiscogsIds, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts or updates the provided wantlist releases.
    /// Existing records matching by Discogs ID are updated; new records are inserted.
    /// </summary>
    /// <param name="releases">Wantlist releases to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpsertAsync(IEnumerable<WantlistRelease> releases, CancellationToken cancellationToken);
}
