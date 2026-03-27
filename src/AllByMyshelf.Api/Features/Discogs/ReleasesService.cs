using System.Text.RegularExpressions;
using AllByMyshelf.Api.Common;

namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Implementation of <see cref="IReleasesService"/> that reads from the local database.
/// </summary>
public partial class ReleasesService(IReleasesRepository repository, DiscogsClient discogsClient) : IReleasesService
{
    private const int MaxPageSize = 10000;

    /// <inheritdoc/>
    public async Task<ReleaseDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var release = await repository.GetByIdAsync(id, cancellationToken);
        if (release is null)
            return null;

        return MapToDetailDto(release);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DuplicateGroupDto>> GetDuplicatesAsync(CancellationToken cancellationToken)
    {
        var duplicates = await repository.GetDuplicatesAsync(cancellationToken);
        return duplicates.Select(d => new DuplicateGroupDto
        {
            Artists = d.Artists,
            Releases = d.Releases.Select(r => new DuplicateReleaseDto
            {
                DiscogsId = r.DiscogsId,
                Format = r.Format,
                Id = r.Id,
                Year = r.Year
            }).ToList(),
            Title = d.Title
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MaintenanceReleaseDto>> GetIncompleteReleasesAsync(CancellationToken cancellationToken)
    {
        var releases = await repository.GetIncompleteReleasesAsync(cancellationToken);
        return releases.Select(r =>
        {
            var missing = new List<string>();
            if (string.IsNullOrEmpty(r.CoverImageUrl)) missing.Add("Cover Art");
            if (r.Genre is null) missing.Add("Genre");
            if (r.Year is null or 0) missing.Add("Year");
            return new MaintenanceReleaseDto
            {
                Artists = r.Artists,
                DiscogsId = r.DiscogsId,
                Id = r.Id,
                MissingFields = missing,
                ThumbnailUrl = r.ThumbnailUrl,
                Title = r.Title,
            };
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<ReleaseDetailDto?> GetRandomAsync(RandomReleaseFilter? filter, CancellationToken cancellationToken)
    {
        var release = await repository.GetRandomAsync(filter, cancellationToken);
        if (release is null) return null;

        return MapToDetailDto(release);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ReleaseDto>> GetRecentlyAddedAsync(CancellationToken cancellationToken)
    {
        var releases = await repository.GetRecentlyAddedAsync(10, 30, cancellationToken);
        return releases.Select(r => new ReleaseDto
        {
            Artists = r.Artists,
            Format = r.Format,
            Genre = r.Genre,
            Id = r.Id,
            ThumbnailUrl = r.ThumbnailUrl,
            Title = r.Title,
            TrackArtists = r.TrackArtists,
            Year = r.Year,
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<PagedResult<ReleaseDto>> GetReleasesAsync(
        int page, int pageSize, CancellationToken cancellationToken, ReleaseFilter? filter = null)
    {
        pageSize = Math.Min(pageSize, MaxPageSize);

        var (items, totalCount) = await repository.GetPagedAsync(page, pageSize, cancellationToken, filter);

        var dtos = items.Select(r => new ReleaseDto
        {
            Artists = r.Artists,
            Format = r.Format,
            Genre = r.Genre,
            Id = r.Id,
            ThumbnailUrl = r.ThumbnailUrl,
            Title = r.Title,
            TrackArtists = r.TrackArtists,
            Year = r.Year,
        }).ToList();

        return new PagedResult<ReleaseDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static ReleaseDetailDto MapToDetailDto(Models.Entities.Release release) => new()
    {
        Artists = release.Artists,
        CoverImageUrl = release.CoverImageUrl,
        DiscogsId = release.DiscogsId,
        Format = release.Format,
        Genre = release.Genre,
        HighestPrice = release.HighestPrice,
        Id = release.Id,
        LowestPrice = release.LowestPrice,
        MedianPrice = release.MedianPrice,
        Notes = release.Notes,
        Rating = release.Rating,
        Title = release.Title,
        TrackArtists = release.TrackArtists,
        Tracks = release.Tracks
            .Select(t => new TrackDto(t.Artists, t.Position, t.Title))
            .ToList(),
        Year = release.Year,
    };

    /// <inheritdoc/>
    public async Task<ReleaseDetailDto?> ResyncAsync(Guid id, CancellationToken cancellationToken)
    {
        var release = await repository.GetByIdAsync(id, cancellationToken);
        if (release is null)
            return null;

        var detail = await discogsClient.GetReleaseDetailAsync(release.DiscogsId, cancellationToken);
        if (detail is not null)
        {
            release.Genre = detail.Genres.FirstOrDefault();

            release.TrackArtists = detail.Tracklist
                .SelectMany(t => t.Artists)
                .Select(a => StripDisambiguation(a.Name))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(n => !release.Artists.Contains(n, StringComparer.OrdinalIgnoreCase))
                .ToList();

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
        }

        var stats = await discogsClient.GetMarketplaceStatsAsync(release.DiscogsId, cancellationToken);
        if (stats is not null)
        {
            release.HighestPrice = stats.HighestPrice?.Value;
            release.LowestPrice = stats.LowestPrice?.Value;
            release.MedianPrice = stats.MedianPrice?.Value;
        }

        release.DetailSyncedAt = DateTimeOffset.UtcNow;

        await repository.UpdateResyncedReleaseAsync(release, cancellationToken);

        return MapToDetailDto(release);
    }

    [GeneratedRegex(@"\s*\(\d+\)$")]
    private static partial Regex DisambiguationPattern();

    private static string StripDisambiguation(string name) =>
        DisambiguationPattern().Replace(name, "").Trim();

    /// <inheritdoc/>
    public async Task<bool> UpdateNotesAndRatingAsync(Guid id, string? notes, int? rating, CancellationToken cancellationToken)
    {
        return await repository.UpdateNotesAndRatingAsync(id, notes, rating, cancellationToken);
    }
}
