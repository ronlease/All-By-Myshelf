using AllByMyshelf.Api.Features.Bgg;
using AllByMyshelf.Api.Features.Discogs;
using AllByMyshelf.Api.Features.Hardcover;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Features.Config;

[ApiController]
[Route("api/v1/config")]
public class ConfigController(
    IOptionsSnapshot<BggOptions> bggOptions,
    IOptionsSnapshot<DiscogsOptions> discogsOptions,
    IOptionsSnapshot<HardcoverOptions> hardcoverOptions) : ControllerBase
{
    private readonly BggOptions _bgg = bggOptions.Value;
    private readonly DiscogsOptions _discogs = discogsOptions.Value;
    private readonly HardcoverOptions _hardcover = hardcoverOptions.Value;

    [HttpGet("features")]
    public IActionResult GetFeatures() =>
        Ok(new FeaturesDto(
            BggEnabled: !string.IsNullOrWhiteSpace(_bgg.ApiToken) && !string.IsNullOrWhiteSpace(_bgg.Username),
            DiscogsEnabled: !string.IsNullOrWhiteSpace(_discogs.PersonalAccessToken),
            HardcoverEnabled: !string.IsNullOrWhiteSpace(_hardcover.ApiToken)));
}
