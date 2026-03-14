namespace AllByMyshelf.Api.Features.Statistics;

public interface IStatisticsRepository
{
    Task<CollectionValueDto> GetCollectionValueAsync(CancellationToken cancellationToken);
    Task<UnifiedStatisticsDto> GetUnifiedStatisticsAsync(CancellationToken cancellationToken);
}
