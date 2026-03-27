using System.Diagnostics.CodeAnalysis;

namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Snapshot of the current sync operation state, returned by GET /api/v1/sync/status.
/// </summary>
/// <param name="Current">Number of releases processed so far.</param>
/// <param name="IsRunning">Whether a sync is currently running.</param>
/// <param name="Phase">Current sync phase (collection, details, pricing, wantlist, saving); null when idle.</param>
/// <param name="RetryAfterSeconds">Seconds remaining before retrying after a rate limit; null when not paused.</param>
/// <param name="Status">Current sync status label.</param>
/// <param name="Total">Total number of releases to process.</param>
[ExcludeFromCodeCoverage]
public record SyncProgressDto(
    int Current,
    bool IsRunning,
    string? Phase,
    int? RetryAfterSeconds,
    string Status,
    int Total
);
