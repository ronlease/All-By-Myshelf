namespace AllByMyshelf.Api.Models.DTOs;

/// <summary>
/// Snapshot of the current sync operation state, returned by GET /api/v1/sync/status.
/// </summary>
public record SyncProgressDto(
    bool IsRunning,
    int Current,
    int? RetryAfterSeconds,
    string Status,
    int Total
);
