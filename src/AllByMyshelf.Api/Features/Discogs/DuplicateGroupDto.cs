namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Represents a group of duplicate releases sharing the same artist and title.
/// </summary>
public record DuplicateGroupDto
{
    /// <summary>Artist name shared by all releases in this group.</summary>
    public required string Artist { get; init; }

    /// <summary>List of duplicate releases in this group.</summary>
    public required List<DuplicateReleaseDto> Releases { get; init; }

    /// <summary>Title shared by all releases in this group.</summary>
    public required string Title { get; init; }
}
