using AllByMyshelf.Api.Models.DTOs;
using AllByMyshelf.Api.Repositories;

namespace AllByMyshelf.Api.Services;

/// <summary>
/// Implementation of <see cref="IReleasesService"/> that reads from the local database.
/// </summary>
public class ReleasesService(IReleasesRepository repository) : IReleasesService
{
    private const int MaxPageSize = 100;

    /// <inheritdoc/>
    public async Task<PagedResult<ReleaseDto>> GetReleasesAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        pageSize = Math.Min(pageSize, MaxPageSize);

        var (items, totalCount) = await repository.GetPagedAsync(page, pageSize, cancellationToken);

        var dtos = items.Select(r => new ReleaseDto
        {
            Artist = r.Artist,
            Title = r.Title,
            Year = r.Year,
            Format = r.Format
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
