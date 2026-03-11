namespace AllByMyshelf.Api.Models.DTOs;

/// <summary>
/// Full detail representation of a single release, returned by the GET /api/v1/releases/{id} endpoint.
/// </summary>
public class ReleaseDetailDto
{
    /// <summary>Artist name.</summary>
    public string Artist { get; init; } = string.Empty;

    /// <summary>Country of release; null when not populated by sync.</summary>
    public string? Country { get; init; }

    /// <summary>Discogs release ID.</summary>
    public int DiscogsId { get; init; }

    /// <summary>Primary format description.</summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>Primary genre; null when not populated by sync.</summary>
    public string? Genre { get; init; }

    /// <summary>Application-generated identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Record label; null when not populated by sync.</summary>
    public string? Label { get; init; }

    /// <summary>Personal notes from Discogs; null when not populated by sync.</summary>
    public string? Notes { get; init; }

    /// <summary>Comma-separated list of styles; null when not populated by sync.</summary>
    public string? Styles { get; init; }

    /// <summary>Release title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Release year; null when unknown.</summary>
    public int? Year { get; init; }
}
