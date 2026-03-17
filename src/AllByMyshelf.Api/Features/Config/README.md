# Config Feature

Exposes feature flags indicating which external integrations are enabled based on API token configuration.

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/config/features` | Returns enabled status for BGG, Discogs, and Hardcover |

## Key Components

- **ConfigController** — Reads `BggOptions`, `DiscogsOptions`, and `HardcoverOptions` to determine which integrations have valid tokens configured

## Models

- `FeaturesDto` — Contains `BggEnabled`, `DiscogsEnabled`, `HardcoverEnabled` boolean flags
