# All By Myshelf — Claude Code Orchestration

## Project Overview
All By Myshelf is a personal collection dashboard that aggregates data from external APIs
(Discogs, Hardcover, and others) into a single read-only view. It is a single-user application.

## Tech Stack
- **API:** ASP.NET Core 10 Web API, Entity Framework Core 10, PostgreSQL
- **Frontend:** Angular 21, standalone components, Angular Material
- **Auth:** Auth0 (single user)
- **Testing:** xUnit, Gherkin (SpecFlow or plain xUnit with Gherkin-style naming)
- **Documentation:** Swashbuckle (OpenAPI/Swagger), PlantUML (C4 models)
- **Infrastructure:** Docker, Docker Compose, AWS (future)
- **Secrets:** dotnet user-secrets (local), AWS Secrets Manager (cloud, future)

## Repository Structure
```
AllByMyshelf/
  src/
    AllByMyshelf.Api/         # ASP.NET Core 10 Web API
    AllByMyshelf.Web/         # Angular 21 frontend
  tests/
    AllByMyshelf.Unit/        # xUnit unit tests
    AllByMyshelf.Integration/ # xUnit integration tests (EF Core in-memory)
  docs/
    backlog.md                # Owned by Product Owner agent
    c4/                       # PlantUML C4 model files
  docker-compose.yml
  .claude/
    agents/
```

## Agent Roster
| Agent | File | Responsibility |
|---|---|---|
| Product Owner | `product-owner.md` | Backlog, business problems, acceptance criteria |
| Architect | `architect.md` | Swashbuckle OpenAPI, PlantUML C4 models |
| Backend Engineer | `backend-engineer.md` | .NET 10 API implementation |
| Frontend Engineer | `frontend-engineer.md` | Angular 21 implementation |
| QA Engineer | `qa-engineer.md` | Gherkin scenarios, xUnit tests |

## Workflow
- Workflow is fluid. Any agent may be invoked at any time.
- **The user must approve all file changes before they are written.**
- The Backend Engineer and QA Engineer work alongside each other:
  the Engineer implements a feature, QA immediately writes tests for it before moving on.
- The Architect generates and updates OpenAPI specs and C4 models after API changes.
- The Product Owner owns `docs/backlog.md` exclusively.

## Routing Rules
- "backlog", "story", "feature request", "business problem" → Product Owner
- "C4", "diagram", "architecture", "swagger", "openapi" → Architect
- "implement", "endpoint", "controller", "service", "repository", "EF", "migration" → Backend Engineer
- "component", "angular", "frontend", "UI", "page", "route" → Frontend Engineer
- "test", "gherkin", "scenario", "given/when/then", "coverage" → QA Engineer

## Conventions
- C# follows Microsoft conventions. Use `var` where type is obvious.
- All API endpoints are versioned under `/api/v1/`.
- All secrets go through `dotnet user-secrets` locally. Never hardcode credentials.
- EF Core migrations are explicit — never auto-migrate on startup.
- Angular uses standalone components. No NgModules.
- All new features require a backlog entry before implementation.
- **All fields, properties, methods, and variables within a class must be declared in alphabetical order.** This applies to both C# and TypeScript. Enforced to ease diffs and code review.
