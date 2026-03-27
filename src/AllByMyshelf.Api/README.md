# AllByMyshelf.Api

ASP.NET Core 10 Web API for All By Myshelf. See the [root README](../../README.md) for full setup instructions.

## Tech Stack

- .NET 10, ASP.NET Core 10 Web API
- Entity Framework Core 10 with Npgsql provider (PostgreSQL 17)
- Auth0 JWT Bearer authentication
- Swashbuckle 10.x for OpenAPI/Swagger
- `dotnet user-secrets` for local secrets
- `IHttpClientFactory` for external API clients

## Project Structure

This project follows vertical slice architecture — organized by feature, not by layer.

```
AllByMyshelf.Api/
  Common/                   # Shared types (PagedResult, SyncStartResult)
  Features/
    BoardGameGeek/          # Board games, sync, BoardGameGeek XML API client
    Config/                 # Feature flags (GET /api/v1/config/features)
    Discogs/                # Releases, wantlist, sync, Discogs API client
    Hardcover/              # Books, sync, Hardcover GraphQL client
    Settings/               # User settings (theme, API tokens)
    Statistics/             # Unified statistics (music, books, board games)
    Wantlist/               # Discogs wantlist sync and browsing
  Infrastructure/
    Configuration/          # Strongly-typed config classes, DB-backed configuration provider
    Data/                   # DbContext, EF Core migrations, entity configurations
  Models/
    Entities/               # EF Core entity classes
  Program.cs                # Startup, DI, middleware pipeline
```

## Features

Each feature folder contains a vertical slice with its own:

- Controller (API endpoints)
- Service (business logic)
- Repository (EF Core data access)
- Models (DTOs, filters, request/response objects)
- External API client (if applicable)
- README.md (detailed documentation)

| Feature | Description | README |
|---------|-------------|--------|
| **BoardGameGeek** | Board games collection, sync with BoardGameGeek XML API | [Features/BoardGameGeek/README.md](Features/BoardGameGeek/README.md) |
| **Config** | Feature flags and configuration | [Features/Config/README.md](Features/Config/README.md) |
| **Discogs** | Vinyl/CD collection, sync with Discogs REST API, marketplace pricing | [Features/Discogs/README.md](Features/Discogs/README.md) |
| **Hardcover** | Books collection, sync with Hardcover GraphQL API | [Features/Hardcover/README.md](Features/Hardcover/README.md) |
| **Settings** | User settings (theme, API tokens) stored in database | [Features/Settings/README.md](Features/Settings/README.md) |
| **Statistics** | Unified analytics across all collections | [Features/Statistics/README.md](Features/Statistics/README.md) |
| **Wantlist** | Discogs wantlist sync and browsing | [Features/Wantlist/README.md](Features/Wantlist/README.md) |

## Infrastructure

- **Configuration/** — Strongly-typed options classes (e.g., `BoardGameGeekOptions`, `DiscogsOptions`, `HardcoverOptions`), database-backed configuration provider
- **Data/** — `AllByMyshelfDbContext`, EF Core migrations, entity type configurations (`IEntityTypeConfiguration<T>`)

## Running Locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL 17 (via Docker Compose or local install)

### 1. Start the database

```bash
docker compose up -d
```

### 2. Configure secrets

All credentials are managed via `dotnet user-secrets`:

```bash
cd src/AllByMyshelf.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=allbymyshelf;Username=allbymyshelf;Password=localdev"
dotnet user-secrets set "Auth0:Domain" "<your-auth0-domain>"
dotnet user-secrets set "Auth0:Audience" "<your-auth0-audience>"
dotnet user-secrets set "BoardGameGeek:ApiToken" "<your-boardgamegeek-api-token>"
dotnet user-secrets set "BoardGameGeek:Username" "<your-boardgamegeek-username>"
dotnet user-secrets set "Discogs:PersonalAccessToken" "<your-discogs-personal-access-token>"
dotnet user-secrets set "Discogs:Username" "<your-discogs-username>"
dotnet user-secrets set "Hardcover:ApiToken" "<your-hardcover-api-token>"
```

### 3. Run database migrations

```bash
dotnet ef database update
```

### 4. Run the API

```bash
dotnet run --launch-profile https
```

API runs at `https://localhost:7208`.

## API Versioning

All endpoints are versioned under `/api/v1/`:

- `/api/v1/releases`
- `/api/v1/books`
- `/api/v1/boardgames`
- `/api/v1/statistics`
- `/api/v1/config/features`
- `/api/v1/settings`
- `/api/v1/wantlist`

## Swagger UI

In development, Swagger UI is available at:

```
https://localhost:7208/swagger
```

All endpoints require JWT bearer authentication except `/health`.

## Database Migrations

Migrations are explicit — never auto-migrate on startup.

```bash
# Create a new migration
dotnet ef migrations add <MigrationName>

# Apply migrations
dotnet ef database update

# Revert last migration
dotnet ef database update <PreviousMigrationName>

# Remove last unapplied migration
dotnet ef migrations remove
```

## Authentication

All controllers are protected with `[Authorize]` via Auth0 JWT bearer validation. Unauthenticated requests return `401 Unauthorized`.

To authenticate in Swagger:
1. Click "Authorize" button
2. Enter `Bearer <your-jwt-token>`
3. Click "Authorize"

## Health Check

Health check endpoint (unauthenticated):

```
GET /health
```

Returns `200 OK` if the API is running.

## External API Clients

All external API clients are registered as typed `HttpClient` instances via `IHttpClientFactory`:

- **DiscogsClient** — Discogs REST API with rate-limit handling (429 responses)
- **HardcoverClient** — Hardcover GraphQL API
- **BoardGameGeekClient** — BoardGameGeek XML API v2

Clients live in their respective feature folders and handle retries, rate limiting, and error responses gracefully.

## Testing

See the [root README](../../README.md) for test commands.

Unit tests are in `tests/AllByMyshelf.Unit`. Integration tests are in `tests/AllByMyshelf.Integration`.
