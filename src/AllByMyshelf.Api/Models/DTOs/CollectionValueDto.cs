namespace AllByMyshelf.Api.Models.DTOs;

/// <summary>
/// Aggregate collection value data returned by GET /api/v1/statistics/collection-value.
/// </summary>
public record CollectionValueDto(
    int ExcludedCount,
    int IncludedCount,
    decimal TotalValue
);
