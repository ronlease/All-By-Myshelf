using System.Diagnostics.CodeAnalysis;

namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Estimated scope and cost of a sync operation, returned by GET /api/v1/sync/estimate.
/// </summary>
[ExcludeFromCodeCoverage]
public record SyncEstimateDto(
    /// <summary>Number of releases that already have detail data cached.</summary>
    int CachedReleases,

    /// <summary>Number of new releases that would need detail/pricing API calls.</summary>
    int NewReleases,

    /// <summary>Total releases in the collection.</summary>
    int TotalReleases
);
