# Discogs Feature — Frontend

Displays the vinyl record collection, wantlist, and collection management tools sourced from Discogs.

## Components

| Component | Description |
|-----------|-------------|
| `CollectionComponent` | Full collection list with advanced filtering, sorting, and grouping |
| `ReleaseDetailComponent` | Single record detail with editable notes and star rating (1–5) |
| `RandomPickerComponent` | Cross-feature random item picker (records/books/board games) with filters and history |
| `DuplicatesComponent` | Lists duplicate releases in collection |
| `MaintenanceComponent` | Shows releases with incomplete metadata |
| `WantlistComponent` | Paginated wantlist view |
| `FormatIconPipe` | Custom pipe for displaying format icons |

## Services

- **DiscogsService** — API calls to `/api/v1/releases/*` and `/api/v1/sync/*`
- **WantlistService** (core) — API calls to `/api/v1/wantlist`

## Routes

| Route | Component | Description |
|-------|-----------|-------------|
| `/releases` | CollectionComponent | Collection list |
| `/releases/:id` | ReleaseDetailComponent | Release detail with notes/rating |
| `/random-picker` | RandomPickerComponent | Cross-collection random picker |
| `/duplicates` | DuplicatesComponent | Duplicate releases report |
| `/maintenance` | MaintenanceComponent | Incomplete data cleanup |
| `/wantlist` | WantlistComponent | Wanted records list |

## UI Features

- Advanced filtering: Artist, Format, Genre, Year multi-select
- Multi-column sorting with persistence (default: artist → title); sort state saved to localStorage
- Grouping by Artist, Decade, Format, Genre, or Year (collapsible)
- Search with debouncing — searches artists, track artists (compilations), title, format, genre, year
- Configurable page size (10/20/50/100) with persistence
- Artists displayed as flat chips with Discogs disambiguation suffixes stripped
- Price tracking (lowest/highest/median) in detail view
- User notes and 1–5 star rating per record
- Recently added sidebar on collection
- Format icons for visual distinction
- Auto-reloads on sync completion
