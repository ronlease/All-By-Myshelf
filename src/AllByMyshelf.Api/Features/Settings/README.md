# Settings Feature

Manages application settings and API credentials, with database storage and configuration fallback.

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/settings` | Retrieve all settings with tokens masked |
| PUT | `/api/v1/settings` | Update settings (only non-null fields are updated) |

## Key Components

- **SettingsController** — Reads/writes settings to database, falls back to `IConfiguration` for secrets

## Behavior

- Settings can be stored in the database or via `dotnet user-secrets` (database takes precedence)
- Tokens are masked in GET responses (first 4 chars + "••••" + last 2 chars)
- After update, configuration is reloaded to propagate changes across the application
- All setting values are sanitized via `InputSanitizer` before storage (trim, strip control chars, enforce max length)
- Theme values are validated against an allowlist (`light`, `dark`, `os-default`); invalid values return 400
- Token fields: max length 2000; username fields: max length 100

## Settings Keys

| Key | Description |
|-----|-------------|
| `BoardGameGeek:ApiToken` | BoardGameGeek API token |
| `BoardGameGeek:Username` | BoardGameGeek username |
| `Discogs:PersonalAccessToken` | Discogs personal access token |
| `Discogs:Username` | Discogs username |
| `Hardcover:ApiToken` | Hardcover API token |
| `App:Theme` | UI theme (light, dark, os-default) |

## Models

- `AppSetting` — EF Core entity (Key, Value, UpdatedAt)
- `SettingsDto` — Read model
- `UpdateSettingsDto` — Write model
