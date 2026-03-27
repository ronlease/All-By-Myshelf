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
    private volatile string? _phase;
    private volatile int _retryAfterSeconds;
    private volatile string _status = SyncConstants.Statuses.Idle;
    private SyncOptionsDto _syncOptions = new();
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
        Phase: _phase,
        RetryAfterSeconds: _retryAfterSeconds > 0 ? _retryAfterSeconds : null,
        Status: _status,
        Total: _total
    );

    /// <inheritdoc/>
    public SyncOptionsDto SyncOptions => _syncOptions;

    /// <inheritdoc/>
    public async Task<SyncEstimateDto> GetEstimateAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var discogsClient = scope.ServiceProvider.GetRequiredService<DiscogsClient>();
        var releasesRepository = scope.ServiceProvider.GetRequiredService<IReleasesRepository>();

        var apiReleases = await discogsClient.GetCollectionAsync(cancellationToken);
        var existingReleases = await releasesRepository.GetAllByDiscogsIdAsync(cancellationToken);

        var newCount = apiReleases.Count(r => !existingReleases.ContainsKey(r.Id));

        return new SyncEstimateDto(
            CachedReleases: apiReleases.Count - newCount,
            NewReleases: newCount,
            TotalReleases: apiReleases.Count
        );
    }

    /// <inheritdoc/>
    public SyncStartResult TryStartSync(SyncOptionsDto? options = null)
    {
        _syncOptions = options ?? new SyncOptionsDto();
        return base.TryStartSync();
    }

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
        _phase = null;
        _retryAfterSeconds = 0;
        _status = SyncConstants.Statuses.Idle;
        _total = 0;
    }

    /// <summary>
    /// Determines whether a release's detail data should be re-fetched based on the current sync mode.
    /// </summary>
    private bool ShouldRefresh(DateTimeOffset? detailSyncedAt)
    {
        var mode = _syncOptions.Mode;

        if (string.Equals(mode, SyncConstants.Modes.Full, StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(mode, SyncConstants.Modes.Stale, StringComparison.OrdinalIgnoreCase))
        {
            if (detailSyncedAt is null) return true;
            var staleCutoff = DateTimeOffset.UtcNow.AddDays(-_syncOptions.StaleDays);
            return detailSyncedAt < staleCutoff;
        }

        // Incremental: only fetch if not previously synced.
        return detailSyncedAt is null;
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
            _status = SyncConstants.Statuses.Pausing;
        };
        discogsClient.OnRateLimitResume += () =>
        {
            _retryAfterSeconds = 0;
            _status = SyncConstants.Statuses.Resuming;
        };

        _phase = SyncConstants.Phases.Collection;
        var apiReleases = await discogsClient.GetCollectionAsync(cancellationToken);
        logger.LogInformation("Fetched {Count} releases from Discogs.", apiReleases.Count);

        // Load existing releases to skip detail/pricing fetches for complete records.
        var existingReleases = await releasesRepository.GetAllByDiscogsIdAsync(cancellationToken);

        _total = apiReleases.Count;
        _current = 0;
        _status = SyncConstants.Statuses.Syncing;

        var now = DateTimeOffset.UtcNow;
        var entities = new List<Release>(apiReleases.Count);
        var skipped = 0;

        foreach (var r in apiReleases)
        {
            _current++;
            _status = SyncConstants.Statuses.Syncing;

            var (artists, coverImageUrl, format, thumbnailUrl, title, year) = MapBasicReleaseFields(r);

            var release = new Release
            {
                Artists = artists,
                CoverImageUrl = coverImageUrl,
                CreatedAt = now,
                DiscogsId = r.Id,
                Format = format,
                Id = Guid.NewGuid(),
                LastSyncedAt = now,
                ThumbnailUrl = thumbnailUrl,
                Title = title,
                Year = year,
            };

            // Determine whether to fetch detail for this release.
            var isCached = existingReleases.TryGetValue(r.Id, out var cached);
            var shouldFetchDetail = !isCached || ShouldRefresh(cached?.DetailSyncedAt);

            if (isCached && !shouldFetchDetail)
            {
                // Reuse cached detail/pricing fields and skip API calls.
                release.DetailSyncedAt = cached!.DetailSyncedAt;
                release.Genre = cached.Genre;
                release.HighestPrice = cached.HighestPrice;
                release.LowestPrice = cached.LowestPrice;
                release.MedianPrice = cached.MedianPrice;
                release.TrackArtists = cached.TrackArtists;
                release.Tracks = cached.Tracks;
                skipped++;
            }
            else
            {
                // Carry forward cached values first, then overwrite selectively.
                if (isCached)
                {
                    release.DetailSyncedAt = cached!.DetailSyncedAt;
                    release.Genre = cached.Genre;
                    release.HighestPrice = cached.HighestPrice;
                    release.LowestPrice = cached.LowestPrice;
                    release.MedianPrice = cached.MedianPrice;
                    release.TrackArtists = cached.TrackArtists;
                    release.Tracks = cached.Tracks;
                }

                if (_syncOptions.IncludeDetails)
                {
                    _phase = SyncConstants.Phases.Details;
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

                        release.Tracks = detail.Tracklist
                            .Where(t => !string.IsNullOrWhiteSpace(t.Title))
                            .Select(t => new Models.Entities.Track
                            {
                                Artists = t.Artists
                                    .Select(a => StripDisambiguation(a.Name))
                                    .Where(n => !string.IsNullOrWhiteSpace(n))
                                    .ToList(),
                                Id = Guid.NewGuid(),
                                Position = t.Position,
                                ReleaseId = release.Id,
                                Title = t.Title,
                            })
                            .ToList();

                        logger.LogDebug("Fetched detail for Discogs ID {DiscogsId}.", r.Id);
                    }
                }

                if (_syncOptions.IncludePricing)
                {
                    _phase = SyncConstants.Phases.Pricing;
                    var stats = await discogsClient.GetMarketplaceStatsAsync(r.Id, cancellationToken);
                    if (stats is not null)
                    {
                        release.HighestPrice = stats.HighestPrice?.Value;
                        release.LowestPrice = stats.LowestPrice?.Value;
                        release.MedianPrice = stats.MedianPrice?.Value;
                        logger.LogDebug("Fetched marketplace stats for Discogs ID {DiscogsId}.", r.Id);
                    }
                }

                release.DetailSyncedAt = now;
            }
            entities.Add(release);
        }

        logger.LogInformation("Skipped detail/pricing fetches for {Skipped} releases with complete data.", skipped);

        _phase = SyncConstants.Phases.Saving;
        _status = SyncConstants.Statuses.Saving;
        await releasesRepository.UpsertCollectionAsync(entities, cancellationToken);
        logger.LogInformation("Discogs collection sync completed.");

        if (!_syncOptions.IncludeWantlist)
        {
            logger.LogInformation("Wantlist sync skipped per sync options.");
            logger.LogInformation("Discogs sync completed successfully.");
            return;
        }

        logger.LogInformation("Starting wantlist sync...");

        // Sync wantlist
        var wantlistEntities = new List<Models.Entities.WantlistRelease>();
        var wantlistPage = 1;
        _phase = SyncConstants.Phases.Wantlist;
        _status = SyncConstants.Statuses.SyncingWantlist;

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
                    CreatedAt = now,
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

        _status = SyncConstants.Statuses.SavingWantlist;
        await wantlistRepository.UpsertAsync(wantlistEntities, cancellationToken);

        var activeWantlistIds = wantlistEntities.Select(w => w.DiscogsId).ToHashSet();
        await wantlistRepository.RemoveAbsentAsync(activeWantlistIds, cancellationToken);

        logger.LogInformation("Discogs wantlist sync completed. Synced {Count} wantlist items.", wantlistEntities.Count);
        logger.LogInformation("Discogs sync completed successfully.");
    }

}
