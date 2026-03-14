namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Represents a single release within a duplicate group.
/// </summary>
public record DuplicateReleaseDto
{
    /// <summary>Discogs release ID.</summary>
    public required int DiscogsId { get; init; }

    /// <summary>Primary format description.</summary>
    public required string Format { get; init; }

    /// <summary>Application-generated identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Release year; null when unknown.</summary>
    public int? Year { get; init; }
}
