using AllByMyshelf.Api.Models.DTOs;

namespace AllByMyshelf.Api.Services;

public interface IStatisticsService
{
    Task<CollectionValueDto> GetCollectionValueAsync(CancellationToken cancellationToken);
}
