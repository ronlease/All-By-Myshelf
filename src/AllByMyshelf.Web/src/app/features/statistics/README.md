# Statistics Feature — Frontend

Unified dashboard showing aggregate statistics across all collections.

## Components

| Component | Description |
|-----------|-------------|
| `StatisticsComponent` | Dashboard with breakdowns for records, books, and board games |

## Services

- **StatisticsService** — API calls to `/api/v1/statistics` and `/api/v1/statistics/collection-value`

## Routes

| Route | Component | Description |
|-------|-----------|-------------|
| `/statistics` | StatisticsComponent | Unified statistics dashboard (also the default landing page) |

## UI Features

- Board games section: total count with genre breakdown
- Books section: total count with author, decade, and genre breakdowns
- Records section: total count with estimated collection value, decade/format/genre breakdowns
- Shows excluded items without pricing separately
- Collapsible category sections
- Clickable breakdown items link to collection views with pre-applied grouping/filtering
- Material icons for visual category identification
- Empty state handling for disabled collections
