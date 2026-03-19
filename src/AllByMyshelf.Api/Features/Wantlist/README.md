# Wantlist Feature

Exposes the Discogs wantlist — items the user wants to acquire. Data is populated automatically during the Discogs sync process.

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/wantlist` | Paginated wantlist (default pageSize=20, max=10000) |

## Key Components

- **WantlistController** — API endpoint
- **WantlistRepository** — Paged queries from local database

## Data Source

This feature does not have its own sync. `WantlistRelease` records are created and removed by the Discogs feature's `SyncService` as part of the full collection sync.

## Models

- `WantlistRelease` — EF Core entity
- `WantlistReleaseDto` — Response DTO (Artists, CoverImageUrl, DiscogsId, Format, Genre, Id, ThumbnailUrl, Title, Year)
