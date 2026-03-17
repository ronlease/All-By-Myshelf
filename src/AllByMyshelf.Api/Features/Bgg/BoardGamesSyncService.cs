using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Models.Entities;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Features.Bgg;

/// <summary>
/// Singleton service that coordinates triggering and executing a BoardGameGeek collection sync.
/// Implements <see cref="IBoardGamesSyncService"/> for the controller layer and
/// <see cref="IHostedService"/> so it can run as a background worker.
/// </summary>
public class BoardGamesSyncService(
    IOptions<BggOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<BoardGamesSyncService> logger)
    : BackgroundService, IBoardGamesSyncService
{
    private readonly BggOptions _options = options.Value;

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
                logger.LogInformation("BGG sync cancelled due to application shutdown.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "BGG sync failed.");
            }
            finally
            {
                Volatile.Write(ref _syncRunning, 0);
            }
        }
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("BGG sync started.");

        // Resolve scoped services (DbContext, BggClient) from a fresh scope.
        await using var scope = scopeFactory.CreateAsyncScope();
        var bggClient = scope.ServiceProvider.GetRequiredService<BggClient>();
        var boardGamesRepository = scope.ServiceProvider.GetRequiredService<IBoardGamesRepository>();

        var collectionItems = await bggClient.GetCollectionAsync(_options.Username, cancellationToken);
        logger.LogInformation("Fetched {Count} board games from BGG.", collectionItems.Count);

        // Batch IDs into groups of 20 for enrichment
        var allDetails = new Dictionary<int, BggThingDetail>();
        var ids = collectionItems.Select(c => c.BggId).ToList();

        for (var i = 0; i < ids.Count; i += 20)
        {
            var batch = ids.Skip(i).Take(20).ToList();
            logger.LogInformation("Fetching thing details for batch {Start}-{End} ({Count} items)", i, i + batch.Count - 1, batch.Count);

            var details = await bggClient.GetThingDetailsAsync(batch, cancellationToken);
            foreach (var detail in details)
            {
                allDetails[detail.Id] = detail;
            }

            // Rate limiting: 500ms delay between batches
            if (i + 20 < ids.Count)
            {
                await Task.Delay(500, cancellationToken);
            }
        }

        var now = DateTimeOffset.UtcNow;
        var entities = new List<BoardGame>(collectionItems.Count);

        foreach (var c in collectionItems)
        {
            allDetails.TryGetValue(c.BggId, out var detail);

            var boardGame = new BoardGame
            {
                BggId = c.BggId,
                CoverImageUrl = c.CoverImageUrl,
                Description = detail?.Description,
                Designers = detail?.Designers ?? [],
                Genre = detail?.Category,
                Id = Guid.NewGuid(),
                LastSyncedAt = now,
                MaxPlayers = c.MaxPlayers,
                MaxPlaytime = c.MaxPlaytime,
                MinPlayers = c.MinPlayers,
                MinPlaytime = c.MinPlaytime,
                ThumbnailUrl = c.ThumbnailUrl,
                Title = c.Name,
                YearPublished = c.YearPublished
            };

            entities.Add(boardGame);
        }

        await boardGamesRepository.UpsertCollectionAsync(entities, cancellationToken);
        logger.LogInformation("BGG sync completed successfully.");
    }

    /// <inheritdoc/>
    public SyncStartResult TryStartSync()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiToken))
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
