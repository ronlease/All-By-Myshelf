using AllByMyshelf.Api.Models.DTOs;
using AllByMyshelf.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllByMyshelf.Api.Controllers;

/// <summary>
/// Exposes the locally stored vinyl release collection.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class ReleasesController(IReleasesService releasesService) : ControllerBase
{
    /// <summary>
    /// Returns a paginated list of releases from the local database.
    /// </summary>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, maximum 100 (default: 20).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result containing artist, title, year, and format for each release.</returns>
    /// <response code="200">Returns the paginated release list (may be empty if no sync has run).</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReleaseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReleaseDto>>> GetReleases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 1;

        var result = await releasesService.GetReleasesAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }
}
