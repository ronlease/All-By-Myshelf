namespace AllByMyshelf.Api.Models.DTOs;

/// <summary>
/// Represents a single release in an API response.
/// </summary>
public class ReleaseDto
{
    /// <summary>Artist name.</summary>
    public string Artist { get; init; } = string.Empty;

    /// <summary>Primary format description.</summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>Database identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Release title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Release year; null when unknown.</summary>
    public int? Year { get; init; }
}
