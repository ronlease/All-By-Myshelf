using AllByMyshelf.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllByMyshelf.Api.Features.Wantlist;

/// <summary>
/// Exposes the locally stored Discogs wantlist.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class WantlistController(IWantlistRepository wantlistRepository) : ControllerBase
{
    /// <summary>
    /// Returns a paginated list of wantlist releases from the local database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, maximum 10000 (default: 20).</param>
    /// <returns>A paginated result containing artist, title, year, format, and genre for each wantlist release.</returns>
    /// <response code="200">Returns the paginated wantlist (may be empty if no sync has run).</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WantlistReleaseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WantlistReleaseDto>>> GetWantlist(
        CancellationToken cancellationToken = default,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 1;

        const int maxPageSize = 10000;
        pageSize = Math.Min(pageSize, maxPageSize);

        var (items, totalCount) = await wantlistRepository.GetPagedAsync(page, pageSize, cancellationToken);

        var dtos = items.Select(w => new WantlistReleaseDto
        {
            Artists = w.Artists,
            CoverImageUrl = w.CoverImageUrl,
            DiscogsId = w.DiscogsId,
            Format = w.Format,
            Genre = w.Genre,
            Id = w.Id,
            ThumbnailUrl = w.ThumbnailUrl,
            Title = w.Title,
            Year = w.Year
        }).ToList();

        return Ok(new PagedResult<WantlistReleaseDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }
}
