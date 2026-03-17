# Hardcover Feature

Manages the read books collection sourced from the Hardcover GraphQL API.

## External API

- **Hardcover GraphQL API** (`https://api.hardcover.app/v1/graphql`)
- Authentication via API token (`Hardcover:ApiToken`)
- Fetches books with read status (status_id=3)

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/books` | Paginated list with filters (author, title, genre, year) |
| GET | `/api/v1/books/{id}` | Full book detail |
| GET | `/api/v1/books/random` | Random book |
| GET | `/api/v1/books/sync/status` | Check if sync is running |
| POST | `/api/v1/books/sync` | Trigger manual sync |

## Key Components

- **HardcoverClient** — GraphQL client; resolves user ID then fetches read books with pagination (500 per page)
- **BooksService** — Business logic layer
- **BooksSyncService** — `BackgroundService` with bounded channel for sync requests
- **BooksRepository** — EF Core data access
- **BooksController** — API endpoints

## Sync Behavior

- Authenticates via API token, resolves current user ID via GraphQL
- Paginates through read books (status_id=3) at 500 items per page
- Extracts genre from `cached_tags` JSON field (first Genre tag)
- Parses `release_date` to extract year
- Upserts entire collection atomically

## Models

- `Book` — EF Core entity
- `BookDto`, `BookDetailDto` — Response DTOs
- `BookFilter` — Query filter model
- `HardcoverBook` — API response model
- `HardcoverOptions` — Configuration options (ApiToken)
