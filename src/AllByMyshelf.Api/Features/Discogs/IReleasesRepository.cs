using AllByMyshelf.Api.Models.Entities;

namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Data-access contract for the <see cref="Release"/> entity.
/// </summary>
public interface IReleasesRepository
{
    /// <summary>
    /// Returns all releases indexed by their Discogs ID for fast lookup during sync.
    /// </summary>
    Task<Dictionary<int, Release>> GetAllByDiscogsIdAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the release with the specified application ID, or null if not found.
    /// </summary>
    /// <param name="id">The application-generated GUID for the release.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Release?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns groups of releases that share the same artists and title (case-insensitive)
    /// but have different Discogs IDs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<(List<string> Artists, string Title, List<Release> Releases)>> GetDuplicatesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns all releases that have at least one missing data field.
    /// </summary>
    Task<IReadOnlyList<Release>> GetIncompleteReleasesAsync(CancellationToken cancellationToken);

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
    /// Returns a single randomly selected release, optionally filtered by the criteria
    /// in <paramref name="filter"/>. Returns null if no releases match.
    /// </summary>
    /// <param name="filter">Optional filter criteria; null means any release.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Release?> GetRandomAsync(RandomReleaseFilter? filter, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the most recently added releases within the specified time window.
    /// </summary>
    Task<IReadOnlyList<Release>> GetRecentlyAddedAsync(int count, int days, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the notes and rating for a specific release.
    /// </summary>
    /// <param name="id">The application-generated GUID for the release.</param>
    /// <param name="notes">The new notes value; null to clear.</param>
    /// <param name="rating">The new rating value (1-5); null to clear.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the release was found and updated; false otherwise.</returns>
    Task<bool> UpdateNotesAndRatingAsync(Guid id, string? notes, int? rating, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a single release with re-synced detail/pricing data, replacing its tracks.
    /// </summary>
    /// <param name="release">The release with updated fields and tracks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateResyncedReleaseAsync(Models.Entities.Release release, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the entire collection with <paramref name="releases"/>.
    /// Existing records matching by <see cref="Release.DiscogsId"/> are updated;
    /// new records are inserted; records no longer present are deleted.
    /// </summary>
    Task UpsertCollectionAsync(IEnumerable<Release> releases, CancellationToken cancellationToken);
}
