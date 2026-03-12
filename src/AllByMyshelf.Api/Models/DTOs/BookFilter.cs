namespace AllByMyshelf.Api.Models.DTOs;

/// <summary>
/// Filter criteria for querying books.
/// All filters use case-insensitive contains matching.
/// </summary>
/// <param name="Author">Optional author filter.</param>
/// <param name="Genre">Optional genre filter.</param>
/// <param name="Title">Optional title filter.</param>
/// <param name="Year">Optional year filter.</param>
public record BookFilter(
    string? Author = null,
    string? Genre = null,
    string? Title = null,
    string? Year = null);
