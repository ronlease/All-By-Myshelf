using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Models.Entities;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Features.BoardGameGeek;

/// <summary>
/// Singleton service that coordinates triggering and executing a BoardGameGeek collection sync.
/// Implements <see cref="IBoardGamesSyncService"/> for the controller layer and
/// <see cref="IHostedService"/> so it can run as a background worker.
/// </summary>
public class BoardGamesSyncService(
    IOptions<BoardGameGeekOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<BoardGamesSyncService> logger)
    : SyncServiceBase, IBoardGamesSyncService
{
    private readonly BoardGameGeekOptions _options = options.Value;

    /// <inheritdoc/>
    protected override bool IsTokenConfigured => !string.IsNullOrWhiteSpace(_options.ApiToken) && !string.IsNullOrWhiteSpace(_options.Username);

    /// <inheritdoc/>
    protected override ILogger Logger => logger;

    /// <inheritdoc/>
    protected override string LogName => "BoardGameGeek";

    /// <inheritdoc/>
    protected override async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("BoardGameGeek sync started.");

        // Resolve scoped services (DbContext, BoardGameGeekClient) from a fresh scope.
        await using var scope = scopeFactory.CreateAsyncScope();
        var boardGameGeekClient = scope.ServiceProvider.GetRequiredService<BoardGameGeekClient>();
        var boardGamesRepository = scope.ServiceProvider.GetRequiredService<IBoardGamesRepository>();

        var collectionItems = await boardGameGeekClient.GetCollectionAsync(_options.Username, cancellationToken);
        logger.LogInformation("Fetched {Count} board games from BoardGameGeek.", collectionItems.Count);

        // Batch IDs into groups of 20 for enrichment
        var allDetails = new Dictionary<int, BoardGameGeekThingDetail>();
        var ids = collectionItems.Select(c => c.BoardGameGeekId).ToList();

        for (var i = 0; i < ids.Count; i += 20)
        {
            var batch = ids.Skip(i).Take(20).ToList();
            logger.LogInformation("Fetching thing details for batch {Start}-{End} ({Count} items)", i, i + batch.Count - 1, batch.Count);

            var details = await boardGameGeekClient.GetThingDetailsAsync(batch, cancellationToken);
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
            allDetails.TryGetValue(c.BoardGameGeekId, out var detail);

            var boardGame = new BoardGame
            {
                BoardGameGeekId = c.BoardGameGeekId,
                CoverImageUrl = c.CoverImageUrl,
                Description = detail?.Description,
                Designers = detail?.Designers ?? [],
                Genre = detail?.Category,
                CreatedAt = now,
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
        logger.LogInformation("BoardGameGeek sync completed successfully.");
    }
}
