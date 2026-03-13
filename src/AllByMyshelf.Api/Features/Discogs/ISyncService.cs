using AllByMyshelf.Api.Common;

namespace AllByMyshelf.Api.Features.Discogs;

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
