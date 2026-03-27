using AllByMyshelf.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Exposes an endpoint to manually trigger a Discogs collection sync.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class SyncController(ISyncService syncService) : ControllerBase
{
    /// <summary>
    /// Returns an estimate of the sync scope for the current collection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Estimated sync scope with new/cached release counts.</returns>
    /// <response code="200">Returns the sync estimate.</response>
    [HttpGet("estimate")]
    [ProducesResponseType(typeof(SyncEstimateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEstimate(CancellationToken cancellationToken = default)
    {
        var estimate = await syncService.GetEstimateAsync(cancellationToken);
        return Ok(estimate);
    }

    /// <summary>Returns the current sync progress.</summary>
    /// <response code="200">Current sync status.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SyncProgressDto), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(syncService.Progress);
    }

    /// <summary>
    /// Triggers a manual sync of the Discogs collection with optional configuration.
    /// </summary>
    /// <param name="options">Optional sync configuration. Defaults to incremental sync with all data categories.</param>
    /// <returns>
    /// 202 Accepted when the sync starts successfully,
    /// 409 Conflict when a sync is already running,
    /// 503 Service Unavailable when the Discogs token is not configured.
    /// </returns>
    /// <response code="202">Sync started. The operation runs asynchronously in the background.</response>
    /// <response code="409">A sync is already in progress.</response>
    /// <response code="503">The Discogs personal access token is not configured.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult TriggerSync([FromBody] SyncOptionsDto? options = null)
    {
        var result = syncService.TryStartSync(options);

        return result switch
        {
            SyncStartResult.Started => Accepted(new { message = "Sync started." }),

            SyncStartResult.AlreadyRunning => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Sync Already Running",
                Detail = "A Discogs collection sync is already in progress. Please wait for it to complete."
            }),

            SyncStartResult.TokenNotConfigured => StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Discogs Token Not Configured",
                    Detail = "The Discogs personal access token is not configured. " +
                             "Set it via dotnet user-secrets with key 'Discogs:PersonalAccessToken'."
                }),

            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
