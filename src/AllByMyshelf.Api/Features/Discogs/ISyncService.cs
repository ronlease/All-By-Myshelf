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

    /// <summary>The sync options for the current or most recent sync.</summary>
    SyncOptionsDto SyncOptions { get; }

    /// <summary>
    /// Returns an estimate of the sync scope based on the current collection state.
    /// </summary>
    Task<SyncEstimateDto> GetEstimateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to start a background sync with the given options.
    /// </summary>
    /// <param name="options">Sync configuration options.</param>
    /// <returns>
    /// A <see cref="SyncStartResult"/> indicating whether the sync was started or why it could not be.
    /// </returns>
    SyncStartResult TryStartSync(SyncOptionsDto? options = null);
}
