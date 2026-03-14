using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllByMyshelf.Api.Features.Statistics;

/// <summary>
/// Exposes statistics and analytics for the collection.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/v1/statistics")]
public class StatisticsController(IStatisticsRepository statisticsRepository) : ControllerBase
{
    /// <summary>
    /// Returns the estimated value of the vinyl collection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection value statistics including low, median, and high estimates.</returns>
    /// <response code="200">Returns the collection value statistics.</response>
    [HttpGet("collection-value")]
    [ProducesResponseType(typeof(CollectionValueDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCollectionValue(CancellationToken cancellationToken)
    {
        var result = await statisticsRepository.GetCollectionValueAsync(cancellationToken);
        return Ok(result);
    }
}
