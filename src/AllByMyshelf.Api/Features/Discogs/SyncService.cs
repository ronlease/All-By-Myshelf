using System.Text.RegularExpressions;
using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Models.Entities;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Singleton service that coordinates triggering and executing a Discogs collection sync.
/// Implements <see cref="ISyncService"/> for the controller layer and
/// <see cref="IHostedService"/> so it can run as a background worker.
/// </summary>
public partial class SyncService(
    IOptions<DiscogsOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<SyncService> logger)
    : SyncServiceBase, ISyncService
{
    private readonly DiscogsOptions _options = options.Value;

    private volatile int _current;
    private volatile int _retryAfterSeconds;
    private volatile string _status = "idle";
    private volatile int _total;

    /// <inheritdoc/>
    protected override bool IsTokenConfigured => !string.IsNullOrWhiteSpace(_options.PersonalAccessToken);

    /// <inheritdoc/>
    protected override ILogger Logger => logger;

    /// <inheritdoc/>
    protected override string LogName => "Discogs";

    /// <inheritdoc/>
    public SyncProgressDto Progress => new(
        IsRunning: IsSyncRunning,
        Current: _current,
        RetryAfterSeconds: _retryAfterSeconds > 0 ? _retryAfterSeconds : null,
        Status: _status,
        Total: _total
    );

    /// <summary>
    /// Maps common fields from Discogs BasicInformation to a tuple of properties
    /// shared between Release and WantlistRelease entities.
    /// </summary>
    /// <summary>
    /// Strips Discogs disambiguation suffixes like " (2)" from artist names.
    /// </summary>
    private static string StripDisambiguation(string name) =>
        DisambiguationPattern().Replace(name, "").Trim();

    [GeneratedRegex(@"\s*\(\d+\)$")]
    private static partial Regex DisambiguationPattern();

    private static (
        List<string> Artists,
        string? CoverImageUrl,
        string Format,
        string? ThumbnailUrl,
        string Title,
        int? Year
    ) MapBasicReleaseFields(DiscogsRelease r)
    {
        var artists = r.BasicInformation.Artists
            .Select(a => StripDisambiguation(a.Name))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();
        if (artists.Count == 0) artists = ["Unknown Artist"];

        var format = r.BasicInformation.Formats.FirstOrDefault()?.Name ?? string.Empty;
        var year = r.BasicInformation.Year == 0 ? (int?)null : r.BasicInformation.Year;

        return (artists, r.BasicInformation.CoverImage, format, r.BasicInformation.Thumb, r.BasicInformation.Title, year);
    }

    /// <inheritdoc/>
    protected override void OnSyncCompleted()
    {
        _current = 0;
        _retryAfterSeconds = 0;
        _status = "idle";
        _total = 0;
    }

    /// <inheritdoc/>
    protected override async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Discogs sync started.");

        // Resolve scoped services (DbContext, DiscogsClient) from a fresh scope.
        await using var scope = scopeFactory.CreateAsyncScope();
        var discogsClient = scope.ServiceProvider.GetRequiredService<DiscogsClient>();
        var releasesRepository = scope.ServiceProvider.GetRequiredService<IReleasesRepository>();
        var wantlistRepository = scope.ServiceProvider.GetRequiredService<Features.Wantlist.IWantlistRepository>();

        discogsClient.OnRateLimitPause += seconds =>
        {
            _retryAfterSeconds = seconds;
            _status = "pausing";
        };
        discogsClient.OnRateLimitResume += () =>
        {
            _retryAfterSeconds = 0;
            _status = "resuming";
        };

        var apiReleases = await discogsClient.GetCollectionAsync(cancellationToken);
        logger.LogInformation("Fetched {Count} releases from Discogs.", apiReleases.Count);

        // Load existing releases to skip detail/pricing fetches for complete records.
        var existingReleases = await releasesRepository.GetAllByDiscogsIdAsync(cancellationToken);

        _total = apiReleases.Count;
        _current = 0;
        _status = "syncing";

        var now = DateTimeOffset.UtcNow;
        var entities = new List<Release>(apiReleases.Count);
        var skipped = 0;

        foreach (var r in apiReleases)
        {
            _current++;
            _status = "syncing";

            var (artists, coverImageUrl, format, thumbnailUrl, title, year) = MapBasicReleaseFields(r);

            var release = new Release
            {
                Artists = artists,
                CoverImageUrl = coverImageUrl,
                DiscogsId = r.Id,
                Format = format,
                Id = Guid.NewGuid(),
                LastSyncedAt = now,
                ThumbnailUrl = thumbnailUrl,
                Title = title,
                Year = year,
            };

            // If this release was previously synced, reuse its detail/pricing
            // fields and skip the expensive per-release API calls.
            if (existingReleases.TryGetValue(r.Id, out var cached))
            {
                release.Genre = cached.Genre;
                release.HighestPrice = cached.HighestPrice;
                release.LowestPrice = cached.LowestPrice;
                release.MedianPrice = cached.MedianPrice;
                release.TrackArtists = cached.TrackArtists;
                skipped++;
            }
            else
            {
                var detail = await discogsClient.GetReleaseDetailAsync(r.Id, cancellationToken);
                if (detail is not null)
                {
                    release.Genre = detail.Genres.FirstOrDefault();

                    var trackArtists = detail.Tracklist
                        .SelectMany(t => t.Artists)
                        .Select(a => StripDisambiguation(a.Name))
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Where(n => !artists.Contains(n, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                    release.TrackArtists = trackArtists;

                    logger.LogDebug("Fetched detail for Discogs ID {DiscogsId}.", r.Id);
                }

                var stats = await discogsClient.GetMarketplaceStatsAsync(r.Id, cancellationToken);
                if (stats is not null)
                {
                    release.HighestPrice = stats.HighestPrice?.Value;
                    release.LowestPrice = stats.LowestPrice?.Value;
                    release.MedianPrice = stats.MedianPrice?.Value;
                    logger.LogDebug("Fetched marketplace stats for Discogs ID {DiscogsId}.", r.Id);
                }
            }
            entities.Add(release);
        }

        logger.LogInformation("Skipped detail/pricing fetches for {Skipped} releases with complete data.", skipped);

        _status = "saving";
        await releasesRepository.UpsertCollectionAsync(entities, cancellationToken);
        logger.LogInformation("Discogs collection sync completed. Starting wantlist sync...");

        // Sync wantlist
        var wantlistEntities = new List<Models.Entities.WantlistRelease>();
        var wantlistPage = 1;
        _status = "syncing wantlist";

        while (true)
        {
            var pageData = await discogsClient.GetWantlistPageAsync(_options.Username!, wantlistPage, cancellationToken);
            if (pageData?.Releases is null || pageData.Releases.Count == 0)
                break;

            foreach (var r in pageData.Releases)
            {
                var (artists, coverImageUrl, format, thumbnailUrl, title, year) = MapBasicReleaseFields(r);

                var wantlistRelease = new Models.Entities.WantlistRelease
                {
                    Artists = artists,
                    CoverImageUrl = coverImageUrl,
                    DiscogsId = r.Id,
                    Format = format,
                    Id = Guid.NewGuid(),
                    LastSyncedAt = now,
                    ThumbnailUrl = thumbnailUrl,
                    Title = title,
                    Year = year,
                };

                // Fetch extended detail for genre
                var detail = await discogsClient.GetReleaseDetailAsync(r.Id, cancellationToken);
                if (detail is not null)
                {
                    wantlistRelease.Genre = detail.Genres.FirstOrDefault();
                }

                wantlistEntities.Add(wantlistRelease);
            }

            if (wantlistPage >= pageData.Pagination.Pages)
                break;

            wantlistPage++;
        }

        _status = "saving wantlist";
        await wantlistRepository.UpsertAsync(wantlistEntities, cancellationToken);

        var activeWantlistIds = wantlistEntities.Select(w => w.DiscogsId).ToHashSet();
        await wantlistRepository.RemoveAbsentAsync(activeWantlistIds, cancellationToken);

        logger.LogInformation("Discogs wantlist sync completed. Synced {Count} wantlist items.", wantlistEntities.Count);
        logger.LogInformation("Discogs sync completed successfully.");
    }

}
