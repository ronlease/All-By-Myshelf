using AllByMyshelf.Api.Models.DTOs;

namespace AllByMyshelf.Api.Services;

/// <summary>
/// Represents the outcome of attempting to start a background sync.
/// </summary>
public enum SyncStartResult
{
    /// <summary>A new sync was successfully enqueued.</summary>
    Started,

    /// <summary>A sync was already in progress; no new sync was started.</summary>
    AlreadyRunning,

    /// <summary>The Discogs token is not configured; sync cannot proceed.</summary>
    TokenNotConfigured
}

/// <summary>
/// Contract for triggering and monitoring a Discogs collection sync.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Indicates whether a sync is currently running.
    /// </summary>
    bool IsSyncRunning { get; }

    /// <summary>Current sync progress snapshot.</summary>
    SyncProgressDto Progress { get; }

    /// <summary>
    /// Attempts to start a background sync.
    /// </summary>
    /// <returns>
    /// A <see cref="SyncStartResult"/> indicating whether the sync was started or why it could not be.
    /// </returns>
    SyncStartResult TryStartSync();
}
