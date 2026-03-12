using AllByMyshelf.Api.Models.DTOs;

namespace AllByMyshelf.Api.Repositories;

public interface IStatisticsRepository
{
    Task<CollectionValueDto> GetCollectionValueAsync(CancellationToken cancellationToken);
}
