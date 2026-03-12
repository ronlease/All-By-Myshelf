using AllByMyshelf.Api.Models.DTOs;
using AllByMyshelf.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllByMyshelf.Api.Controllers;

/// <summary>
/// Exposes an endpoint to manually trigger a Discogs collection sync.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class SyncController(ISyncService syncService) : ControllerBase
{
    /// <summary>Returns the current sync progress.</summary>
    /// <response code="200">Current sync status.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SyncProgressDto), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(syncService.Progress);
    }

    /// <summary>
    /// Triggers a manual sync of the Discogs collection.
    /// </summary>
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
    public IActionResult TriggerSync()
    {
        var result = syncService.TryStartSync();

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
