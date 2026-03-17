# Hardcover Feature — Frontend

Displays the read books collection sourced from Hardcover.

## Components

| Component | Description |
|-----------|-------------|
| `BooksComponent` | Main book collection list with search, grouping, and pagination |
| `BookDetailComponent` | Single book detail view |

## Services

- **HardcoverService** — API calls to `/api/v1/books/*` (list, detail, random, sync status, trigger sync)

## Routes

| Route | Component | Description |
|-------|-----------|-------------|
| `/books` | BooksComponent | Collection list |
| `/books/:id` | BookDetailComponent | Book detail |

## UI Features

- Full-text search across title, authors, genre, year
- Grouping by Author, Decade, Genre, or Year (collapsible groups)
- Pagination (25 items per page)
- Thumbnail, author(s), title, genre, year display
- Multiple authors support
- Auto-reloads on sync completion via `SyncStateService`
