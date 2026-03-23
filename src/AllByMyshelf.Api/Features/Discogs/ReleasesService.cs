using AllByMyshelf.Api.Common;

namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Implementation of <see cref="IReleasesService"/> that reads from the local database.
/// </summary>
public class ReleasesService(IReleasesRepository repository) : IReleasesService
{
    private const int MaxPageSize = 10000;

    /// <inheritdoc/>
    public async Task<ReleaseDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var release = await repository.GetByIdAsync(id, cancellationToken);
        if (release is null)
            return null;

        return new ReleaseDetailDto
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
            Year = release.Year,
        };
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

        return new ReleaseDetailDto
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
            Year = release.Year,
        };
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

    /// <inheritdoc/>
    public async Task<bool> UpdateNotesAndRatingAsync(Guid id, string? notes, int? rating, CancellationToken cancellationToken)
    {
        return await repository.UpdateNotesAndRatingAsync(id, notes, rating, cancellationToken);
    }
}
