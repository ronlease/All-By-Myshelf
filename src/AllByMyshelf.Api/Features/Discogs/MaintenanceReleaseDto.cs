namespace AllByMyshelf.Api.Features.Discogs;

public record MaintenanceReleaseDto
{
    public required string Artist { get; init; }
    public required int DiscogsId { get; init; }
    public required Guid Id { get; init; }
    public required List<string> MissingFields { get; init; }
    public string? ThumbnailUrl { get; init; }
    public required string Title { get; init; }
}
