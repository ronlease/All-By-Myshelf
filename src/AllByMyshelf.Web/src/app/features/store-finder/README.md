# Store Finder Feature — Frontend

Location-based search for nearby record stores and bookstores using OpenStreetMap data.

## Components

| Component | Description |
|-----------|-------------|
| `StoreFinderComponent` | Search UI with location input and results display |

## Services

- **StoreFinderService** — Multi-API integration:
  - **Nominatim** (OpenStreetMap) for geocoding US locations
  - **Overpass API** for POI queries (music/records/vinyl shops and bookstores)
  - Excludes major chains (Amazon, Target, Walmart, etc.)

## Routes

| Route | Component | Description |
|-------|-----------|-------------|
| `/store-finder` | StoreFinderComponent | Store search page |
| `/store-finder?type=books` | StoreFinderComponent | Bookstore mode |

## UI Features

- Location input: accepts US ZIP codes (12345 or 12345-6789) or City, State format
- Toggle between record stores and bookstores
- Custom form validation for US locations
- Results display store name and address
- External API integration (no backend proxying)
- Loading state and error handling for location not found
