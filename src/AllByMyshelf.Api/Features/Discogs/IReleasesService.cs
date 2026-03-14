using AllByMyshelf.Api.Common;

namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Business logic contract for querying the local releases collection.
/// </summary>
public interface IReleasesService
{
    /// <summary>
    /// Returns the full detail for a single release, or null if no release with that ID exists.
    /// </summary>
    /// <param name="id">The application-generated GUID for the release.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ReleaseDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns groups of releases that share the same artist and title but have different Discogs IDs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<DuplicateGroupDto>> GetDuplicatesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns all releases with at least one missing data field, with computed missing-field labels.
    /// </summary>
    Task<IReadOnlyList<MaintenanceReleaseDto>> GetIncompleteReleasesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns a randomly selected release, optionally filtered by the criteria in
    /// <paramref name="filter"/>. Returns null if no releases match.
    /// </summary>
    /// <param name="filter">Optional filter criteria; null means any release.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ReleaseDetailDto?> GetRandomAsync(RandomReleaseFilter? filter, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the most recently added releases (up to 10, within the last 30 days).
    /// </summary>
    Task<IReadOnlyList<ReleaseDto>> GetRecentlyAddedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns a paginated result of releases stored in the local database,
    /// optionally filtered by the criteria in <paramref name="filter"/>.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of items per page (capped at 10000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filter">Optional filter criteria; null means no filtering.</param>
    Task<PagedResult<ReleaseDto>> GetReleasesAsync(
        int page, int pageSize, CancellationToken cancellationToken, ReleaseFilter? filter = null);

    /// <summary>
    /// Updates the notes and rating for a specific release.
    /// </summary>
    /// <param name="id">The application-generated GUID for the release.</param>
    /// <param name="notes">The new notes value; null to clear.</param>
    /// <param name="rating">The new rating value (1-5); null to clear.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the release was found and updated; false otherwise.</returns>
    Task<bool> UpdateNotesAndRatingAsync(Guid id, string? notes, int? rating, CancellationToken cancellationToken);
}
