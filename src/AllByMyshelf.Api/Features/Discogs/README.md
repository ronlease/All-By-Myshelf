# Discogs Feature

Manages the vinyl record collection and wantlist sourced from the Discogs REST API.

## External API

- **Discogs API** (`https://api.discogs.com/`)
- Authentication via personal access token (`Discogs:PersonalAccessToken`)
- Handles 429 rate-limit responses with exponential backoff

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/releases` | Paginated list with filters (artist, title, format, genre, year, search) |
| GET | `/api/v1/releases/{id}` | Full release detail with pricing |
| GET | `/api/v1/releases/random` | Random release with optional decade/format/genre filters |
| GET | `/api/v1/releases/recent` | Recently added releases (10–30 items) |
| GET | `/api/v1/releases/duplicates` | Releases with same artist+title but different Discogs IDs |
| GET | `/api/v1/releases/maintenance` | Releases with incomplete data (missing cover, genre, year) |
| PUT | `/api/v1/releases/{id}/notes-rating` | Update user notes and rating (1–5 stars) |
| POST | `/api/v1/sync` | Trigger Discogs sync |
| GET | `/api/v1/sync/status` | Current sync progress |

## Key Components

- **DiscogsClient** — HTTP client with rate-limit handling and pause/resume events; URL-encodes usernames in API paths
- **ReleasesService** — Business logic layer
- **SyncService** — `BackgroundService` that syncs both collection and wantlist with progress tracking
- **ReleasesRepository** — EF Core data access for releases
- **ReleasesController** — Release endpoints
- **SyncController** — Sync trigger and status endpoints

## Sync Behavior

- Background worker with detailed progress tracking (current/total/status/retryAfterSeconds)
- Fetches full collection (100 per page), then for each release fetches detail and marketplace stats
- Handles 429 rate-limit with exponential backoff
- Syncs wantlist after collection; removes items no longer on user's wantlist

## Models

- `Release`, `WantlistRelease` — EF Core entities
- `ReleaseDto`, `ReleaseDetailDto`, `DuplicateGroupDto`, `MaintenanceReleaseDto` — Response DTOs
- `ReleaseFilter`, `RandomReleaseFilter` — Query filter models
- `SyncProgressDto` — Sync status (Current, Total, Status, RetryAfterSeconds)
- `DiscogsOptions` — Configuration options (Username, PersonalAccessToken)
