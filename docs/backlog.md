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

---

## [ABM-006] MVP Collection Dashboard (Angular Frontend)

**Status:** Backlog
**Priority:** High

### Business Problem
I need a browser-based interface to actually see and use the collection data the API provides. Without a frontend, the application has no practical value day-to-day. The dashboard must require me to be logged in, show my records in a paginated list, and let me kick off a fresh sync without opening a separate HTTP client.

### Acceptance Criteria
```gherkin
Feature: MVP collection dashboard

  Scenario: Unauthenticated user is redirected to login
    Given I am not logged in
    When I navigate to the dashboard
    Then I am redirected to the Auth0 login page
    And I cannot see any collection data

  Scenario: Authenticated user sees the collection list
    Given I am logged in
    And the API returns a non-empty page of releases
    When the dashboard loads
    Then I see a list of releases
    And each row displays the artist, title, year, and format
    And pagination controls are visible

  Scenario: Navigating to another page of results
    Given I am logged in
    And the collection contains more releases than fit on one page
    When I click to go to the next page
    Then the list updates to show the next page of releases
    And the pagination controls reflect the new current page

  Scenario: Collection is empty
    Given I am logged in
    And the API returns zero releases
    When the dashboard loads
    Then I see a message indicating the collection is empty
    And no list rows are rendered

  Scenario: Dashboard shows a loading indicator while fetching data
    Given I am logged in
    When the dashboard is waiting for the API to respond
    Then a loading indicator is visible
    And the list area is not shown until data has loaded

  Scenario: Triggering a manual sync successfully
    Given I am logged in
    When I click the Sync button
    And the API responds with HTTP 202 Accepted
    Then the Sync button is disabled for the duration of the operation
    And I see a success notification confirming the sync has started

  Scenario: Triggering a sync while one is already running
    Given I am logged in
    When I click the Sync button
    And the API responds with HTTP 409 Conflict
    Then I see a notification informing me a sync is already in progress

  Scenario: Triggering a sync when the token is not configured
    Given I am logged in
    When I click the Sync button
    And the API responds with HTTP 503 Service Unavailable
    Then I see a notification informing me the Discogs token is not configured
```

---

## [ABM-007] GitHub Actions — CI Pipeline

**Status:** Backlog
**Priority:** High

### Business Problem
Without automated checks on every pull request and push to main, broken builds and failing tests can silently land in the codebase. I also have no systematic guardrail against accidentally committing credentials or pulling in packages with known vulnerabilities. A CI pipeline catches these problems before they reach production.

### Acceptance Criteria
```gherkin
Feature: GitHub Actions CI pipeline

  Scenario: .NET build and tests pass on a pull request
    Given a pull request is opened or updated against main
    When the CI pipeline runs
    Then the .NET solution builds without errors
    And all xUnit unit and integration tests pass
    And a failing test causes the pipeline to fail and block the PR

  Scenario: Angular build passes on a pull request
    Given a pull request is opened or updated against main
    When the CI pipeline runs
    Then the Angular application compiles without errors
    And a compilation error causes the pipeline to fail and block the PR

  Scenario: .NET dependency vulnerability scan runs on a pull request
    Given a pull request is opened or updated against main
    When the CI pipeline runs
    Then `dotnet list package --vulnerable` is executed
    And if any vulnerable packages are detected the pipeline fails and reports them

  Scenario: npm dependency vulnerability scan runs on a pull request
    Given a pull request is opened or updated against main
    When the CI pipeline runs
    Then `npm audit --audit-level=high` is executed against the Angular project
    And if high or critical vulnerabilities are found the pipeline fails and reports them

  Scenario: Secret scanning prevents credential commits
    Given a pull request is opened or updated against main
    When the CI pipeline runs
    Then GitHub secret scanning or an equivalent step checks the diff for committed secrets
    And if a secret pattern is detected the pipeline fails and reports the finding

  Scenario: CI pipeline also runs on direct push to main
    Given a commit is pushed directly to main
    When the CI pipeline runs
    Then all of the same build, test, and scanning steps execute
    And any failure is reported on the commit
```

---

## [ABM-008] Auto-Refresh Collection After Sync Completes

**Status:** Backlog
**Priority:** Medium

### Business Problem
After I trigger a sync, the collection list on the dashboard still shows stale data until I manually reload the page. Because the sync runs in the background and returns 202 immediately, I have no easy way to know when it is done. The dashboard should update itself so I can see my refreshed collection without any extra effort.

### Acceptance Criteria
```gherkin
Feature: Auto-refresh collection after sync completes

  Scenario: Collection list refreshes after sync starts
    Given I am logged in
    And the dashboard is showing my current collection
    When I click the Sync button
    And the API responds with HTTP 202 Accepted
    Then the dashboard automatically re-fetches the collection list within 10 seconds of the sync starting
    And the updated list is displayed without a manual page reload

  Scenario: Sync button remains disabled during the auto-refresh window
    Given I have triggered a sync
    And the dashboard is waiting before re-fetching the collection
    When I view the Sync button
    Then it remains disabled until the collection re-fetch has completed

  Scenario: Collection list shows updated data after re-fetch
    Given the dashboard has re-fetched the collection after a sync
    When the new data arrives from the API
    Then the list is updated to reflect the latest releases
    And the pagination controls reflect the new total if the count changed

  Scenario: API error during auto-refresh is handled gracefully
    Given the dashboard has triggered an auto-refresh after a sync
    When the API returns an error response during the re-fetch
    Then I see an error notification indicating the refresh failed
    And the previously displayed collection data remains visible
    And the Sync button is re-enabled so I can try again
```

---

## [ABM-009] Respect OS Light/Dark Mode Preference

**Status:** Backlog
**Priority:** Medium

### Business Problem
I switch between light and dark mode at the OS level depending on the time of day and environment. The dashboard should follow that preference automatically so I never have to configure appearance inside the app itself.

### Acceptance Criteria
```gherkin
Feature: OS light/dark mode preference

  Scenario: Dashboard renders in dark mode when OS preference is dark
    Given my operating system is set to dark mode
    When I open the dashboard
    Then the application renders using the dark color scheme
    And no manual theme toggle is required

  Scenario: Dashboard renders in light mode when OS preference is light
    Given my operating system is set to light mode
    When I open the dashboard
    Then the application renders using the light color scheme
    And no manual theme toggle is required

  Scenario: Dashboard updates when OS preference changes while the app is open
    Given I have the dashboard open
    When I change my operating system color scheme preference
    Then the dashboard switches to the new color scheme without a page reload
```

---

## [ABM-010] Purple Brand Theme Using Material 3 Seed Color

**Status:** Backlog
**Priority:** Medium

### Business Problem
The application uses a generic default color palette that does not feel personal. I want the dashboard to use my favourite color, DarkOrchid purple (#9932cc), as the brand identity. The full accessible palette — including all tones for both light and dark schemes — should be derived automatically from that seed so I do not have to hand-pick individual colors.

### Acceptance Criteria
```gherkin
Feature: Purple Material 3 seed color theme

  Scenario: Primary brand color is derived from the DarkOrchid seed
    Given the Angular Material 3 theme is configured with seed color #9932cc
    When I open the dashboard in light mode
    Then primary interactive elements (buttons, links, active states) use a purple tone derived from the seed
    And no element uses the Angular Material default blue or teal palette

  Scenario: Light scheme meets WCAG AA contrast for normal text
    Given the dashboard is rendered in light mode
    When I inspect the contrast ratio of body text against its background
    Then all normal-weight text at 16px or below meets a minimum contrast ratio of 4.5:1

  Scenario: Dark scheme meets WCAG AA contrast for normal text
    Given the dashboard is rendered in dark mode
    When I inspect the contrast ratio of body text against its background
    Then all normal-weight text at 16px or below meets a minimum contrast ratio of 4.5:1

  Scenario: Theme is consistent across all dashboard views
    Given the seed color theme is applied
    When I navigate between the collection list and any other page in the app
    Then the purple-derived color scheme is applied consistently on every view
```

---

## [ABM-011] Album Art Thumbnails on the Collection List

**Status:** Backlog
**Priority:** Medium

### Business Problem
The collection list currently shows only text — artist, title, year, and format. My records have cover art on Discogs and seeing those images at a glance makes the list much more recognisable and enjoyable to browse. The Discogs collection endpoint already returns image URLs, so no additional API calls are needed to support this.

### Acceptance Criteria
```gherkin
Feature: Album art thumbnails on the collection list

  Scenario: Release with a cover image displays a thumbnail
    Given I am logged in
    And the API returns a release that includes a cover image URL
    When the collection list renders
    Then a thumbnail image is displayed alongside the artist, title, year, and format for that release
    And the image is loaded directly from the Discogs CDN URL

  Scenario: Release with no cover image displays a placeholder
    Given I am logged in
    And the API returns a release that has no cover image URL
    When the collection list renders
    Then a placeholder graphic or empty image area is shown in place of the thumbnail
    And no broken image icon is visible

  Scenario: Thumbnail does not disrupt the list layout
    Given I am logged in
    And the collection list contains a mix of releases with and without cover images
    When the collection list renders
    Then all rows are consistently sized and aligned regardless of whether a thumbnail is present
```

---

## [ABM-012] Release Detail View

**Status:** Backlog
**Priority:** Medium

### Business Problem
The collection list shows only a summary of each record. When I want to recall specifics — label, country, genres, styles, or personal notes — I have to leave the app and look it up on Discogs. A detail view surfaces that information in context and links directly to the full Discogs page so I can get deeper when needed, all without leaving my dashboard as the starting point.

### Acceptance Criteria
```gherkin
Feature: Release detail view

  Scenario: Opening the detail view for a release
    Given I am logged in
    And the collection list is showing at least one release
    When I click or tap a release in the list
    Then a detail view opens for that release
    And the detail view displays artist, title, year, format, label, country, genres, styles, and notes where those fields are available from the API
    And fields with no data are not shown

  Scenario: "View on Discogs" link opens the release page in a new tab
    Given I am viewing the detail view for a release with a known Discogs ID
    When I click the "View on Discogs" link
    Then a new browser tab opens at https://www.discogs.com/release/{discogsId} for that release
    And the current dashboard view remains open in the original tab

  Scenario: Closing the detail view returns to the collection list
    Given I have opened the detail view for a release
    When I close or dismiss the detail view
    Then I am returned to the collection list
    And the list is in the same state (same page, same scroll position) as before I opened the detail view

  Scenario: Detail view handles missing optional fields gracefully
    Given I am viewing the detail view for a release where label, country, genres, styles, and notes are all absent
    When the detail view renders
    Then only the fields that have data are displayed
    And no empty rows, blank labels, or placeholder text such as "N/A" are shown
```
