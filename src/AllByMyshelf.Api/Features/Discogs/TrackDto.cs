using System.Diagnostics.CodeAnalysis;

namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Represents a single track in a release tracklist, returned as part of <see cref="ReleaseDetailDto"/>.
/// </summary>
/// <param name="Artists">Per-track artist names; empty for single-artist albums.</param>
/// <param name="Position">Track position (e.g. "A1", "1", "B2").</param>
/// <param name="Title">Track title.</param>
[ExcludeFromCodeCoverage]
public record TrackDto(
    IReadOnlyList<string> Artists,
    string Position,
    string Title);
