using AllByMyshelf.Api.Configuration;
using AllByMyshelf.Api.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Controllers;

[ApiController]
[Route("api/v1/config")]
public class ConfigController(
    IOptions<DiscogsOptions> discogsOptions,
    IOptions<HardcoverOptions> hardcoverOptions) : ControllerBase
{
    private readonly DiscogsOptions _discogs = discogsOptions.Value;
    private readonly HardcoverOptions _hardcover = hardcoverOptions.Value;

    [HttpGet("features")]
    public IActionResult GetFeatures() =>
        Ok(new FeaturesDto(
            DiscogsEnabled: !string.IsNullOrWhiteSpace(_discogs.PersonalAccessToken),
            HardcoverEnabled: !string.IsNullOrWhiteSpace(_hardcover.ApiToken)));
}
