using AllByMyshelf.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllByMyshelf.Api.Features.Bgg;

/// <summary>
/// Exposes the locally stored board game collection and sync operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class BoardGamesController(
    IBoardGamesService boardGamesService,
    IBoardGamesSyncService boardGamesSyncService) : ControllerBase
{
    /// <summary>
    /// Returns the full detail for a single board game.
    /// </summary>
    /// <param name="id">The application-generated GUID for the board game.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full board game detail.</returns>
    /// <response code="200">Returns the board game detail.</response>
    /// <response code="404">No board game with the specified ID was found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BoardGameDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardGameDetailDto>> GetBoardGame(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await boardGamesService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Returns a paginated list of board games from the local database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="designer">Optional case-insensitive contains filter on the designer name.</param>
    /// <param name="genre">Optional case-insensitive contains filter on the genre.</param>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, maximum 10000 (default: 20).</param>
    /// <param name="playerCount">Optional filter for games that support this number of players.</param>
    /// <param name="title">Optional case-insensitive contains filter on the title.</param>
    /// <param name="year">Optional case-insensitive contains filter on the year.</param>
    /// <returns>A paginated result containing board game information.</returns>
    /// <response code="200">Returns the paginated board game list (may be empty if no sync has run).</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BoardGameDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BoardGameDto>>> GetBoardGames(
        CancellationToken cancellationToken = default,
        [FromQuery] string? designer = null,
        [FromQuery] string? genre = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? playerCount = null,
        [FromQuery] string? title = null,
        [FromQuery] string? year = null)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 1;

        var filter = new BoardGameFilter(
            Designer: designer,
            Genre: genre,
            PlayerCount: playerCount,
            Title: title,
            Year: year);

        var result = await boardGamesService.GetBoardGamesAsync(page, pageSize, cancellationToken, filter);
        return Ok(result);
    }

    /// <summary>
    /// Returns a randomly selected board game.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A randomly selected board game.</returns>
    /// <response code="200">Returns a randomly selected board game.</response>
    /// <response code="404">No board games exist in the collection.</response>
    [HttpGet("random")]
    [ProducesResponseType(typeof(BoardGameDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardGameDto>> GetRandom(
        CancellationToken cancellationToken = default)
    {
        var result = await boardGamesService.GetRandomAsync(cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Returns whether a BGG sync is currently running.
    /// </summary>
    /// <returns>An object containing the sync running status.</returns>
    /// <response code="200">Returns the current sync status.</response>
    [HttpGet("sync/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetSyncStatus() =>
        Ok(new { isRunning = boardGamesSyncService.IsSyncRunning });

    /// <summary>
    /// Triggers a manual sync of the BoardGameGeek collection.
    /// </summary>
    /// <returns>
    /// 202 Accepted when the sync starts successfully,
    /// 409 Conflict when a sync is already running,
    /// 503 Service Unavailable when the BGG username is not configured.
    /// </returns>
    /// <response code="202">Sync started. The operation runs asynchronously in the background.</response>
    /// <response code="409">A sync is already in progress.</response>
    /// <response code="503">The BGG username is not configured.</response>
    [HttpPost("sync")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult TriggerSync()
    {
        var result = boardGamesSyncService.TryStartSync();

        return result switch
        {
            SyncStartResult.Started => Accepted(new { message = "BGG sync started." }),

            SyncStartResult.AlreadyRunning => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Sync Already Running",
                Detail = "A BGG collection sync is already in progress. Please wait for it to complete."
            }),

            SyncStartResult.TokenNotConfigured => StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "BGG Username Not Configured",
                    Detail = "The BGG username is not configured. " +
                             "Set it via dotnet user-secrets with key 'Bgg:Username'."
                }),

            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
