# Statistics Feature

Provides unified analytics and value estimates across all collections (records, books, board games).

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/statistics` | Unified statistics across all collections |
| GET | `/api/v1/statistics/collection-value` | Vinyl collection value estimates |

## Key Components

- **StatisticsController** — API endpoints
- **StatisticsRepository** — Direct database queries with LINQ aggregations

## Models

- `UnifiedStatisticsDto` — Contains `RecordStatisticsDto`, `BookStatisticsDto`, and `BoardGameStatisticsDto`
- `RecordStatisticsDto` — TotalCount, TotalValue, DecadeBreakdown, FormatBreakdown, GenreBreakdown, ExcludedFromValueCount
- `BookStatisticsDto` — TotalCount, AuthorBreakdown, GenreBreakdown, YearBreakdown
- `BoardGameStatisticsDto` — TotalCount, DesignerBreakdown, GenreBreakdown, YearBreakdown
- `CollectionValueDto` — TotalValue, IncludedCount, ExcludedCount (based on lowest marketplace price)
- `BreakdownItemDto` — Label, Count
