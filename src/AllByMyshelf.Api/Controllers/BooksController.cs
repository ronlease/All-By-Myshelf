using AllByMyshelf.Api.Models.DTOs;
using AllByMyshelf.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllByMyshelf.Api.Controllers;

/// <summary>
/// Exposes the locally stored book collection and sync operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class BooksController(
    IBooksService booksService,
    IBooksSyncService booksSyncService) : ControllerBase
{
    /// <summary>
    /// Returns a paginated list of books from the local database.
    /// </summary>
    /// <param name="author">Optional case-insensitive contains filter on the author name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="genre">Optional case-insensitive contains filter on the genre.</param>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, maximum 10000 (default: 20).</param>
    /// <param name="title">Optional case-insensitive contains filter on the title.</param>
    /// <param name="year">Optional case-insensitive contains filter on the year.</param>
    /// <returns>A paginated result containing author, title, year, and genre for each book.</returns>
    /// <response code="200">Returns the paginated book list (may be empty if no sync has run).</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BookDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BookDto>>> GetBooks(
        [FromQuery] string? author = null,
        CancellationToken cancellationToken = default,
        [FromQuery] string? genre = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? title = null,
        [FromQuery] string? year = null)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 1;

        var filter = new BookFilter(
            Author: author,
            Genre: genre,
            Title: title,
            Year: year);

        var result = await booksService.GetBooksAsync(page, pageSize, cancellationToken, filter);
        return Ok(result);
    }

    /// <summary>
    /// Triggers a manual sync of the Hardcover read books collection.
    /// </summary>
    /// <returns>
    /// 202 Accepted when the sync starts successfully,
    /// 409 Conflict when a sync is already running,
    /// 503 Service Unavailable when the Hardcover token is not configured.
    /// </returns>
    /// <response code="202">Sync started. The operation runs asynchronously in the background.</response>
    /// <response code="409">A sync is already in progress.</response>
    /// <response code="503">The Hardcover API token is not configured.</response>
    [HttpPost("sync")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult TriggerSync()
    {
        var result = booksSyncService.TryStartSync();

        return result switch
        {
            SyncStartResult.Started => Accepted(new { message = "Hardcover sync started." }),

            SyncStartResult.AlreadyRunning => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Sync Already Running",
                Detail = "A Hardcover collection sync is already in progress. Please wait for it to complete."
            }),

            SyncStartResult.TokenNotConfigured => StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Hardcover Token Not Configured",
                    Detail = "The Hardcover API token is not configured. " +
                             "Set it via dotnet user-secrets with key 'Hardcover:ApiToken'."
                }),

            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
