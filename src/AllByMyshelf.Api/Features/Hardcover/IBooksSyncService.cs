using AllByMyshelf.Api.Common;

namespace AllByMyshelf.Api.Features.Hardcover;

/// <summary>
/// Contract for triggering and monitoring a Hardcover collection sync.
/// </summary>
public interface IBooksSyncService
{
    /// <summary>
    /// Indicates whether a sync is currently running.
    /// </summary>
    bool IsSyncRunning { get; }

    /// <summary>
    /// Attempts to start a background sync.
    /// </summary>
    /// <returns>
    /// A <see cref="SyncStartResult"/> indicating whether the sync was started or why it could not be.
    /// </returns>
    SyncStartResult TryStartSync();
}
