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

    public async Task<UnifiedStatisticsDto> GetUnifiedStatisticsAsync(CancellationToken cancellationToken)
    {
        // --- Records ---
        var releases = await dbContext.Releases
            .AsNoTracking()
            .Select(r => new { r.Format, r.Genre, r.LowestPrice, r.Year })
            .ToListAsync(cancellationToken);

        var releasesWithPrice = releases.Where(r => r.LowestPrice.HasValue).ToList();

        var decadeBreakdown = releases
            .Where(r => r.Year.HasValue)
            .GroupBy(r => (r.Year!.Value / 10) * 10)
            .Select(g => new BreakdownItemDto { Count = g.Count(), Label = $"{g.Key}s" })
            .OrderBy(b => b.Label)
            .ToList();

        var formatBreakdown = releases
            .Where(r => !string.IsNullOrWhiteSpace(r.Format))
            .GroupBy(r => r.Format!)
            .Select(g => new BreakdownItemDto { Count = g.Count(), Label = g.Key })
            .OrderByDescending(b => b.Count)
            .ThenBy(b => b.Label)
            .ToList();

        var genreBreakdown = releases
            .Where(r => !string.IsNullOrWhiteSpace(r.Genre))
            .GroupBy(r => r.Genre!)
            .Select(g => new BreakdownItemDto { Count = g.Count(), Label = g.Key })
            .OrderByDescending(b => b.Count)
            .ThenBy(b => b.Label)
            .ToList();

        var recordStats = new RecordStatisticsDto
        {
            DecadeBreakdown = decadeBreakdown,
            ExcludedFromValueCount = releases.Count - releasesWithPrice.Count,
            FormatBreakdown = formatBreakdown,
            GenreBreakdown = genreBreakdown,
            TotalCount = releases.Count,
            TotalValue = releasesWithPrice.Sum(r => r.LowestPrice!.Value)
        };

        // --- Books ---
        var books = await dbContext.Books
            .AsNoTracking()
            .Select(b => new { b.Genre })
            .ToListAsync(cancellationToken);

        var bookGenreBreakdown = books
            .Where(b => !string.IsNullOrWhiteSpace(b.Genre))
            .GroupBy(b => b.Genre!)
            .Select(g => new BreakdownItemDto { Count = g.Count(), Label = g.Key })
            .OrderByDescending(b => b.Count)
            .ThenBy(b => b.Label)
            .ToList();

        var bookStats = new BookStatisticsDto
        {
            GenreBreakdown = bookGenreBreakdown,
            TotalCount = books.Count
        };

        return new UnifiedStatisticsDto
        {
            Books = bookStats,
            Records = recordStats
        };
    }
}
