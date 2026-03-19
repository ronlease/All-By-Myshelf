using System.Diagnostics.CodeAnalysis;

namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Snapshot of the current sync operation state, returned by GET /api/v1/sync/status.
/// </summary>
[ExcludeFromCodeCoverage]
public record SyncProgressDto(
    bool IsRunning,
    int Current,
    int? RetryAfterSeconds,
    string Status,
    int Total
);
