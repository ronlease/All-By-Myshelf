using System.Diagnostics.CodeAnalysis;

namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Options for configuring which data categories to include in a Discogs sync.
/// Posted to POST /api/v1/sync to start a sync with specific options.
/// </summary>
/// <param name="IncludeDetails">Whether to fetch detail data (genre, tracklist) for new releases.</param>
/// <param name="IncludePricing">Whether to fetch marketplace pricing for new releases.</param>
/// <param name="IncludeWantlist">Whether to sync the wantlist after the collection.</param>
/// <param name="Mode">Sync mode: "incremental" (new only), "full" (all), or "stale" (older than StaleDays).</param>
/// <param name="StaleDays">When Mode is "stale", the number of days after which a release is considered stale. Default 30.</param>
[ExcludeFromCodeCoverage]
public record SyncOptionsDto(
    bool IncludeDetails = true,
    bool IncludePricing = true,
    bool IncludeWantlist = true,
    string Mode = SyncConstants.Modes.Incremental,
    int StaleDays = 30
);
