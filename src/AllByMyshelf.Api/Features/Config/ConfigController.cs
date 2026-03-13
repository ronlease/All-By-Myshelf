using AllByMyshelf.Api.Features.Discogs;
using AllByMyshelf.Api.Features.Hardcover;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Features.Config;

[ApiController]
[Route("api/v1/config")]
public class ConfigController(
    IOptionsSnapshot<DiscogsOptions> discogsOptions,
    IOptionsSnapshot<HardcoverOptions> hardcoverOptions) : ControllerBase
{
    private readonly DiscogsOptions _discogs = discogsOptions.Value;
    private readonly HardcoverOptions _hardcover = hardcoverOptions.Value;

    [HttpGet("features")]
    public IActionResult GetFeatures() =>
        Ok(new FeaturesDto(
            DiscogsEnabled: !string.IsNullOrWhiteSpace(_discogs.PersonalAccessToken),
            HardcoverEnabled: !string.IsNullOrWhiteSpace(_hardcover.ApiToken)));
}
