# BoardGameGeek Feature

Manages the board game collection sourced from the BoardGameGeek XML API v2.

## External API

- **BoardGameGeek XML API v2** (`https://www.boardgamegeek.com/xmlapi2/`)
- Authentication via Bearer token (`BoardGameGeek:ApiToken`) and username (`BoardGameGeek:Username`) — both required to enable BoardGameGeek
- Handles BoardGameGeek's 202 "queued" responses with exponential backoff (max 5 retries)

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/boardgames` | Paginated list with filters (title, designer, genre, playerCount, year) |
| GET | `/api/v1/boardgames/{id}` | Full board game detail |
| GET | `/api/v1/boardgames/random` | Random board game |
| GET | `/api/v1/boardgames/sync/status` | Check if sync is running |
| POST | `/api/v1/boardgames/sync` | Trigger manual sync |

## Key Components

- **BoardGameGeekClient** — HTTP client for BoardGameGeek XML API; handles 202 retry logic
- **BoardGamesService** — Business logic layer
- **BoardGamesSyncService** — `BackgroundService` that listens on a bounded channel for sync requests
- **BoardGamesRepository** — EF Core data access
- **BoardGamesController** — API endpoints

## Sync Behavior

- Runs as a `BackgroundService` with a bounded channel to queue sync requests
- Fetches collection list, then batches IDs in groups of 20 for enrichment
- 500ms delay between batches to respect BoardGameGeek rate limits
- Upserts entire collection atomically
- Prevents concurrent syncs via atomic flag

## Models

- `BoardGame` — EF Core entity (inherits from `CollectionEntityBase`)
- `BoardGameDto`, `BoardGameDetailDto` — Response DTOs
- `BoardGameFilter` — Query filter model
- `BoardGameGeekOptions` — Configuration options (Username, ApiToken)
