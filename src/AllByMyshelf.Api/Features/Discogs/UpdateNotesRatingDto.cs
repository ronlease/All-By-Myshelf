namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Request model for updating a release's notes and rating.
/// </summary>
public record UpdateNotesRatingDto
{
    /// <summary>User-provided listening notes; null or empty to clear notes.</summary>
    public string? Notes { get; init; }

    /// <summary>User-provided rating on a scale of 1-5; null to clear rating.</summary>
    public int? Rating { get; init; }
}
