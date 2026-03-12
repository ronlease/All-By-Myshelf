using AllByMyshelf.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllByMyshelf.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/statistics")]
public class StatisticsController(IStatisticsService statisticsService) : ControllerBase
{
    [HttpGet("collection-value")]
    public async Task<IActionResult> GetCollectionValue(CancellationToken cancellationToken)
    {
        var result = await statisticsService.GetCollectionValueAsync(cancellationToken);
        return Ok(result);
    }
}
