namespace AllByMyshelf.Api.Features.Config;

public record FeaturesDto(
    bool BoardGameGeekEnabled,
    bool DiscogsEnabled,
    bool HardcoverEnabled);
