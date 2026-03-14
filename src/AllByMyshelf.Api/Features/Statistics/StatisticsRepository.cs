using AllByMyshelf.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AllByMyshelf.Api.Features.Statistics;

public class StatisticsRepository(AllByMyshelfDbContext dbContext) : IStatisticsRepository
{
    public async Task<CollectionValueDto> GetCollectionValueAsync(CancellationToken cancellationToken)
    {
        var releases = await dbContext.Releases
            .Select(r => r.LowestPrice)
            .ToListAsync(cancellationToken);

        var withPrice = releases.Where(p => p.HasValue).Select(p => p!.Value).ToList();
        var excluded = releases.Count - withPrice.Count;

        return new CollectionValueDto
        {
            ExcludedCount = excluded,
            IncludedCount = withPrice.Count,
            TotalValue = withPrice.Sum()
        };
    }
}
