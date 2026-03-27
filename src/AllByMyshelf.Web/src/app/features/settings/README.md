# Settings Feature — Frontend

Unified settings page for managing API credentials and application preferences.

## Components

| Component | Description |
|-----------|-------------|
| `SettingsComponent` | Single page for all user configuration |

## Services

- **SettingsService** (core) — CRUD operations for settings via `/api/v1/settings`
- **ThemeService** (core) — Theme switching (light, dark, os-default)
- **FeaturesService** (core) — Feature flag management

## Routes

| Route | Component | Description |
|-------|-----------|-------------|
| `/settings` | SettingsComponent | Settings page |

## UI Features

- API token management: BoardGameGeek, Discogs, and Hardcover tokens
- Username configuration: BoardGameGeek and Discogs usernames
- Theme selector: light, dark, OS-default
- Token inputs masked for security
- Form controls reset after successful save
- Save state indicator
- Feature flags refresh on settings update
- Max length validation on all input fields (2000 for tokens, 100 for usernames)
