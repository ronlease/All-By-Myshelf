namespace AllByMyshelf.Api.Models.DTOs;

/// <summary>
/// Generic paginated result wrapper returned by list endpoints.
/// </summary>
/// <typeparam name="T">The item type contained in this page.</typeparam>
public class PagedResult<T>
{
    /// <summary>Items on the current page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>The current page number (1-based).</summary>
    public int Page { get; init; }

    /// <summary>Maximum number of items per page.</summary>
    public int PageSize { get; init; }

    /// <summary>Total number of items across all pages.</summary>
    public int TotalCount { get; init; }
}
