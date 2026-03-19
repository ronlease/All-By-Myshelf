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
    : SyncServiceBase, IBoardGamesSyncService
{
    private readonly BggOptions _options = options.Value;

    /// <inheritdoc/>
    protected override bool IsTokenConfigured => !string.IsNullOrWhiteSpace(_options.ApiToken) && !string.IsNullOrWhiteSpace(_options.Username);

    /// <inheritdoc/>
    protected override ILogger Logger => logger;

    /// <inheritdoc/>
    protected override string LogName => "BGG";

    /// <inheritdoc/>
    protected override async Task RunSyncAsync(CancellationToken cancellationToken)
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
}
