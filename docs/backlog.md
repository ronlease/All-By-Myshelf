# All By Myshelf — Product Backlog

---

## [ABM-001] Store Discogs Personal Access Token

**Status:** Backlog
**Priority:** High

### Business Problem
Before the application can communicate with the Discogs API, it needs a way to securely hold my personal access token. Storing it in user-secrets keeps it off disk and out of source control so the credential is never accidentally exposed.

### Acceptance Criteria
```gherkin
Feature: Discogs personal access token configuration

  Scenario: Application starts with a valid token configured
    Given the Discogs personal access token has been set in user-secrets
    When the application starts
    Then the application reads the token without error
    And the token is available to the Discogs API client

  Scenario: Application starts without a token configured
    Given the Discogs personal access token has NOT been set in user-secrets
    When the application starts
    Then the application logs a clear error indicating the token is missing
    And any request to the Discogs API client returns a configuration error rather than attempting an unauthenticated call
```

---

## [ABM-002] Sync Discogs Collection in the Background

**Status:** Backlog
**Priority:** High

### Business Problem
I want to import my ~150 Discogs records into the local database so the application has a local copy to serve from. The import can take a few minutes due to Discogs API rate limits, so it must run as a background process and not block the HTTP response that triggered it.

### Acceptance Criteria
```gherkin
Feature: Background sync of Discogs collection

  Scenario: Sync is triggered and runs in the background
    Given the Discogs personal access token is configured
    And no sync is currently running
    When I trigger a manual sync
    Then the API responds immediately with HTTP 202 Accepted
    And the sync runs asynchronously in the background

  Scenario: Sync fetches all releases from the flat collection
    Given the Discogs personal access token is configured
    When a sync runs
    Then every release in my Discogs collection is retrieved from the Discogs API
    And each release record captures artist, title, year, and format

  Scenario: Sync respects Discogs API rate limits
    Given a sync is running
    When the Discogs API returns a 429 Too Many Requests response
    Then the sync pauses and retries that request after the indicated delay
    And the sync eventually completes without losing records

  Scenario: Sync is already in progress
    Given a sync is currently running
    When I trigger another manual sync
    Then the API responds with HTTP 409 Conflict
    And no second sync process is started
```

---

## [ABM-003] Persist Discogs Collection to the Local Database

**Status:** Backlog
**Priority:** High

### Business Problem
After fetching records from Discogs, I need them stored locally so the dashboard can be viewed instantly without hitting the external API on every page load. Re-running a sync should refresh the data rather than create duplicates.

### Acceptance Criteria
```gherkin
Feature: Persisting synced releases to the database

  Scenario: New releases are inserted on first sync
    Given the local database contains no releases
    When a sync completes successfully
    Then all releases retrieved from Discogs are saved to the database
    And each release has artist, title, year, and format persisted

  Scenario: Existing releases are updated on subsequent sync
    Given the local database already contains releases from a previous sync
    When a sync completes successfully
    Then releases that still exist in the Discogs collection are updated with current data
    And releases that no longer exist in the Discogs collection are removed from the database
    And no duplicate release records are created

  Scenario: Sync failure does not corrupt existing data
    Given the local database contains releases from a previous sync
    When a sync fails partway through
    Then the previously stored releases remain intact in the database
```

---

## [ABM-004] Expose a Paginated API Endpoint for the Collection

**Status:** Backlog
**Priority:** High

### Business Problem
I need an API endpoint that returns my locally stored record collection so the frontend can display it as a paginated list. Serving from the local database keeps responses fast regardless of Discogs API availability.

### Acceptance Criteria
```gherkin
Feature: Paginated collection endpoint

  Scenario: Retrieve the first page of releases
    Given the database contains releases
    When I request GET /api/v1/releases?page=1&pageSize=25
    Then the response is HTTP 200 OK
    And the response body contains up to 25 releases
    And each release includes artist, title, year, and format
    And the response includes total record count and total page count

  Scenario: Retrieve a subsequent page
    Given the database contains more than 25 releases
    When I request GET /api/v1/releases?page=2&pageSize=25
    Then the response is HTTP 200 OK
    And the response body contains releases from the second page
    And the releases on page 2 do not overlap with those on page 1

  Scenario: Request a page beyond the available data
    Given the database contains 30 releases
    When I request GET /api/v1/releases?page=5&pageSize=25
    Then the response is HTTP 200 OK
    And the response body contains an empty releases array
    And the total record count still reflects 30

  Scenario: Database contains no releases
    Given no sync has been run and the database contains no releases
    When I request GET /api/v1/releases?page=1&pageSize=25
    Then the response is HTTP 200 OK
    And the response body contains an empty releases array
    And the total record count is 0
```

---

## [ABM-005] Trigger a Manual Collection Sync

**Status:** Backlog
**Priority:** High

### Business Problem
I need a way to tell the application to pull fresh data from Discogs whenever I want. A simple API endpoint gives me that control without requiring a deployment or server restart.

### Acceptance Criteria
```gherkin
Feature: Manual sync trigger endpoint

  Scenario: Successfully trigger a sync
    Given the Discogs personal access token is configured
    And no sync is currently running
    When I send POST /api/v1/sync
    Then the response is HTTP 202 Accepted
    And the response body includes a message confirming the sync has started

  Scenario: Attempt to trigger sync while one is already running
    Given a sync is currently in progress
    When I send POST /api/v1/sync
    Then the response is HTTP 409 Conflict
    And the response body explains that a sync is already in progress
    And the running sync is not interrupted

  Scenario: Attempt to trigger sync with no token configured
    Given the Discogs personal access token is NOT configured
    When I send POST /api/v1/sync
    Then the response is HTTP 503 Service Unavailable
    And the response body explains that the Discogs token is not configured
```
