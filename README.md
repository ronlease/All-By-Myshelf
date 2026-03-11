# All By Myshelf

A personal collection dashboard that aggregates data from external APIs (Discogs, Hardcover, and others) into a single read-only view.

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
    AllByMyshelf.Api/         # ASP.NET Core 10 Web API
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
| Hardcover | Planned | [hardcover.app](https://hardcover.app/) |

## Secrets Reference

Never commit secrets. All secrets are managed via `dotnet user-secrets` locally.

| Key | Description |
|---|---|
| `ConnectionStrings:Default` | PostgreSQL connection string |
| `Auth0:Domain` | Auth0 tenant domain (e.g. `dev-xxxx.us.auth0.com`) |
| `Auth0:Audience` | Auth0 API identifier (e.g. `https://localhost/api`) |
| `Discogs:PersonalAccessToken` | Discogs personal access token |
| `Discogs:Username` | Discogs username |

## License

MIT License. See [LICENSE](LICENSE) for details.
