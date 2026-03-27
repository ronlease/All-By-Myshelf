using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllByMyshelf.Api.Features.Discogs;

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
    /// Returns groups of releases that share the same artist and title but have different Discogs IDs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of duplicate groups, each containing the shared artist/title and a list of releases.</returns>
    /// <response code="200">Returns the list of duplicate groups (may be empty).</response>
    [HttpGet("duplicates")]
    [ProducesResponseType(typeof(IReadOnlyList<DuplicateGroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DuplicateGroupDto>>> GetDuplicates(
        CancellationToken cancellationToken = default)
    {
        var result = await releasesService.GetDuplicatesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns releases that have incomplete data and may need manual attention.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of releases with missing or incomplete data.</returns>
    /// <response code="200">Returns the list of incomplete releases (may be empty).</response>
    [HttpGet("maintenance")]
    [ProducesResponseType(typeof(IReadOnlyList<MaintenanceReleaseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MaintenanceReleaseDto>>> GetMaintenanceReleases(
        CancellationToken cancellationToken = default)
    {
        var result = await releasesService.GetIncompleteReleasesAsync(cancellationToken);
        return Ok(result);
    }

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
    /// Returns the most recently added releases.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recently added releases.</returns>
    /// <response code="200">Returns the list of recently added releases (may be empty).</response>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyList<ReleaseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReleaseDto>>> GetRecentlyAdded(
        CancellationToken cancellationToken = default)
    {
        var result = await releasesService.GetRecentlyAddedAsync(cancellationToken);
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
    /// Re-syncs a single release's detail and pricing data from Discogs.
    /// </summary>
    /// <param name="id">The application-generated GUID for the release.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated release detail after re-sync.</returns>
    /// <response code="200">Returns the updated release detail.</response>
    /// <response code="404">No release with the specified ID was found.</response>
    [HttpPost("{id:guid}/resync")]
    [ProducesResponseType(typeof(ReleaseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReleaseDetailDto>> ResyncRelease(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await releasesService.ResyncAsync(id, cancellationToken);
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

    /// <summary>
    /// Updates the notes and rating for a specific release.
    /// </summary>
    /// <param name="id">The application-generated GUID for the release.</param>
    /// <param name="dto">The notes and rating values to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success, 400 Bad Request if rating is invalid, 404 Not Found if release does not exist.</returns>
    /// <response code="204">The notes and rating were updated successfully.</response>
    /// <response code="400">The rating value is invalid (must be 1-5 or null).</response>
    /// <response code="404">No release with the specified ID was found.</response>
    [HttpPut("{id:guid}/notes-rating")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNotesAndRating(
        Guid id,
        [FromBody] UpdateNotesRatingDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Rating.HasValue && (dto.Rating.Value < 1 || dto.Rating.Value > 5))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Rating",
                Detail = "Rating must be between 1 and 5, or null."
            });
        }

        // Sanitize notes with newlines preserved
        var sanitizedNotes = dto.Notes is not null
            ? InputSanitizer.Sanitize(dto.Notes, maxLength: 2000, preserveNewlines: true)
            : null;

        var updated = await releasesService.UpdateNotesAndRatingAsync(id, sanitizedNotes, dto.Rating, cancellationToken);
        if (!updated)
            return NotFound();

        return NoContent();
    }
}
