using System.Text.Json;
using AllByMyshelf.Api.Configuration;
using AllByMyshelf.Api.Infrastructure.ExternalApis;
using AllByMyshelf.Api.Models.Entities;
using AllByMyshelf.Api.Repositories;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Services;

/// <summary>
/// Singleton service that coordinates triggering and executing a Hardcover collection sync.
/// Implements <see cref="IBooksSyncService"/> for the controller layer and
/// <see cref="IHostedService"/> so it can run as a background worker.
/// </summary>
public class BooksSyncService(
    IOptions<HardcoverOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<BooksSyncService> logger)
    : BackgroundService, IBooksSyncService
{
    private readonly HardcoverOptions _options = options.Value;

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
                logger.LogInformation("Hardcover sync cancelled due to application shutdown.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Hardcover sync failed.");
            }
            finally
            {
                Volatile.Write(ref _syncRunning, 0);
            }
        }
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Hardcover sync started.");

        // Resolve scoped services (DbContext, HardcoverClient) from a fresh scope.
        await using var scope = scopeFactory.CreateAsyncScope();
        var hardcoverClient = scope.ServiceProvider.GetRequiredService<HardcoverClient>();
        var booksRepository = scope.ServiceProvider.GetRequiredService<IBooksRepository>();

        var apiBooks = await hardcoverClient.GetReadBooksAsync(cancellationToken);
        logger.LogInformation("Fetched {Count} books from Hardcover.", apiBooks.Count);

        var now = DateTimeOffset.UtcNow;
        var entities = new List<Book>(apiBooks.Count);

        foreach (var b in apiBooks)
        {
            var author = b.Contributions?.FirstOrDefault()?.Author?.Name;
            var coverImageUrl = b.Image?.Url;
            var genre = ParseGenre(b.CachedTags);

            int? year = null;
            if (!string.IsNullOrWhiteSpace(b.ReleaseDate) &&
                DateTime.TryParse(b.ReleaseDate, out var releaseDate))
            {
                year = releaseDate.Year;
            }

            var book = new Book
            {
                Author = author,
                CoverImageUrl = coverImageUrl,
                Genre = genre,
                HardcoverId = b.Id,
                Id = Guid.NewGuid(),
                LastSyncedAt = now,
                Title = b.Title ?? "Unknown Title",
                Year = year
            };

            entities.Add(book);
        }

        await booksRepository.UpsertCollectionAsync(entities, cancellationToken);
        logger.LogInformation("Hardcover sync completed successfully.");
    }

    private static string? ParseGenre(JsonElement? cachedTags)
    {
        if (cachedTags is null || cachedTags.Value.ValueKind != JsonValueKind.Object)
            return null;

        if (!cachedTags.Value.TryGetProperty("Genre", out var genreArray))
            return null;

        if (genreArray.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var element in genreArray.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.String)
                return element.GetString();
        }

        return null;
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
