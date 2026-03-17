# BGG (BoardGameGeek) Feature — Frontend

Displays the board game collection sourced from BoardGameGeek.

## Components

| Component | Description |
|-----------|-------------|
| `BoardGamesComponent` | Main list view with pagination, search, sorting, and grouping |
| `BoardGameDetailComponent` | Single game detail page |

## Services

- **BggService** — API calls to `/api/v1/boardgames/*` (list, detail, random, sync status, trigger sync)

## Routes

| Route | Component | Description |
|-------|-----------|-------------|
| `/board-games` | BoardGamesComponent | Collection list |
| `/board-games/:id` | BoardGameDetailComponent | Game detail |

## UI Features

- Full-text search across title, designers, genre, year
- Grouping by Designer, Decade, Genre, or Year (collapsible groups)
- Pagination (25 items per page)
- Thumbnail, title, designer, genre, player count, year display
- Auto-reloads on sync completion via `SyncStateService`
