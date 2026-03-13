using AllByMyshelf.Api.Models.DTOs;
using AllByMyshelf.Api.Repositories;

namespace AllByMyshelf.Api.Services;

/// <summary>
/// Implementation of <see cref="IReleasesService"/> that reads from the local database.
/// </summary>
public class ReleasesService(IReleasesRepository repository) : IReleasesService
{
    private const int MaxPageSize = 10000;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MaintenanceReleaseDto>> GetIncompleteReleasesAsync(CancellationToken cancellationToken)
    {
        var releases = await repository.GetIncompleteReleasesAsync(cancellationToken);
        return releases.Select(r =>
        {
            var missing = new List<string>();
            if (r.CoverImageUrl is null) missing.Add("Cover Art");
            if (r.Genre is null) missing.Add("Genre");
            if (r.HighestPrice is null || r.LowestPrice is null || r.MedianPrice is null) missing.Add("Pricing");
            if (r.Year is null) missing.Add("Year");
            return new MaintenanceReleaseDto
            {
                Artist = r.Artist,
                DiscogsId = r.DiscogsId,
                Id = r.Id,
                MissingFields = missing,
                ThumbnailUrl = r.ThumbnailUrl,
                Title = r.Title,
            };
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<ReleaseDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var release = await repository.GetByIdAsync(id, cancellationToken);
        if (release is null)
            return null;

        return new ReleaseDetailDto
        {
            Artist = release.Artist,
            CoverImageUrl = release.CoverImageUrl,
            DiscogsId = release.DiscogsId,
            Format = release.Format,
            Genre = release.Genre,
            HighestPrice = release.HighestPrice,
            Id = release.Id,
            LowestPrice = release.LowestPrice,
            MedianPrice = release.MedianPrice,
            Title = release.Title,
            Year = release.Year,
        };
    }

    /// <inheritdoc/>
    public async Task<ReleaseDetailDto?> GetRandomAsync(RandomReleaseFilter? filter, CancellationToken cancellationToken)
    {
        var release = await repository.GetRandomAsync(filter, cancellationToken);
        if (release is null) return null;

        return new ReleaseDetailDto
        {
            Artist = release.Artist,
            CoverImageUrl = release.CoverImageUrl,
            DiscogsId = release.DiscogsId,
            Format = release.Format,
            Genre = release.Genre,
            HighestPrice = release.HighestPrice,
            Id = release.Id,
            LowestPrice = release.LowestPrice,
            MedianPrice = release.MedianPrice,
            Title = release.Title,
            Year = release.Year,
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ReleaseDto>> GetRecentlyAddedAsync(CancellationToken cancellationToken)
    {
        var releases = await repository.GetRecentlyAddedAsync(10, 30, cancellationToken);
        return releases.Select(r => new ReleaseDto
        {
            Artist = r.Artist,
            Format = r.Format,
            Genre = r.Genre,
            Id = r.Id,
            ThumbnailUrl = r.ThumbnailUrl,
            Title = r.Title,
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
            Artist = r.Artist,
            Format = r.Format,
            Genre = r.Genre,
            Id = r.Id,
            ThumbnailUrl = r.ThumbnailUrl,
            Title = r.Title,
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
}
