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
    /// Returns a randomly selected release, optionally filtered by format, genre, or decade.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="decade">Optional decade filter in the form "1980s".</param>
    /// <param name="format">Optional case-insensitive contains filter on the format.</param>
    /// <param name="genre">Optional case-insensitive contains filter on the genre.</param>
    /// <returns>A randomly selected release matching the specified criteria.</returns>
    /// <response code="200">Returns a randomly selected release.</response>
    /// <response code="404">No releases match the specified criteria.</response>
    [HttpGet("random")]
    [ProducesResponseType(typeof(ReleaseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReleaseDetailDto>> GetRandom(
        CancellationToken cancellationToken = default,
        [FromQuery] string? decade = null,
        [FromQuery] string? format = null,
        [FromQuery] string? genre = null)
    {
        var filter = new RandomReleaseFilter(
            Decade: decade,
            Format: format,
            Genre: genre);

        var result = await releasesService.GetRandomAsync(filter, cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Returns the full detail for a single release.
    /// </summary>
    /// <param name="id">The application-generated GUID for the release.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full release detail including label, country, genre, styles, and notes.</returns>
    /// <response code="200">Returns the release detail.</response>
    /// <response code="404">No release with the specified ID was found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReleaseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReleaseDetailDto>> GetRelease(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await releasesService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Returns a paginated list of releases from the local database.
    /// </summary>
    /// <param name="artist">Optional case-insensitive contains filter on the artist name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="format">Optional case-insensitive contains filter on the format.</param>
    /// <param name="genre">Optional case-insensitive contains filter on the genre.</param>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, maximum 10000 (default: 20).</param>
    /// <param name="search">Optional full-text search across artist, format, genre, title, and year.</param>
    /// <param name="title">Optional case-insensitive contains filter on the title.</param>
    /// <param name="year">Optional case-insensitive contains filter on the year.</param>
    /// <returns>A paginated result containing artist, title, year, format, and genre for each release.</returns>
    /// <response code="200">Returns the paginated release list (may be empty if no sync has run).</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReleaseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReleaseDto>>> GetReleases(
        [FromQuery] string? artist = null,
        CancellationToken cancellationToken = default,
        [FromQuery] string? format = null,
        [FromQuery] string? genre = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? title = null,
        [FromQuery] string? year = null)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 1;

        var filter = new ReleaseFilter(
            Artist: artist,
            Format: format,
            Genre: genre,
            Search: search,
            Title: title,
            Year: year);

        var result = await releasesService.GetReleasesAsync(page, pageSize, cancellationToken, filter);
        return Ok(result);
    }
}
