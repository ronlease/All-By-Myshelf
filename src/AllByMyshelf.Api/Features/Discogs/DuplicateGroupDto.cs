namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Represents a group of duplicate releases sharing the same artists and title.
/// </summary>
public record DuplicateGroupDto
{
    /// <summary>List of artist names shared by all releases in this group.</summary>
    public required IReadOnlyList<string> Artists { get; init; }

    /// <summary>List of duplicate releases in this group.</summary>
    public required IReadOnlyList<DuplicateReleaseDto> Releases { get; init; }

    /// <summary>Title shared by all releases in this group.</summary>
    public required string Title { get; init; }
}
