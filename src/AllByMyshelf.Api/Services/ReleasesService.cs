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
            Id = release.Id,
            Title = release.Title,
            Year = release.Year,
        };
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
