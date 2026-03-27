using System.Text.Json;
using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Models.Entities;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Features.Hardcover;

/// <summary>
/// Singleton service that coordinates triggering and executing a Hardcover collection sync.
/// Implements <see cref="IBooksSyncService"/> for the controller layer and
/// <see cref="IHostedService"/> so it can run as a background worker.
/// </summary>
public class BooksSyncService(
    IOptions<HardcoverOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<BooksSyncService> logger)
    : SyncServiceBase, IBooksSyncService
{
    private readonly HardcoverOptions _options = options.Value;

    /// <inheritdoc/>
    protected override bool IsTokenConfigured => !string.IsNullOrWhiteSpace(_options.ApiToken);

    /// <inheritdoc/>
    protected override ILogger Logger => logger;

    /// <inheritdoc/>
    protected override string LogName => "Hardcover";

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
    protected override async Task RunSyncAsync(CancellationToken cancellationToken)
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
            var authors = b.Contributions?
                .Select(c => c.Author?.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Cast<string>()
                .ToList() ?? [];
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
                Authors = authors,
                CoverImageUrl = coverImageUrl,
                CreatedAt = now,
                Genre = genre,
                HardcoverId = b.Id,
                Id = Guid.NewGuid(),
                LastSyncedAt = now,
                Slug = b.Slug,
                Title = b.Title ?? "Unknown Title",
                Year = year
            };

            entities.Add(book);
        }

        await booksRepository.UpsertCollectionAsync(entities, cancellationToken);
        logger.LogInformation("Hardcover sync completed successfully.");
    }
}
