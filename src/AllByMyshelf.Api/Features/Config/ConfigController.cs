using AllByMyshelf.Api.Features.BoardGameGeek;
using AllByMyshelf.Api.Features.Discogs;
using AllByMyshelf.Api.Features.Hardcover;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AllByMyshelf.Api.Features.Config;

/// <summary>
/// API controller for configuration and feature flags.
/// </summary>
[ApiController]
[Route("api/v1/config")]
public class ConfigController(
    IOptionsSnapshot<BoardGameGeekOptions> boardGameGeekOptions,
    IOptionsSnapshot<DiscogsOptions> discogsOptions,
    IOptionsSnapshot<HardcoverOptions> hardcoverOptions) : ControllerBase
{
    private readonly BoardGameGeekOptions _boardGameGeek = boardGameGeekOptions.Value;
    private readonly DiscogsOptions _discogs = discogsOptions.Value;
    private readonly HardcoverOptions _hardcover = hardcoverOptions.Value;

    /// <summary>
    /// Returns feature availability flags for each integrated service.
    /// A service is enabled if its required credentials are configured.
    /// </summary>
    /// <returns>A <see cref="FeaturesDto"/> containing enabled flags for BoardGameGeek, Discogs, and Hardcover.</returns>
    /// <response code="200">Returns the feature availability flags.</response>
    [HttpGet("features")]
    [ProducesResponseType(typeof(FeaturesDto), StatusCodes.Status200OK)]
    public IActionResult GetFeatures() =>
        Ok(new FeaturesDto(
            BoardGameGeekEnabled: !string.IsNullOrWhiteSpace(_boardGameGeek.ApiToken) && !string.IsNullOrWhiteSpace(_boardGameGeek.Username),
            DiscogsEnabled: !string.IsNullOrWhiteSpace(_discogs.PersonalAccessToken),
            HardcoverEnabled: !string.IsNullOrWhiteSpace(_hardcover.ApiToken)));
}
