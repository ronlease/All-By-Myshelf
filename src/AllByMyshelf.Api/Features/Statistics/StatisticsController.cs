using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllByMyshelf.Api.Features.Statistics;

[ApiController]
[Authorize]
[Route("api/v1/statistics")]
public class StatisticsController(IStatisticsRepository statisticsRepository) : ControllerBase
{
    [HttpGet("collection-value")]
    public async Task<IActionResult> GetCollectionValue(CancellationToken cancellationToken)
    {
        var result = await statisticsRepository.GetCollectionValueAsync(cancellationToken);
        return Ok(result);
    }
}
