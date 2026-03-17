using System.Threading.Channels;

namespace AllByMyshelf.Api.Common;

/// <summary>
/// Abstract base class for background sync services that use a channel-based
/// request queue and an atomic running flag to ensure single-instance execution.
/// </summary>
/// <remarks>
/// Derived classes must implement:
/// <list type="bullet">
/// <item><see cref="IsTokenConfigured"/> to validate their API token configuration</item>
/// <item><see cref="LogName"/> to provide a human-readable name for logging</item>
/// <item><see cref="RunSyncAsync"/> to perform the actual sync logic</item>
/// <item>Optionally override <see cref="OnSyncCompleted"/> for cleanup tasks</item>
/// </list>
/// </remarks>
public abstract class SyncServiceBase : BackgroundService
{
    private readonly Channel<bool> _syncChannel = Channel.CreateBounded<bool>(1);
    private int _syncRunning;

    /// <summary>
    /// Indicates whether a sync is currently running.
    /// </summary>
    public bool IsSyncRunning => Volatile.Read(ref _syncRunning) == 1;

    /// <summary>
    /// Gets a value indicating whether the API token is configured.
    /// </summary>
    protected abstract bool IsTokenConfigured { get; }

    /// <summary>
    /// Gets the logger to use for this sync service.
    /// </summary>
    protected abstract ILogger Logger { get; }

    /// <summary>
    /// Gets the human-readable name of this sync service for logging purposes (e.g., "Discogs", "BGG", "Hardcover").
    /// </summary>
    protected abstract string LogName { get; }

    /// <summary>
    /// Background loop: waits for sync signals and executes them one at a time.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var _ in _syncChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await RunSyncAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                Logger.LogInformation("{LogName} sync cancelled due to application shutdown.", LogName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{LogName} sync failed.", LogName);
            }
            finally
            {
                OnSyncCompleted();
                Volatile.Write(ref _syncRunning, 0);
            }
        }
    }

    /// <summary>
    /// Called after each sync completes (successfully or with an error), before the running flag is cleared.
    /// Derived classes can override this to reset progress counters or other state.
    /// </summary>
    protected virtual void OnSyncCompleted()
    {
        // Default implementation does nothing.
    }

    /// <summary>
    /// Performs the actual sync logic. This method is called by the background loop when a sync is requested.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the sync operation.</param>
    protected abstract Task RunSyncAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to start a background sync.
    /// </summary>
    /// <returns>
    /// A <see cref="SyncStartResult"/> indicating whether the sync was started or why it could not be.
    /// </returns>
    public SyncStartResult TryStartSync()
    {
        if (!IsTokenConfigured)
            return SyncStartResult.TokenNotConfigured;

        // Try to acquire the running flag atomically.
        if (Interlocked.CompareExchange(ref _syncRunning, 1, 0) != 0)
            return SyncStartResult.AlreadyRunning;

        // Signal the background loop. If the channel is already full the write
        // will fail, but that's fine — a sync is about to run anyway.
        _syncChannel.Writer.TryWrite(true);
        return SyncStartResult.Started;
    }
}
