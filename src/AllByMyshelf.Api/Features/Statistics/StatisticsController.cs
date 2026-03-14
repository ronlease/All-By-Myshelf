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

    /// <summary>
    /// Returns unified statistics for both records and books collections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unified statistics including breakdowns by format, genre, and decade.</returns>
    /// <response code="200">Returns the unified statistics.</response>
    [HttpGet]
    [ProducesResponseType(typeof(UnifiedStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnifiedStatistics(CancellationToken cancellationToken)
    {
        var result = await statisticsRepository.GetUnifiedStatisticsAsync(cancellationToken);
        return Ok(result);
    }
}
