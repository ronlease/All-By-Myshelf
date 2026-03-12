using AllByMyshelf.Api.Models.DTOs;
using AllByMyshelf.Api.Repositories;

namespace AllByMyshelf.Api.Services;

public class StatisticsService(IStatisticsRepository statisticsRepository) : IStatisticsService
{
    public Task<CollectionValueDto> GetCollectionValueAsync(CancellationToken cancellationToken)
        => statisticsRepository.GetCollectionValueAsync(cancellationToken);
}
