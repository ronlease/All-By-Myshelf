# All By Myshelf

A personal collection dashboard that aggregates data from external APIs (Discogs, Hardcover, and others) into a single read-only view.

## Features

- **Records** — syncs your Discogs vinyl/CD collection; paginated table with search, column filters, grouping, and album art thumbnails
- **Record detail** — cover art, format, genre, year, Discogs marketplace pricing (low/median/high), personal notes, and star rating
- **Wantlist** — browse and sync your Discogs wantlist
- **Duplicates** — detect duplicate releases in your collection
- **Random picker** — suggests a random record or book, context-aware (defaults to last-viewed collection), filterable by decade, format, and genre
- **Store finder** — locates independent record stores or bookstores near a US zip code or city using OpenStreetMap, context-aware (records vs books)
- **Books** — syncs your read books from Hardcover; paginated table with cover thumbnails, author, title, year
- **Collection value** — estimates total collection value from Discogs marketplace lowest-price data
- **Maintenance** — maintenance view for collection upkeep
- **Settings** — configuration page with theme switching
- **Sync progress** — live progress indicator with rate-limit countdown during Discogs syncs
- **Side drawer navigation** — all pages accessible from a slide-out drawer

## Tech Stack

- **API:** ASP.NET Core 10, Entity Framework Core 10, PostgreSQL 17
- **Frontend:** Angular 21, standalone components, Angular Material
- **Auth:** Auth0 (single user)
- **Testing:** xUnit, FluentAssertions, Moq
- **Docs:** Swashbuckle (OpenAPI/Swagger), PlantUML (C4 models)
- **Infrastructure:** Docker, Docker Compose, AWS (planned)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22](https://nodejs.org/)
- [Docker](https://www.docker.com/) with Docker Compose plugin
- [Claude Code](https://claude.ai/code) (optional, for AI-assisted development)

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/ronlease/All-By-Myshelf.git
cd All-By-Myshelf
```

### 2. Start the database

```bash
docker compose up -d
```

### 3. Configure API secrets

```bash
cd src/AllByMyshelf.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=allbymyshelf;Username=allbymyshelf;Password=localdev"
dotnet user-secrets set "Auth0:Domain" "<your-auth0-domain>"
dotnet user-secrets set "Auth0:Audience" "<your-auth0-audience>"
dotnet user-secrets set "Discogs:PersonalAccessToken" "<your-discogs-personal-access-token>"
dotnet user-secrets set "Discogs:Username" "<your-discogs-username>"
dotnet user-secrets set "Hardcover:ApiToken" "<your-hardcover-api-token>"
```

### 4. Run database migrations

```bash
dotnet ef database update --project src/AllByMyshelf.Api
```

### 5. Run the API

```bash
dotnet run --launch-profile https --project src/AllByMyshelf.Api
```

API runs at `https://localhost:7208`. Swagger UI available at `https://localhost:7208/swagger`.

### 6. Configure the frontend

```bash
cd src/AllByMyshelf.Web
cp src/environments/environment.template.ts src/environments/environment.ts
```

Edit `src/environments/environment.ts` and fill in your Auth0 credentials.

### 7. Run the frontend

```bash
npm install
npx ng serve --ssl
```

Frontend runs at `https://localhost:4200`.

> **Auth0 setup:** Add `https://localhost:4200` to Allowed Web Origins and Allowed Logout URLs, and `https://localhost:4200/callback` to Allowed Callback URLs in your Auth0 application settings.

## Running Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/AllByMyshelf.Unit

# Integration tests only
dotnet test tests/AllByMyshelf.Integration
```

## Project Structure

```
All-By-Myshelf/
  src/
    AllByMyshelf.Api/
      Common/                 # Shared types (PagedResult, SyncStartResult)
      Features/
        Config/               # GET /api/v1/config/features
        Discogs/              # Releases, sync, duplicates, Discogs API client
        Hardcover/            # Books, sync, Hardcover API client
        Settings/             # User settings (theme)
        Statistics/           # Unified statistics (records + books breakdowns)
        Wantlist/             # Discogs wantlist sync and browsing
      Infrastructure/
        Data/                 # EF Core DbContext, migrations
      Models/
        Entities/             # EF Core entity classes
      Program.cs
    AllByMyshelf.Web/         # Angular 21 frontend
  tests/
    AllByMyshelf.Unit/        # xUnit unit tests
    AllByMyshelf.Integration/ # xUnit integration tests
  docs/
    backlog.md                # Product backlog
    c4/                       # PlantUML C4 architecture diagrams
  .claude/
    CLAUDE.md                 # Claude Code orchestration config
    agents/                   # Claude Code agent definitions
  docker-compose.yml
```

## AI-Assisted Development

This project uses [Claude Code](https://claude.ai/code) with a multi-agent setup. Agents are defined in `.claude/agents/` and orchestrated via `.claude/CLAUDE.md`.

| Agent | Responsibility |
|---|---|
| Product Owner | Backlog and acceptance criteria |
| Architect | OpenAPI spec and C4 diagrams |
| Backend Engineer | API implementation |
| Frontend Engineer | Angular implementation |
| QA Engineer | Gherkin scenarios and xUnit tests |

## External APIs

| API | Status | Docs |
|---|---|---|
| Discogs | Live | [discogs.com/developers](https://www.discogs.com/developers/) |
| Hardcover | Live | [docs.hardcover.app](https://docs.hardcover.app/api/getting-started/) |
| Nominatim (OpenStreetMap) | Live | [nominatim.org/release-docs/latest/api/Search/](https://nominatim.org/release-docs/latest/api/Search/) |
| Overpass API (OpenStreetMap) | Live | [overpass-api.de](https://overpass-api.de/) |

## Secrets Reference

Never commit secrets. All secrets are managed via `dotnet user-secrets` locally.

| Key | Description |
|---|---|
| `ConnectionStrings:Default` | PostgreSQL connection string |
| `Auth0:Domain` | Auth0 tenant domain (e.g. `dev-xxxx.us.auth0.com`) |
| `Auth0:Audience` | Auth0 API identifier (e.g. `https://localhost/api`) |
| `Discogs:PersonalAccessToken` | Discogs personal access token |
| `Discogs:Username` | Discogs username |
| `Hardcover:ApiToken` | Hardcover API token (from hardcover.app/account/api) |

## License

MIT License. See [LICENSE](LICENSE) for details.
