using System.Text;
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
    IOptionsMonitor<HardcoverOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<BooksSyncService> logger)
    : SyncServiceBase, IBooksSyncService
{
    /// <inheritdoc/>
    /// <remarks>
    /// Read through <see cref="IOptionsMonitor{TOptions}.CurrentValue"/> on every call so
    /// credentials saved via the Settings page apply without restarting the API (ABM-075).
    /// This service is a singleton, so a captured <c>IOptions.Value</c> would stay stale.
    /// </remarks>
    protected override bool IsTokenConfigured =>
        !string.IsNullOrWhiteSpace(options.CurrentValue.ApiToken);

    /// <inheritdoc/>
    protected override ILogger Logger => logger;

    /// <inheritdoc/>
    protected override string LogName => "Hardcover";

    /// <summary>
    /// Extracts the most representative genre from Hardcover's <c>cached_tags</c> blob.
    /// </summary>
    /// <remarks>
    /// Hardcover returns each category as an array of tag objects, not strings:
    /// <c>"Genre": [{ "tag": "Horror", "count": 3, "tagSlug": "horror", ... }]</c>.
    /// The tag with the highest <c>count</c> wins, since that is the classification most
    /// readers agreed on; ties keep the first occurrence. A plain string array is still
    /// accepted so the parser survives a future shape change.
    /// </remarks>
    private static string? ParseGenre(JsonElement? cachedTags)
    {
        if (cachedTags is null || cachedTags.Value.ValueKind != JsonValueKind.Object)
            return null;

        if (!cachedTags.Value.TryGetProperty("Genre", out var genreArray))
            return null;

        if (genreArray.ValueKind != JsonValueKind.Array)
            return null;

        string? best = null;
        var bestCount = int.MinValue;

        foreach (var element in genreArray.EnumerateArray())
        {
            var (name, count) = ReadTag(element);

            if (string.IsNullOrWhiteSpace(name) || count <= bestCount)
                continue;

            best = name;
            bestCount = count;
        }

        return best is null ? null : NormalizeGenre(best);
    }

    /// <summary>
    /// Reads a tag name and its vote count from either a tag object or a bare string.
    /// </summary>
    private static (string? Name, int Count) ReadTag(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return (element.GetString(), 0);

        if (element.ValueKind != JsonValueKind.Object)
            return (null, 0);

        var name = element.TryGetProperty("tag", out var tag) && tag.ValueKind == JsonValueKind.String
            ? tag.GetString()
            : null;

        var count = element.TryGetProperty("count", out var countElement)
                    && countElement.ValueKind == JsonValueKind.Number
                    && countElement.TryGetInt32(out var parsed)
            ? parsed
            : 0;

        return (name, count);
    }

    /// <summary>
    /// Strips the decorative leading symbols Hardcover allows on tag names,
    /// so "💀 Horror" is stored as "Horror".
    /// </summary>
    private static string NormalizeGenre(string genre)
    {
        var index = 0;

        while (index < genre.Length
               && Rune.TryGetRuneAt(genre, index, out var rune)
               && !Rune.IsLetterOrDigit(rune))
        {
            index += rune.Utf16SequenceLength;
        }

        var trimmed = genre[index..].Trim();

        // A tag made up entirely of symbols keeps its original text.
        return trimmed.Length > 0 ? trimmed : genre.Trim();
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
