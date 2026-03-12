using AllByMyshelf.Api.Configuration;
using AllByMyshelf.Api.Infrastructure.ExternalApis;
using AllByMyshelf.Api.Models.Entities;
using AllByMyshelf.Api.Repositories;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Services;

/// <summary>
/// Singleton service that coordinates triggering and executing a Discogs collection sync.
/// Implements <see cref="ISyncService"/> for the controller layer and
/// <see cref="IHostedService"/> so it can run as a background worker.
/// </summary>
public class SyncService(
    IOptions<DiscogsOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<SyncService> logger)
    : BackgroundService, ISyncService
{
    private readonly DiscogsOptions _options = options.Value;

    // Channel used to signal the background loop that a sync was requested.
    private readonly System.Threading.Channels.Channel<bool> _syncChannel =
        System.Threading.Channels.Channel.CreateBounded<bool>(1);

    // 0 = idle, 1 = running
    private int _syncRunning;

    /// <inheritdoc/>
    public bool IsSyncRunning => Volatile.Read(ref _syncRunning) == 1;

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
                logger.LogInformation("Discogs sync cancelled due to application shutdown.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Discogs sync failed.");
            }
            finally
            {
                Volatile.Write(ref _syncRunning, 0);
            }
        }
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Discogs sync started.");

        // Resolve scoped services (DbContext, DiscogsClient) from a fresh scope.
        await using var scope = scopeFactory.CreateAsyncScope();
        var discogsClient = scope.ServiceProvider.GetRequiredService<DiscogsClient>();
        var releasesRepository = scope.ServiceProvider.GetRequiredService<IReleasesRepository>();

        var apiReleases = await discogsClient.GetCollectionAsync(cancellationToken);
        logger.LogInformation("Fetched {Count} releases from Discogs.", apiReleases.Count);

        var now = DateTimeOffset.UtcNow;
        var entities = new List<Release>(apiReleases.Count);

        foreach (var r in apiReleases)
        {
            var artist = r.BasicInformation.Artists.FirstOrDefault()?.Name ?? "Unknown Artist";
            var format = r.BasicInformation.Formats.FirstOrDefault()?.Name ?? string.Empty;
            var year = r.BasicInformation.Year == 0 ? (int?)null : r.BasicInformation.Year;

            var release = new Release
            {
                Artist = artist,
                CoverImageUrl = r.BasicInformation.CoverImage,
                DiscogsId = r.Id,
                Format = format,
                Id = Guid.NewGuid(),
                LastSyncedAt = now,
                ThumbnailUrl = r.BasicInformation.Thumb,
                Title = r.BasicInformation.Title,
                Year = year,
            };

            // Fetch extended detail fields for each release. Rate-limit back-off
            // is handled inside FetchWithRetryAsync; failures are logged and skipped.
            var detail = await discogsClient.GetReleaseDetailAsync(r.Id, cancellationToken);
            if (detail is not null)
            {
                release.Genre = detail.Genres.FirstOrDefault();

                logger.LogDebug("Fetched detail for Discogs ID {DiscogsId}.", r.Id);
            }

            entities.Add(release);
        }

        await releasesRepository.UpsertCollectionAsync(entities, cancellationToken);
        logger.LogInformation("Discogs sync completed successfully.");
    }

    /// <inheritdoc/>
    public SyncStartResult TryStartSync()
    {
        if (string.IsNullOrWhiteSpace(_options.PersonalAccessToken))
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
