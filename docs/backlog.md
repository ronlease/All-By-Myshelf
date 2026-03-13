# All By Myshelf — Product Backlog

---

## [ABM-001] Store Discogs Personal Access Token

**Status:** Done
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

**Status:** Done
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

**Status:** Done
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

**Status:** Done
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

**Status:** Done
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

**Status:** Done
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

**Status:** Done
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

**Status:** Done
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

**Status:** Done
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

**Status:** Done
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

**Status:** Done
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

**Status:** Done
**Priority:** Medium

### Business Problem
The collection list shows only a summary of each record. When I want to recall specifics — label, country, genre, styles, or personal notes — I have to leave the app and look it up on Discogs. A detail view surfaces that information in context and links directly to the full Discogs page so I can get deeper when needed, all without leaving my dashboard as the starting point.

### Data Notes
- The sync (ABM-002/ABM-003) is extended to call `GET https://api.discogs.com/releases/{discogsId}` for each release and store: label, country, genre (primary genre only), notes, and styles (stored as a comma-separated list). These fields are populated by a manual resync — no on-demand per-release API calls are made.
- Album art on this view is a placeholder image only. Real cover art is handled by ABM-011.

### Acceptance Criteria
```gherkin
Feature: Release detail view

  Scenario: Navigating to the detail view for a release
    Given I am logged in
    And the collection list is showing at least one release
    When I click a release in the list
    Then the application navigates to /releases/{id}
    And the detail view displays artist, title, year, and format
    And any of the following fields that are present in the stored data are also displayed: label, country, genre, styles, notes
    And a placeholder image is shown in the album art area

  Scenario: Fields absent from the stored release data are omitted
    Given I am logged in
    And I navigate to /releases/{id} for a release where label, country, genre, styles, and notes are all absent
    When the detail view renders
    Then only the fields that have stored data are displayed
    And no empty rows, blank labels, or placeholder text such as "N/A" are shown

  Scenario: "View on Discogs" link opens the release page in a new tab
    Given I am logged in
    And I am viewing the detail view for a release with a known Discogs ID
    When I click the "View on Discogs" link
    Then a new browser tab opens at https://www.discogs.com/release/{discogsId}
    And the detail view remains open in the original tab

  Scenario: Navigating back returns to the collection list
    Given I am logged in
    And I have navigated to /releases/{id}
    When I navigate back
    Then I am returned to the collection list
    And the list is in the same state (same page, same scroll position) as before I opened the detail view

  Scenario: Detail view fields are populated after a resync
    Given a release was previously synced without the extended detail fields
    When I trigger a manual resync
    And the sync calls the Discogs release detail endpoint for that release
    Then label, country, genre, notes, and styles (where present in the Discogs response) are stored in the database
    And the detail view at /releases/{id} displays the newly stored fields
```

---

## [ABM-013] Local Independent Record Store Finder

**Status:** Done
**Priority:** Low

### Business Problem
When I am looking to buy records locally, I have no quick way to find independent record stores near me. I want to enter a US zip code or city and state and get back a list of nearby music shops so I know where to go without relying on a general-purpose search engine that buries independent stores under big-box retail results.

### Acceptance Criteria
```gherkin
Feature: Local independent record store finder

  Scenario: Search by zip code returns nearby music shops
    Given I am on the store finder page
    When I enter a valid US zip code and submit the search
    Then the application queries OpenStreetMap via the Overpass API for nodes and ways tagged shop=music within a reasonable radius of that zip code
    And the results list displays each store's name and address
    And stores whose names match known big-box retailers (Target, Walmart, Best Buy, Amazon, FYE) are excluded from the results

  Scenario: Search by city and state returns nearby music shops
    Given I am on the store finder page
    When I enter a US city name and a two-letter state abbreviation and submit the search
    Then the application queries OpenStreetMap via the Overpass API for nodes and ways tagged shop=music in that city
    And the results list displays each store's name and address
    And stores whose names match known big-box retailers (Target, Walmart, Best Buy, Amazon, FYE) are excluded from the results

  Scenario: Search returns no results
    Given I am on the store finder page
    When I submit a search for a location that has no shop=music nodes or ways in OpenStreetMap
    Then the results area displays a message indicating no stores were found
    And no list rows are rendered

  Scenario: Search input is empty
    Given I am on the store finder page
    When I submit the search form without entering a zip code or city and state
    Then the form displays a validation message indicating that a location is required
    And no API call is made to the Overpass API

  Scenario: Non-US location is entered
    Given I am on the store finder page
    When I enter a location that does not correspond to a US zip code or US city and state format
    Then the form displays a validation message indicating that only US locations are supported
    And no API call is made to the Overpass API

  Scenario: Overpass API request fails
    Given I am on the store finder page
    And the Overpass API is unavailable or returns an error
    When I submit a valid US location search
    Then the results area displays an error message indicating the store data could not be retrieved
    And no partial or empty results list is shown

  Scenario: Store detail does not include hours, phone, or website
    Given the search has returned one or more results
    When I view the results list
    Then each result shows only the store name and address
    And no hours, phone number, or website link is displayed for any store
```

---

## [ABM-014] Genre Column on Collection List

**Status:** Done
**Priority:** Medium

### Business Problem
The collection list shows artist, title, year, and format but omits genre. Genre is one of the first things I use to orient myself when browsing, and having to open a detail view just to see it breaks my scanning flow. Adding genre as a column means the most useful classification information is visible at a glance without any extra navigation.

### Acceptance Criteria
```gherkin
Feature: Genre column on the collection list

  Scenario: Release with a genre displays it in the list
    Given I am logged in
    And the API returns a release that has a genre value stored
    When the collection list renders
    Then a Genre column is visible in the list
    And the genre value for that release is displayed in the Genre column

  Scenario: Release without a genre shows an empty cell
    Given I am logged in
    And the API returns a release that has no genre value stored
    When the collection list renders
    Then the Genre column cell for that release is empty
    And no placeholder text such as "N/A" is shown

  Scenario: Genre column is consistently present regardless of data
    Given I am logged in
    And the collection contains a mix of releases with and without a stored genre
    When the collection list renders
    Then the Genre column header is always visible
    And each row has a Genre cell in the correct column position alongside artist, title, year, and format
```

---

## [ABM-015] Collection Search

**Status:** Done
**Priority:** Medium

### Business Problem
As my collection grows, scrolling through pages to find a specific release becomes tedious. I want a single search input that filters the visible results across artist, title, year, format, and genre simultaneously so I can narrow the list down to what I am looking for in seconds.

### Acceptance Criteria
```gherkin
Feature: Collection search

  Scenario: Typing in the search input filters the collection list
    Given I am logged in
    And the collection list is showing releases
    When I type a search term into the search input
    Then only releases whose artist, title, year, format, or genre contain the search term (case-insensitive) are displayed
    And the pagination reflects the filtered result count

  Scenario: Clearing the search input restores the full collection
    Given I am logged in
    And I have typed a search term that has narrowed the list
    When I clear the search input
    Then the full unfiltered collection list is displayed again
    And the pagination reflects the total unfiltered record count

  Scenario: Search term that matches no releases shows an empty state
    Given I am logged in
    When I type a search term that matches no release in any field
    Then the collection list shows no rows
    And a message is displayed indicating no results were found for that term

  Scenario: Search resets to the first page
    Given I am logged in
    And I am viewing page 2 or later of the collection
    When I type a search term into the search input
    Then the results reset to page 1
    And the pagination controls reflect the new filtered page count
```

---

## [ABM-016] Column Header Filtering

**Status:** Done
**Priority:** Low

### Business Problem
The global search input (ABM-015) narrows results across all fields at once, but sometimes I want to filter on a specific column — for example, all releases in a given year or all releases of a certain format — without that filter term also matching unrelated fields. Per-column filter inputs at the header level give me that precision.

### Acceptance Criteria
```gherkin
Feature: Column header filtering

  Scenario: Entering a value in a column filter narrows the list to that column
    Given I am logged in
    And the collection list is showing releases
    When I enter a filter value in the column header filter for a specific column
    Then only releases whose value in that column contains the filter term (case-insensitive) are displayed
    And releases that match in other columns but not the filtered column are excluded

  Scenario: Multiple column filters are applied together
    Given I am logged in
    And the collection list is showing releases
    When I enter filter values in two or more column header filters
    Then only releases that satisfy all active column filters simultaneously are displayed

  Scenario: Clearing a column filter restores results for that column
    Given I have an active column header filter that has narrowed the list
    When I clear the filter input for that column
    Then releases previously excluded only by that column filter reappear
    And any other active column filters remain in effect

  Scenario: Column filters reset to the first page
    Given I am logged in
    And I am viewing page 2 or later of the collection
    When I enter a value in any column header filter
    Then the results reset to page 1
    And the pagination controls reflect the new filtered page count
```

---

## [ABM-017] Collection Grouping

**Status:** Done
**Priority:** Low

### Business Problem
When I want to browse my collection by a particular dimension — say, all my records from a given year, or everything in a format — a flat paginated list forces me to scroll and search rather than navigate naturally. A grouping option lets me select one dimension and see the collection organized into collapsible sections, making thematic browsing effortless.

### Acceptance Criteria
```gherkin
Feature: Collection grouping

  Scenario: Default state shows no grouping
    Given I am logged in
    And I have not changed the grouping selection
    When the collection list renders
    Then the grouping combobox displays "No grouping"
    And the collection is displayed as a flat paginated list

  Scenario: Selecting a grouping field organizes the list into groups
    Given I am logged in
    When I select a grouping field (artist, format, year, or genre) from the grouping combobox
    Then the collection list is reorganized into sections, one per distinct value of the selected field
    And each section header displays the group value
    And all sections are collapsed by default
    And pagination is no longer shown while grouping is active

  Scenario: Expanding a group reveals its releases
    Given I have selected a grouping field
    And the collection list shows collapsed group sections
    When I click on a group section header
    Then that section expands to show all releases belonging to that group
    And other sections remain in their current collapsed or expanded state

  Scenario: Collapsing an expanded group hides its releases
    Given I have selected a grouping field
    And at least one group section is expanded
    When I click the expanded section header
    Then that section collapses and its releases are hidden

  Scenario: Changing the grouping selection resets all groups to collapsed
    Given I have selected a grouping field
    And one or more sections are expanded
    When I select a different grouping field from the combobox
    Then the collection is regrouped by the new field
    And all sections in the new grouping are collapsed

  Scenario: Selecting "No grouping" returns to the flat paginated list
    Given I have an active grouping selection
    When I select "No grouping" from the grouping combobox
    Then the collection returns to the flat paginated list view
    And pagination controls reappear
```

---

## [ABM-018] Statistics Dashboard

**Status:** Done
**Priority:** Medium

### Business Problem
I have a growing vinyl collection but no way to see patterns or trends in what I own. I want a dedicated statistics page with visual breakdowns — by genre, format, decade, artist, and label — so I can understand my collection at a glance. This helps me identify gaps (genres I am under-represented in), spot my biases (decades I gravitate toward), and simply appreciate the shape of my library.

### Acceptance Criteria
```gherkin
Feature: Statistics dashboard

  Scenario: Navigating to the statistics page
    Given I am logged in
    When I navigate to /statistics
    Then the statistics dashboard page loads
    And the page title indicates this is the collection statistics view

  Scenario: Genre breakdown chart is displayed
    Given I am logged in
    And my collection contains releases with genre data
    When the statistics page renders
    Then a chart displays the distribution of releases by genre
    And each genre segment shows the genre name and count or percentage

  Scenario: Format breakdown chart is displayed
    Given I am logged in
    And my collection contains releases with format data
    When the statistics page renders
    Then a chart displays the distribution of releases by format
    And each format segment shows the format name and count or percentage

  Scenario: Decade breakdown chart is displayed
    Given I am logged in
    And my collection contains releases with year data
    When the statistics page renders
    Then a chart displays the distribution of releases by decade
    And each decade segment shows the decade label (e.g., "1970s") and count or percentage

  Scenario: Top artists list is displayed
    Given I am logged in
    And my collection contains releases from multiple artists
    When the statistics page renders
    Then a ranked list of the top 10 artists by release count is displayed
    And each entry shows the artist name and number of releases

  Scenario: Top labels list is displayed
    Given I am logged in
    And my collection contains releases with label data
    When the statistics page renders
    Then a ranked list of the top 10 labels by release count is displayed
    And each entry shows the label name and number of releases

  Scenario: Collection contains no releases
    Given I am logged in
    And my collection is empty
    When the statistics page renders
    Then the page displays a message indicating there is no data to display
    And no charts or lists are rendered

  Scenario: Releases missing a particular field are excluded from that chart
    Given I am logged in
    And some releases have no genre stored
    When the genre breakdown chart renders
    Then only releases with a stored genre are counted
    And a note or separate "Unknown" category indicates how many releases lack genre data
```

---

## [ABM-019] Random Record Picker

**Status:** Done
**Priority:** Medium

### Business Problem
Sometimes I cannot decide what to play. Staring at a list of 150 records leads to decision paralysis, and I end up putting on the same few favorites. I want a "What should I listen to?" feature that randomly selects a record for me. Optional filters let me narrow the pool first — maybe I am in the mood for jazz, or I only want to consider records from the 1980s — before the app surprises me with a selection.

### Acceptance Criteria
```gherkin
Feature: Random record picker

  Scenario: Navigating to the random picker page
    Given I am logged in
    When I navigate to /pick
    Then the random picker page loads
    And a "Pick for me" button is visible

  Scenario: Picking a random record with no filters
    Given I am logged in
    And I have not applied any filters
    When I click the "Pick for me" button
    Then a single release is selected at random from the entire collection
    And the selected release is displayed with its cover art, artist, title, year, format, and genre

  Scenario: Picking a random record with a genre filter
    Given I am logged in
    And I select a genre from the filter options
    When I click the "Pick for me" button
    Then a single release is selected at random from only releases matching that genre
    And the selected release is displayed

  Scenario: Picking a random record with a decade filter
    Given I am logged in
    And I select a decade from the filter options
    When I click the "Pick for me" button
    Then a single release is selected at random from only releases in that decade
    And the selected release is displayed

  Scenario: Picking a random record with a format filter
    Given I am logged in
    And I select a format from the filter options
    When I click the "Pick for me" button
    Then a single release is selected at random from only releases matching that format
    And the selected release is displayed

  Scenario: Combining multiple filters narrows the pool
    Given I am logged in
    And I select both a genre filter and a decade filter
    When I click the "Pick for me" button
    Then a single release is selected at random from only releases matching both filters
    And the selected release is displayed

  Scenario: No releases match the selected filters
    Given I am logged in
    And I select filters that exclude all releases in my collection
    When I click the "Pick for me" button
    Then a message is displayed indicating no records match the current filters
    And no release is shown

  Scenario: Clicking the button again picks a new random record
    Given I am logged in
    And a release has already been selected
    When I click the "Pick for me" button again
    Then a new release is selected at random (which may or may not differ from the previous selection)
    And the display updates to show the new selection

  Scenario: Link to view the selected release details
    Given I am logged in
    And a release has been selected by the random picker
    When I click on the displayed release
    Then I am navigated to /releases/{id} for that release
```

---

## [ABM-020] Collection Value Estimate

**Status:** Done
**Priority:** Medium

### Business Problem
I am curious what my vinyl collection is worth on the secondhand market. Discogs provides low, median, and high marketplace pricing data for most releases. Displaying this information per record — and summing it for a total collection value — helps me understand the financial dimension of my hobby, whether for insurance purposes, bragging rights, or deciding which records to sell.

### Data Notes
- The Discogs release detail endpoint returns pricing data in the `lowest_price` field on the release object, but for full low/median/high stats, the `/releases/{id}` endpoint or the `community` statistics may be used. The sync should fetch and store `lowest_price`, `median_price`, and `highest_price` per release (where available).
- Value is displayed in USD. Currency conversion is out of scope.

### Acceptance Criteria
```gherkin
Feature: Collection value estimate

  Scenario: Sync stores marketplace pricing data for each release
    Given a manual sync is triggered
    When the sync fetches a release from the Discogs API
    Then the lowest price, median price, and highest price (if available) are stored in the database for that release

  Scenario: Release detail view shows marketplace pricing
    Given I am logged in
    And I navigate to /releases/{id} for a release that has pricing data stored
    When the detail view renders
    Then the lowest price, median price, and highest price are displayed
    And the prices are shown in USD format

  Scenario: Release without pricing data does not show a price section
    Given I am logged in
    And I navigate to /releases/{id} for a release that has no pricing data stored
    When the detail view renders
    Then no marketplace pricing section is displayed
    And no placeholder text such as "$0.00" or "N/A" is shown for price

  Scenario: Statistics page displays total collection value
    Given I am logged in
    And my collection contains releases with pricing data
    When I navigate to /statistics
    Then a "Collection Value" section is visible
    And the section displays the sum of median prices for all releases that have pricing data
    And the value is shown in USD format

  Scenario: Collection value excludes releases without pricing
    Given I am logged in
    And some releases have no pricing data stored
    When the collection value is calculated
    Then only releases with a stored median price are included in the sum
    And a note indicates how many releases were excluded from the calculation

  Scenario: Collection value when no releases have pricing data
    Given I am logged in
    And no releases have pricing data stored
    When I view the collection value section on the statistics page
    Then a message is displayed indicating pricing data is unavailable
    And no dollar amount is shown
```

---

## [ABM-021] Recently Added Releases

**Status:** Done
**Priority:** Low

### Business Problem
After a Discogs sync, I want to see which records are new to my collection since the last sync. When I add records on Discogs and then sync to the dashboard, a "Recently Added" section lets me confirm the import worked and highlights my newest acquisitions without having to hunt through the full list.

### Data Notes
- The sync should track the date each release was first added to the local database. This timestamp is set once on first insert and never updated on subsequent syncs.

### Acceptance Criteria
```gherkin
Feature: Recently added releases

  Scenario: Sync stores an "added" timestamp for newly inserted releases
    Given a release does not exist in the local database
    When a sync inserts that release for the first time
    Then an "added" timestamp is stored with the current date and time
    And subsequent syncs do not update that timestamp

  Scenario: Dashboard shows a "Recently Added" section
    Given I am logged in
    And at least one release has an "added" timestamp within the last 30 days
    When the dashboard loads
    Then a "Recently Added" section is visible
    And the section displays releases added in the last 30 days, sorted by most recent first

  Scenario: Recently added section shows limited number of releases
    Given I am logged in
    And more than 10 releases have been added in the last 30 days
    When the recently added section renders
    Then at most 10 releases are displayed in the section
    And a link is provided to view all recently added releases

  Scenario: No releases added recently
    Given I am logged in
    And no releases have an "added" timestamp within the last 30 days
    When the dashboard loads
    Then the "Recently Added" section is not displayed
    And no empty placeholder is shown

  Scenario: Clicking a recently added release navigates to its detail view
    Given I am logged in
    And the recently added section is displaying releases
    When I click on a release in the section
    Then I am navigated to /releases/{id} for that release
```

---

## [ABM-022] Wishlist Tracking

**Status:** Backlog
**Priority:** Low

### Business Problem
Discogs lets me maintain a wantlist of records I am hunting for. I want to see my wantlist alongside my collection in the dashboard so I can track what I am looking for and celebrate when items move from "wanted" to "owned." Surfacing wantlist items here saves me from switching between the dashboard and Discogs to check what I am still seeking.

### Data Notes
- The Discogs API provides a wantlist endpoint (`GET /users/{username}/wants`) that returns paginated results. This should be synced similarly to the collection.
- Wantlist releases are stored in a separate table or with a flag distinguishing them from owned releases.

### Acceptance Criteria
```gherkin
Feature: Wishlist tracking

  Scenario: Sync fetches wantlist releases from Discogs
    Given the Discogs personal access token is configured
    When a sync runs
    Then the sync retrieves all releases from my Discogs wantlist
    And each wantlist release is stored in the database with artist, title, year, format, and cover image URL

  Scenario: Navigating to the wantlist page
    Given I am logged in
    When I navigate to /wantlist
    Then the wantlist page loads
    And a paginated list of wantlist releases is displayed

  Scenario: Wantlist display matches the collection list format
    Given I am logged in
    And the wantlist contains releases
    When the wantlist page renders
    Then each row displays cover thumbnail, artist, title, year, format, and genre
    And pagination controls are visible

  Scenario: Wantlist release is removed from Discogs
    Given a release is in the local wantlist database
    When a sync runs
    And that release is no longer in my Discogs wantlist
    Then the release is removed from the local wantlist database

  Scenario: Wantlist item moves to collection
    Given a release is in my Discogs wantlist and synced locally
    When I add that same release to my Discogs collection
    And a sync runs
    Then the release appears in the local collection
    And the release is removed from the local wantlist (if it was removed from Discogs wantlist)

  Scenario: Wantlist page shows empty state
    Given I am logged in
    And my wantlist is empty
    When I navigate to /wantlist
    Then a message is displayed indicating the wantlist is empty
    And no list rows are rendered
```

---

## [ABM-023] Listening Notes and Personal Ratings

**Status:** Backlog
**Priority:** Low

### Business Problem
Discogs is great for catalog data, but I want to capture my own thoughts about my records. A personal notes field and a simple 1-to-5 star rating let me record my impressions — when I last played a record, whether it is a favorite, or any other observations. This data stays local since Discogs does not store arbitrary personal notes.

### Data Notes
- Notes and ratings are stored locally only and are not synced back to Discogs.
- Rating is optional and can be null.
- Notes is a free-text field with no length limit in the UI, though a reasonable database column limit (e.g., 2000 characters) is acceptable.

### Acceptance Criteria
```gherkin
Feature: Listening notes and personal ratings

  Scenario: Viewing notes and rating on the release detail page
    Given I am logged in
    And I navigate to /releases/{id}
    When the detail view renders
    Then a "My Notes" section is visible with a text area for notes
    And a "My Rating" section is visible with a 1-to-5 star selector

  Scenario: Saving a personal note
    Given I am logged in
    And I am viewing /releases/{id}
    When I enter text into the notes field
    And I click the Save button
    Then the note is saved to the database for that release
    And a success message confirms the note was saved

  Scenario: Saving a personal rating
    Given I am logged in
    And I am viewing /releases/{id}
    When I select a star rating
    And I click the Save button
    Then the rating is saved to the database for that release
    And a success message confirms the rating was saved

  Scenario: Viewing a previously saved note and rating
    Given I have previously saved a note and rating for a release
    When I navigate to /releases/{id}
    Then the notes field displays my saved note
    And the star selector displays my saved rating

  Scenario: Clearing a note
    Given I have a saved note for a release
    When I clear the notes field and click Save
    Then the note is removed from the database for that release
    And the notes field appears empty

  Scenario: Clearing a rating
    Given I have a saved rating for a release
    When I deselect all stars (or click a "clear rating" control) and click Save
    Then the rating is removed from the database for that release
    And the star selector shows no rating selected

  Scenario: Notes and ratings are not affected by sync
    Given I have saved a note and rating for a release
    When a manual sync runs
    Then the saved note and rating remain unchanged
    And the synced catalog data from Discogs is updated independently
```

---

## [ABM-024] Duplicate Detection

**Status:** Backlog
**Priority:** Low

### Business Problem
Over time, especially with a large collection, I may accidentally buy a record I already own — or intentionally own multiple copies for different pressings. I want the dashboard to identify potential duplicates (same artist and title but different release IDs) so I can review them. This helps me avoid accidental re-purchases when shopping and lets me see at a glance which records I own in multiple versions.

### Acceptance Criteria
```gherkin
Feature: Duplicate detection

  Scenario: Navigating to the duplicates page
    Given I am logged in
    When I navigate to /duplicates
    Then the duplicates page loads
    And potential duplicates are displayed

  Scenario: Duplicates are identified by matching artist and title
    Given my collection contains multiple releases with the same artist and title but different Discogs release IDs
    When the duplicates page renders
    Then each group of matching releases is displayed together
    And the group shows the shared artist and title
    And each release in the group shows its year, format, and label to help distinguish versions

  Scenario: No duplicates in collection
    Given my collection contains no releases that share both artist and title
    When I navigate to /duplicates
    Then a message is displayed indicating no potential duplicates were found
    And no list or groups are rendered

  Scenario: Clicking a release in a duplicate group navigates to its detail view
    Given the duplicates page is displaying duplicate groups
    When I click on a release within a group
    Then I am navigated to /releases/{id} for that release

  Scenario: Single-copy releases are not shown
    Given my collection contains some releases with unique artist-title combinations
    When the duplicates page renders
    Then only releases that have at least one other release with the same artist and title are displayed
    And unique releases do not appear on the duplicates page
```

---

## [ABM-025] Format Emoji Icons

**Status:** Done
**Priority:** Low

### Business Problem
The plain-text format labels in the collection list and detail view are functional but visually bland. A small emoji alongside each format makes scanning the list faster and more enjoyable. I can spot vinyl versus cassette versus CD at a glance without reading the text, and the visual variety adds personality to the interface.

### Format-to-Emoji Mapping
| Format | Emoji |
|--------|-------|
| Vinyl, LP, 12", 10", 7" | vinyl record |
| Cassette | videocassette |
| CD | optical disc |
| Box Set | package |
| DVD, Blu-ray | optical disc (same as CD) |
| VHS | videocassette (same as Cassette) |
| Digital, File | musical note |
| Unknown / unmapped | musical note (fallback) |

### Acceptance Criteria
```gherkin
Feature: Format emoji icons

  Scenario: Vinyl formats display a vinyl record emoji
    Given I am logged in
    And the collection contains a release with format "Vinyl", "LP", "12\"", "10\"", or "7\""
    When the collection list renders
    Then that release displays a vinyl record emoji alongside the format text

  Scenario: Cassette format displays a videocassette emoji
    Given I am logged in
    And the collection contains a release with format "Cassette"
    When the collection list renders
    Then that release displays a videocassette emoji alongside the format text

  Scenario: CD format displays an optical disc emoji
    Given I am logged in
    And the collection contains a release with format "CD"
    When the collection list renders
    Then that release displays an optical disc emoji alongside the format text

  Scenario: Box Set format displays a package emoji
    Given I am logged in
    And the collection contains a release with format "Box Set"
    When the collection list renders
    Then that release displays a package emoji alongside the format text

  Scenario: DVD and Blu-ray formats display an optical disc emoji
    Given I am logged in
    And the collection contains a release with format "DVD" or "Blu-ray"
    When the collection list renders
    Then that release displays an optical disc emoji alongside the format text

  Scenario: VHS format displays a videocassette emoji
    Given I am logged in
    And the collection contains a release with format "VHS"
    When the collection list renders
    Then that release displays a videocassette emoji alongside the format text

  Scenario: Digital formats display a musical note emoji
    Given I am logged in
    And the collection contains a release with format "File" or other digital-only format
    When the collection list renders
    Then that release displays a musical note emoji alongside the format text

  Scenario: Unknown format displays a fallback emoji
    Given I am logged in
    And the collection contains a release with a format not in the known mapping
    When the collection list renders
    Then that release displays a musical note emoji as a fallback alongside the format text

  Scenario: Detail view also displays the format emoji
    Given I am logged in
    And I navigate to /releases/{id}
    When the detail view renders
    Then the format field displays the appropriate emoji alongside the format text

  Scenario: Emoji does not replace the text label
    Given I am logged in
    When the collection list or detail view renders a format
    Then both the emoji and the plain-text format label are visible
    And the text remains readable for accessibility
```

---

## [ABM-026] GitHub Workflow Audit and Hardening

**Status:** Done
**Priority:** Medium

### Business Problem
The CI pipeline works but has accumulated inefficiencies and gaps that cost time and money on every build. Redundant setup steps across jobs mean I pay for the same checkout, restore, and install work multiple times per workflow run. Missing quality gates — Angular linting, Angular unit tests, and TypeScript static analysis — let preventable defects slip through. Incorrectly pinned action versions may resolve unpredictably, and the lack of test artifacts makes debugging failures harder than it should be. Tightening up the workflows saves runner minutes, catches more bugs earlier, and makes CI failures easier to diagnose.

### Current State
- **ci.yml** has 5 jobs: `dotnet-build-test`, `dotnet-dependency-scan`, `angular-build`, `npm-audit`, `secret-scan`
- **codeql.yml** runs C# static analysis on push, PR, and a weekly schedule
- **dependabot-auto-merge.yml** auto-merges minor/patch Dependabot PRs

### Acceptance Criteria
```gherkin
Feature: GitHub workflow audit and hardening

  # --- Redundancy fixes ---

  Scenario: .NET jobs share a single checkout and restore
    Given the ci.yml workflow is triggered
    When the dotnet-build-test and dotnet-dependency-scan jobs run
    Then checkout and dotnet restore are performed once, not independently in each job
    And both jobs reuse the restored packages via caching or a shared artifact

  Scenario: Angular jobs share a single checkout and npm ci
    Given the ci.yml workflow is triggered
    When the angular-build and npm-audit jobs run
    Then checkout and npm ci are performed once, not independently in each job
    And both jobs reuse the installed node_modules via caching or a shared artifact

  Scenario: NuGet packages are cached between workflow runs
    Given a workflow run completes successfully
    When a subsequent workflow run starts with the same dependency lockfile
    Then NuGet packages are restored from cache
    And dotnet restore completes faster than a cold restore

  Scenario: Dependabot auto-merge workflow only runs for Dependabot PRs
    Given a pull request is opened by a non-Dependabot actor
    When the dependabot-auto-merge workflow is evaluated
    Then no runner is started for that workflow
    And runner minutes are not consumed

  # --- Gap fixes: Angular quality gates ---

  Scenario: Angular linting runs on pull requests
    Given a pull request is opened or updated against main
    When the CI pipeline runs
    Then ng lint is executed against the Angular project
    And linting errors cause the pipeline to fail

  Scenario: Angular unit tests run on pull requests
    Given a pull request is opened or updated against main
    When the CI pipeline runs
    Then ng test is executed with headless Chrome
    And failing tests cause the pipeline to fail

  # --- Gap fixes: CodeQL coverage ---

  Scenario: CodeQL analyzes TypeScript in addition to C#
    Given a pull request is opened or updated against main
    When the CodeQL workflow runs
    Then both csharp and javascript languages are analyzed
    And security findings in Angular TypeScript code are reported

  # --- Gap fixes: Action version pinning ---

  Scenario: All actions are pinned to stable versions
    Given I inspect the workflow YAML files
    When I review the uses: declarations for checkout, setup-node, and setup-dotnet
    Then actions/checkout is pinned to v4 or a specific SHA
    And actions/setup-node is pinned to v4 or a specific SHA
    And actions/setup-dotnet is pinned to a stable version or SHA
    And no action is pinned to a non-existent version such as v6

  # --- Gap fixes: Test artifacts ---

  Scenario: .NET test results are uploaded as artifacts
    Given the dotnet-build-test job runs
    When tests complete (pass or fail)
    Then a test results artifact (TRX or JUnit XML) is uploaded to the workflow run
    And the artifact is downloadable from the GitHub Actions UI

  Scenario: Angular test results are uploaded as artifacts
    Given the Angular unit test job runs
    When tests complete (pass or fail)
    Then a test results artifact (JUnit XML or HTML report) is uploaded to the workflow run
    And the artifact is downloadable from the GitHub Actions UI

  # --- Out of scope ---

  Scenario: CD / deployment workflow is out of scope
    Given this backlog item is focused on CI hardening
    Then no continuous deployment workflow is added as part of this item
    And deployment automation is tracked separately if needed
```

---

## [ABM-027] Code Complexity Audit and Simplification

**Status:** Backlog
**Priority:** Low

### Business Problem
This is a single-user, read-mostly personal dashboard — not enterprise software serving thousands of tenants. Over time, code can accumulate layers of abstraction, defensive logic, and generalizations that made sense speculatively but add no value for the actual use case. Every unnecessary interface, factory, or indirection layer is cognitive overhead when I read the code, fix bugs, or add features. A deliberate simplification pass removes that cruft, making the codebase easier to understand, faster to modify, and more pleasant to work in.

### Scope
- **Backend:** ASP.NET Core 10 Web API (`src/AllByMyshelf.Api`)
- **Frontend:** Angular 21 standalone components (`src/AllByMyshelf.Web`)
- **Tests:** Unit and integration tests (`tests/`)

### What to Look For
- Interfaces with only one implementation that exist "for testability" but are never mocked
- Factory or builder patterns where direct instantiation would suffice
- Generic abstractions parameterized on a single concrete type
- Dead code: unused methods, unreachable branches, commented-out blocks
- Overly defensive validation for inputs that are already validated upstream or cannot occur in a single-user context
- Premature pagination or batching logic for data sets that will never exceed a few hundred rows
- Layers of indirection (service calls service calls repository) where a simpler call chain would be clearer
- Alphabetical ordering violations (per project convention)

### Acceptance Criteria
```gherkin
Feature: Code complexity audit and simplification

  # --- Audit phase ---

  Scenario: Backend audit identifies unnecessary abstractions
    Given I review the ASP.NET Core codebase
    When I find an interface with exactly one implementation that is never substituted in tests
    Then that interface is flagged for removal
    And the concrete class is used directly

  Scenario: Backend audit identifies dead code
    Given I review the ASP.NET Core codebase
    When I find methods, classes, or branches that are never called or unreachable
    Then that dead code is flagged for removal

  Scenario: Backend audit identifies over-defensive validation
    Given I review the ASP.NET Core codebase
    When I find null checks, permission checks, or input validation for scenarios that cannot occur in a single-user read-mostly context
    Then that validation is flagged for simplification or removal

  Scenario: Frontend audit identifies unnecessary abstractions
    Given I review the Angular codebase
    When I find services, utilities, or indirection layers that add no value over direct implementation
    Then those abstractions are flagged for removal or inlining

  Scenario: Frontend audit identifies dead code
    Given I review the Angular codebase
    When I find components, methods, or imports that are never used
    Then that dead code is flagged for removal

  Scenario: Alphabetical ordering violations are identified
    Given I review both the backend and frontend codebases
    When I find classes where fields, properties, methods, or variables are not in alphabetical order
    Then those classes are flagged for reordering

  # --- Simplification phase ---

  Scenario: Unnecessary interfaces are removed from the backend
    Given an interface has been flagged as unnecessary
    When the simplification is applied
    Then the interface is deleted
    And all references are updated to use the concrete class directly
    And all existing tests continue to pass

  Scenario: Dead code is removed from the backend
    Given dead code has been flagged in the backend
    When the simplification is applied
    Then the dead code is deleted
    And the solution builds without errors
    And all existing tests continue to pass

  Scenario: Over-defensive validation is simplified in the backend
    Given validation has been flagged as over-defensive
    When the simplification is applied
    Then the unnecessary checks are removed
    And the API continues to function correctly for the single-user use case

  Scenario: Unnecessary abstractions are removed from the frontend
    Given an abstraction has been flagged as unnecessary in the frontend
    When the simplification is applied
    Then the abstraction is inlined or deleted
    And the Angular application builds without errors

  Scenario: Dead code is removed from the frontend
    Given dead code has been flagged in the frontend
    When the simplification is applied
    Then the dead code is deleted
    And the Angular application builds without errors

  Scenario: Alphabetical ordering is corrected
    Given classes have been flagged for ordering violations
    When the simplification is applied
    Then all fields, properties, methods, and variables are reordered alphabetically
    And the application builds and all tests pass

  # --- Verification ---

  Scenario: All existing functionality is preserved
    Given the audit and simplification work is complete
    When I run the full test suite (backend and frontend)
    Then all tests pass
    And no regressions are introduced

  Scenario: Codebase is measurably simpler
    Given the audit and simplification work is complete
    When I compare the before and after states
    Then the total line count is reduced or unchanged
    And the number of files, classes, or interfaces is reduced or unchanged
    And no new abstractions have been introduced
```

---

## [ABM-028] Enforce Authentication on All API Endpoints

**Status:** Done
**Priority:** High

### Business Problem
My collection data is personal and should never be exposed to unauthenticated callers. Although Auth0 authentication exists on the frontend, I need confidence that the backend API consistently enforces authorization on every endpoint. If any endpoint allows unauthenticated requests to reach business logic, my data could be accessed by anyone who discovers the API URL. A global authentication requirement ensures that only requests with a valid token are processed, protecting my collection information from unauthorized access.

### Acceptance Criteria
```gherkin
Feature: API authentication enforcement

  # --- Authenticated requests succeed ---

  Scenario: Authenticated request to a protected endpoint succeeds
    Given I have a valid Auth0 access token
    When I make a GET request to "/api/v1/collection" with the Authorization header set to "Bearer <valid_token>"
    Then the response status code is 200
    And the response body contains collection data

  Scenario: Authenticated request with valid token to any endpoint succeeds
    Given I have a valid Auth0 access token
    When I make a request to any API endpoint under "/api/v1/" with the Authorization header set to "Bearer <valid_token>"
    Then the request is processed by the endpoint's business logic
    And the response reflects the expected behavior for that endpoint

  # --- Unauthenticated requests are rejected ---

  Scenario: Request without Authorization header is rejected
    Given I do not include an Authorization header
    When I make a GET request to "/api/v1/collection"
    Then the response status code is 401 Unauthorized
    And the response body does not contain any collection data

  Scenario: Request with missing Bearer token is rejected
    Given I include an Authorization header with value ""
    When I make a GET request to "/api/v1/collection"
    Then the response status code is 401 Unauthorized

  Scenario: Request with malformed Authorization header is rejected
    Given I include an Authorization header with value "InvalidScheme abc123"
    When I make a GET request to "/api/v1/collection"
    Then the response status code is 401 Unauthorized

  Scenario: Request with invalid token is rejected
    Given I include an Authorization header with value "Bearer invalid_or_expired_token"
    When I make a GET request to "/api/v1/collection"
    Then the response status code is 401 Unauthorized
    And the response body does not contain any collection data

  Scenario: Request with expired token is rejected
    Given I have an Auth0 access token that has expired
    When I make a GET request to "/api/v1/collection" with the Authorization header set to "Bearer <expired_token>"
    Then the response status code is 401 Unauthorized

  # --- All endpoints are covered ---

  Scenario: All API endpoints require authentication by default
    Given the ASP.NET Core API is configured
    When I review the authentication middleware and controller configurations
    Then authentication is required globally for all endpoints under "/api/v1/"
    And no endpoint is accidentally left open via [AllowAnonymous] or missing [Authorize] attributes

  Scenario: No business logic executes for unauthenticated requests
    Given I make an unauthenticated request to any API endpoint
    When the request is processed by the middleware pipeline
    Then the request is rejected before reaching any controller action
    And no database queries or external API calls are made

  # --- Health check exception (if applicable) ---

  Scenario: Health check endpoint remains accessible without authentication
    Given the application exposes a health check endpoint at "/health" or "/healthz"
    When I make a GET request to the health check endpoint without an Authorization header
    Then the response status code is 200
    And the response indicates the application health status
    And no sensitive data is exposed

  # --- Error response format ---

  Scenario: Unauthorized response includes appropriate headers
    Given I make an unauthenticated request to a protected endpoint
    When the response is returned with status 401
    Then the response includes a "WWW-Authenticate" header indicating the expected authentication scheme
    And the response body is either empty or contains a generic error message without sensitive details
```

---

## [ABM-029] Collection Maintenance View

**Status:** Done
**Priority:** Medium

### Business Problem
I have approximately 150 records in my collection, and some of them have gaps in their data such as missing release year, pricing information, genre, or cover art. Without a quick way to identify which records are incomplete, I have to scroll through my entire collection manually to find them. A dedicated maintenance view saves time by surfacing all records with missing data in one place, and provides direct links to Discogs so I can fix the gaps at the source.

### Acceptance Criteria
```gherkin
Feature: Collection maintenance view

  # --- Navigation ---

  Scenario: Toolbar button navigates to maintenance view
    Given I am on the collection page
    When I click the "Maintenance" toolbar button
    Then I am navigated to the "/maintenance" route
    And the maintenance view is displayed

  # --- Listing records with missing data ---

  Scenario: Maintenance view lists records with missing year
    Given my collection contains a record with a missing release year
    When I view the maintenance page
    Then the record appears in the maintenance list
    And the "Year" field is shown as missing for that record

  Scenario: Maintenance view lists records with missing pricing data
    Given my collection contains a record with missing lowest price
    When I view the maintenance page
    Then the record appears in the maintenance list
    And the "Lowest Price" field is shown as missing for that record

  Scenario: Maintenance view lists records with missing median price
    Given my collection contains a record with missing median price
    When I view the maintenance page
    Then the record appears in the maintenance list
    And the "Median Price" field is shown as missing for that record

  Scenario: Maintenance view lists records with missing highest price
    Given my collection contains a record with missing highest price
    When I view the maintenance page
    Then the record appears in the maintenance list
    And the "Highest Price" field is shown as missing for that record

  Scenario: Maintenance view lists records with missing genre
    Given my collection contains a record with a missing genre
    When I view the maintenance page
    Then the record appears in the maintenance list
    And the "Genre" field is shown as missing for that record

  Scenario: Maintenance view lists records with missing cover image
    Given my collection contains a record with a missing cover image
    When I view the maintenance page
    Then the record appears in the maintenance list
    And the "Cover Image" field is shown as missing for that record

  Scenario: Records with multiple missing fields appear once with all gaps listed
    Given my collection contains a record missing both year and genre
    When I view the maintenance page
    Then the record appears once in the maintenance list
    And both "Year" and "Genre" are shown as missing for that record

  Scenario: Records with complete data do not appear in the maintenance list
    Given my collection contains a record with all fields populated (year, genre, all price fields, cover image)
    When I view the maintenance page
    Then that record does not appear in the maintenance list

  # --- Row information ---

  Scenario: Each row displays record name and artist
    Given my collection contains a record with missing data
    When I view the maintenance page
    Then the row displays the record title
    And the row displays the artist name

  Scenario: Each row shows which specific fields are missing
    Given my collection contains a record with missing year and missing cover image
    When I view the maintenance page
    Then the row clearly indicates "Year" and "Cover Image" as the missing fields

  # --- Discogs links ---

  Scenario: Each row links to the Discogs release page
    Given my collection contains a record with Discogs ID "12345" and missing data
    When I view the maintenance page
    Then the row contains a link to "https://www.discogs.com/release/12345"
    And clicking the link opens the Discogs release page in a new tab

  Scenario: Each row links to the user's Discogs collection entry
    Given my collection contains a record with Discogs ID "12345" and instance ID "67890" and missing data
    When I view the maintenance page
    Then the row contains a link to edit the collection entry on Discogs
    And clicking the link opens the Discogs collection entry page in a new tab

  # --- Empty state ---

  Scenario: Empty state when all records have complete data
    Given all records in my collection have year, genre, all price fields, and cover image populated
    When I view the maintenance page
    Then the maintenance list is empty
    And an empty state message is displayed indicating "No records with missing data"

  # --- Loading state ---

  Scenario: Loading indicator while fetching maintenance data
    Given I navigate to the maintenance page
    When the page is loading data from the API
    Then a loading indicator is displayed
    And the maintenance list is not yet visible

  Scenario: Loading indicator disappears when data loads successfully
    Given the maintenance page is loading
    When the API returns the list of records with missing data
    Then the loading indicator is hidden
    And the maintenance list is displayed

  # --- Error state ---

  Scenario: Error state when API request fails
    Given I navigate to the maintenance page
    When the API request fails with an error
    Then an error message is displayed to the user
    And the error message suggests trying again later

  Scenario: Error state does not show maintenance list
    Given the maintenance page encountered an API error
    When I view the maintenance page
    Then the maintenance list is not displayed
    And only the error message is visible
```

---

## [ABM-030] Store Hardcover API Token

**Status:** Done
**Priority:** High

### Business Problem
Before the application can communicate with the Hardcover GraphQL API, it needs a way to securely hold my API token. Storing it in user-secrets keeps it off disk and out of source control so the credential is never accidentally exposed. The application should fail fast on startup if the token is missing, making configuration errors immediately obvious rather than surfacing later at runtime.

### Acceptance Criteria
```gherkin
Feature: Hardcover API token configuration

  Scenario: Application starts with a valid token configured
    Given the Hardcover API token has been set in user-secrets under the key "Hardcover:ApiToken"
    When the application starts
    Then the application reads the token without error
    And the token is available to the Hardcover GraphQL client

  Scenario: Application starts without a token configured
    Given the Hardcover API token has NOT been set in user-secrets
    When the application starts
    Then the application fails to start
    And a clear error message is logged indicating "Hardcover:ApiToken" is missing from configuration
    And the error message includes instructions on how to configure the token using dotnet user-secrets

  Scenario: Application starts with an empty token value
    Given the Hardcover API token has been set in user-secrets with an empty string value
    When the application starts
    Then the application fails to start
    And a clear error message is logged indicating the token value cannot be empty

  Scenario: Token is used as Bearer authentication
    Given the Hardcover API token is configured
    When the application makes a request to the Hardcover GraphQL API
    Then the request includes an Authorization header with value "Bearer <token>"
```

---

## [ABM-031] Sync Read Books from Hardcover

**Status:** Done
**Priority:** High

### Business Problem
I want to import my read books from Hardcover into the local database so the application has a local copy to serve from. Hardcover tracks books with different statuses (want to read, currently reading, read, did not finish), but I only care about books I have finished reading (status_id: 3). The sync must run as a background process so the HTTP response that triggered it returns immediately. With a rate limit of 60 requests per minute, standard retry logic is sufficient without special backoff handling.

### Acceptance Criteria
```gherkin
Feature: Background sync of read books from Hardcover

  # --- Triggering the sync ---

  Scenario: Sync is triggered and runs in the background
    Given the Hardcover API token is configured
    And no Hardcover sync is currently running
    When I trigger a manual Hardcover sync via POST /api/v1/books/sync
    Then the API responds immediately with HTTP 202 Accepted
    And the sync runs asynchronously in the background

  Scenario: Sync is already in progress
    Given a Hardcover sync is currently running
    When I trigger another manual Hardcover sync via POST /api/v1/books/sync
    Then the API responds with HTTP 409 Conflict
    And no second sync process is started

  Scenario: Attempt to trigger sync with no token configured
    Given the Hardcover API token is NOT configured
    When I send POST /api/v1/books/sync
    Then the response is HTTP 503 Service Unavailable
    And the response body explains that the Hardcover token is not configured

  # --- GraphQL query ---

  Scenario: Sync fetches only read books from Hardcover
    Given the Hardcover API token is configured
    When a sync runs
    Then the sync queries the Hardcover GraphQL API for "me { user_books }"
    And only books with status_id equal to 3 (Read) are retrieved
    And books with other status values are ignored

  # --- Data extraction ---

  Scenario: Sync extracts book title
    Given the Hardcover API returns a read book
    When the sync processes the book
    Then the book title is extracted from the response

  Scenario: Sync extracts author name
    Given the Hardcover API returns a read book with contributions
    When the sync processes the book
    Then the author is extracted from the first contribution's name field

  Scenario: Sync handles book with no contributions
    Given the Hardcover API returns a read book with an empty contributions array
    When the sync processes the book
    Then the author is stored as null or empty
    And the sync does not fail

  Scenario: Sync extracts publication year
    Given the Hardcover API returns a read book with a release_date
    When the sync processes the book
    Then the year is extracted from the release_date field

  Scenario: Sync handles book with no release date
    Given the Hardcover API returns a read book with a null release_date
    When the sync processes the book
    Then the year is stored as null
    And the sync does not fail

  Scenario: Sync extracts genre from tags
    Given the Hardcover API returns a read book with cached_tags containing genre tags
    When the sync processes the book
    Then the genre is extracted from the first tag in cached_tags

  Scenario: Sync handles book with no tags
    Given the Hardcover API returns a read book with an empty or null cached_tags array
    When the sync processes the book
    Then the genre is stored as null
    And the sync does not fail

  Scenario: Sync extracts cover image URL
    Given the Hardcover API returns a read book with an image object
    When the sync processes the book
    Then the cover image URL is extracted from image.url

  Scenario: Sync handles book with no cover image
    Given the Hardcover API returns a read book with a null image object
    When the sync processes the book
    Then the cover image URL is stored as null
    And the sync does not fail

  Scenario: Sync stores Hardcover book ID
    Given the Hardcover API returns a read book
    When the sync processes the book
    Then the Hardcover book ID is stored for future reference and deduplication

  # --- Persistence ---

  Scenario: New books are inserted on first sync
    Given the local database contains no books
    When a Hardcover sync completes successfully
    Then all read books retrieved from Hardcover are saved to the database

  Scenario: Existing books are updated on subsequent sync
    Given the local database already contains books from a previous Hardcover sync
    When a sync completes successfully
    Then books that still exist in Hardcover with status "Read" are updated with current data
    And books that no longer exist or are no longer marked as "Read" are removed from the database
    And no duplicate book records are created

  Scenario: Sync failure does not corrupt existing data
    Given the local database contains books from a previous Hardcover sync
    When a sync fails partway through
    Then the previously stored books remain intact in the database

  # --- Rate limiting ---

  Scenario: Sync respects Hardcover rate limits
    Given the Hardcover API rate limit is 60 requests per minute
    When a sync runs
    Then the sync does not exceed 60 requests per minute
    And standard HTTP retry logic handles any 429 responses
```

---

## [ABM-032] Expose Paginated Books Endpoint

**Status:** Done
**Priority:** High

### Business Problem
I need an API endpoint that returns my locally stored book collection so the frontend can display it as a paginated list. Serving from the local database keeps responses fast regardless of Hardcover API availability. Filters for author, title, year, and genre help me quickly find specific books.

### Acceptance Criteria
```gherkin
Feature: Paginated books endpoint

  # --- Basic pagination ---

  Scenario: Retrieve the first page of books
    Given the database contains books
    When I request GET /api/v1/books?page=1&pageSize=25
    Then the response is HTTP 200 OK
    And the response body contains up to 25 books
    And each book includes title, author, year, genre, and cover image URL
    And the response includes total record count and total page count

  Scenario: Retrieve a subsequent page
    Given the database contains more than 25 books
    When I request GET /api/v1/books?page=2&pageSize=25
    Then the response is HTTP 200 OK
    And the response body contains books from the second page
    And the books on page 2 do not overlap with those on page 1

  Scenario: Request a page beyond the available data
    Given the database contains 30 books
    When I request GET /api/v1/books?page=5&pageSize=25
    Then the response is HTTP 200 OK
    And the response body contains an empty books array
    And the total record count still reflects 30

  Scenario: Database contains no books
    Given no Hardcover sync has been run and the database contains no books
    When I request GET /api/v1/books?page=1&pageSize=25
    Then the response is HTTP 200 OK
    And the response body contains an empty books array
    And the total record count is 0

  # --- Filtering by title ---

  Scenario: Filter books by title (partial match)
    Given the database contains books with titles "The Great Gatsby", "Great Expectations", and "Moby Dick"
    When I request GET /api/v1/books?title=great
    Then the response contains "The Great Gatsby" and "Great Expectations"
    And the response does not contain "Moby Dick"

  Scenario: Filter by title is case-insensitive
    Given the database contains a book with title "The Great Gatsby"
    When I request GET /api/v1/books?title=GREAT
    Then the response contains "The Great Gatsby"

  # --- Filtering by author ---

  Scenario: Filter books by author (partial match)
    Given the database contains books by "F. Scott Fitzgerald", "Charles Dickens", and "Herman Melville"
    When I request GET /api/v1/books?author=fitzgerald
    Then the response contains only books by "F. Scott Fitzgerald"

  Scenario: Filter by author is case-insensitive
    Given the database contains a book by "F. Scott Fitzgerald"
    When I request GET /api/v1/books?author=FITZGERALD
    Then the response contains books by "F. Scott Fitzgerald"

  # --- Filtering by year ---

  Scenario: Filter books by exact year
    Given the database contains books from years 1925, 1851, and 1860
    When I request GET /api/v1/books?year=1925
    Then the response contains only books from 1925

  Scenario: Filter by year with no matches
    Given the database contains books from years 1925, 1851, and 1860
    When I request GET /api/v1/books?year=2000
    Then the response contains an empty books array

  # --- Filtering by genre ---

  Scenario: Filter books by genre (partial match)
    Given the database contains books with genres "Fiction", "Science Fiction", and "Biography"
    When I request GET /api/v1/books?genre=fiction
    Then the response contains books with genres "Fiction" and "Science Fiction"
    And the response does not contain books with genre "Biography"

  Scenario: Filter by genre is case-insensitive
    Given the database contains a book with genre "Science Fiction"
    When I request GET /api/v1/books?genre=SCIENCE
    Then the response contains books with genre "Science Fiction"

  # --- Combined filters ---

  Scenario: Multiple filters are combined with AND logic
    Given the database contains books with various titles, authors, years, and genres
    When I request GET /api/v1/books?author=fitzgerald&year=1925
    Then the response contains only books by Fitzgerald published in 1925
    And books by Fitzgerald from other years are excluded
    And books from 1925 by other authors are excluded

  Scenario: Filters combined with pagination
    Given the database contains 50 books matching a filter criteria
    When I request GET /api/v1/books?genre=fiction&page=2&pageSize=25
    Then the response contains the second page of fiction books
    And the total count reflects only the filtered results

  # --- Default sorting ---

  Scenario: Books are sorted by title by default
    Given the database contains books with titles "Zebra", "Apple", and "Mango"
    When I request GET /api/v1/books
    Then the books are returned in alphabetical order by title
```

---

## [ABM-033] Books Dashboard

**Status:** Done
**Priority:** High

### Business Problem
I need a browser-based interface to see my read book collection from Hardcover. Without a frontend, the book data has no practical value day-to-day. The dashboard must require me to be logged in, show my books in a data table with title, author, year, genre, and cover thumbnail, and let me trigger a fresh sync without opening a separate HTTP client.

### Acceptance Criteria
```gherkin
Feature: Books dashboard

  # --- Authentication ---

  Scenario: Unauthenticated user is redirected to login
    Given I am not logged in
    When I navigate to /books
    Then I am redirected to the Auth0 login page
    And I cannot see any book data

  # --- Data table display ---

  Scenario: Authenticated user sees the books data table
    Given I am logged in
    And the API returns a non-empty page of books
    When the books dashboard loads
    Then I see a data table of books
    And each row displays the title, author, year, genre, and cover thumbnail
    And pagination controls are visible

  Scenario: Cover thumbnail displays book cover image
    Given I am logged in
    And a book has a cover image URL
    When the books dashboard loads
    Then the cover thumbnail displays the image from the cover URL
    And the thumbnail is appropriately sized for a table row

  Scenario: Cover thumbnail handles missing cover image
    Given I am logged in
    And a book has no cover image URL
    When the books dashboard loads
    Then a placeholder image or icon is displayed for that book
    And the table layout remains consistent

  Scenario: Navigating to another page of results
    Given I am logged in
    And the book collection contains more books than fit on one page
    When I click to go to the next page
    Then the table updates to show the next page of books
    And the pagination controls reflect the new current page

  # --- Empty state ---

  Scenario: Book collection is empty
    Given I am logged in
    And the API returns zero books
    When the books dashboard loads
    Then I see a message indicating the book collection is empty
    And no table rows are rendered
    And the Sync Books button is prominently displayed

  # --- Loading state ---

  Scenario: Dashboard shows a loading indicator while fetching data
    Given I am logged in
    When the dashboard is waiting for the API to respond
    Then a loading indicator is visible
    And the table area is not shown until data has loaded

  # --- Sync button ---

  Scenario: Triggering a manual sync successfully
    Given I am logged in
    When I click the "Sync Books" button
    And the API responds with HTTP 202 Accepted
    Then the Sync Books button is disabled for the duration of the operation
    And I see a success notification confirming the sync has started

  Scenario: Triggering a sync while one is already running
    Given I am logged in
    When I click the "Sync Books" button
    And the API responds with HTTP 409 Conflict
    Then I see a notification informing me a sync is already in progress

  Scenario: Triggering a sync when the token is not configured
    Given I am logged in
    When I click the "Sync Books" button
    And the API responds with HTTP 503 Service Unavailable
    Then I see a notification informing me the Hardcover token is not configured

  # --- Navigation ---

  Scenario: Books page is accessible from main navigation
    Given I am logged in
    And I am on any page in the application
    When I look at the main navigation
    Then I see a link or menu item for "Books"
    And clicking it navigates me to /books

  # --- Error handling ---

  Scenario: Error loading books displays error message
    Given I am logged in
    When the API request to fetch books fails
    Then an error message is displayed
    And the error message suggests trying again later
    And a retry option is available
```

---

## [ABM-034] Unified Statistics Dashboard for Records and Books

**Status:** Backlog
**Priority:** Medium

### Business Problem
The current statistics page only displays collection value for music records. Now that the dashboard supports both records (from Discogs) and books (from Hardcover), I want a unified statistics view that provides meaningful insights across both collections. This helps me understand my collecting habits, see trends over time, and get a high-level summary of what I own without scrolling through individual items.

### Data Notes
- Records: Collection value (sum of LowestPrice) is already implemented via ABM-020. This feature enhances it with additional breakdowns.
- Books: Reading pace is calculated from read dates stored during Hardcover sync. If read dates are unavailable for some books, those books are excluded from pace calculations but still counted in totals.
- All statistics are read-only aggregations of existing data. No new external API calls are required.

### Acceptance Criteria
```gherkin
Feature: Unified statistics dashboard

  # --- Page structure ---

  Scenario: Statistics page displays sections for both collections
    Given I am logged in
    When I navigate to /statistics
    Then I see a "Records" statistics section
    And I see a "Books" statistics section
    And both sections are visible on the same page

  # --- Records statistics ---

  Scenario: Records section displays total count
    Given I am logged in
    And my records collection contains items
    When I view the records statistics section
    Then I see the total number of records in my collection

  Scenario: Records section displays collection value
    Given I am logged in
    And my records collection contains items with pricing data
    When I view the records statistics section
    Then I see the total collection value (sum of lowest prices)
    And the value is displayed in USD format
    And a note indicates how many records lack pricing data

  Scenario: Records section displays format breakdown
    Given I am logged in
    And my records collection contains items with various formats
    When I view the records statistics section
    Then I see a breakdown of records by format (e.g., LP, CD, 7")
    And each format shows the count of records

  Scenario: Records section displays genre breakdown
    Given I am logged in
    And my records collection contains items with genre data
    When I view the records statistics section
    Then I see a breakdown of records by genre
    And each genre shows the count of records

  Scenario: Records section displays decade breakdown
    Given I am logged in
    And my records collection contains items with release year data
    When I view the records statistics section
    Then I see a breakdown of records by decade (e.g., 1970s, 1980s, 1990s)
    And each decade shows the count of records

  # --- Books statistics ---

  Scenario: Books section displays total read count
    Given I am logged in
    And my books collection contains items
    When I view the books statistics section
    Then I see the total number of books I have read

  Scenario: Books section displays genre breakdown
    Given I am logged in
    And my books collection contains items with genre data
    When I view the books statistics section
    Then I see a breakdown of books by genre
    And each genre shows the count of books

  Scenario: Books section displays year breakdown
    Given I am logged in
    And my books collection contains items with read dates
    When I view the books statistics section
    Then I see a breakdown of books read by year
    And each year shows the count of books read that year

  Scenario: Books section displays reading pace
    Given I am logged in
    And my books collection contains items with read dates spanning multiple years
    When I view the books statistics section
    Then I see a reading pace metric (average books per year)
    And the pace is calculated from the earliest to latest read date

  Scenario: Reading pace handles missing read dates
    Given I am logged in
    And some books in my collection have no read date
    When I view the books statistics section
    Then the reading pace is calculated using only books with read dates
    And a note indicates how many books were excluded from the pace calculation

  # --- Empty states ---

  Scenario: Records section handles empty collection
    Given I am logged in
    And my records collection is empty
    When I view the records statistics section
    Then I see a message indicating no records are available
    And no statistics are displayed for records

  Scenario: Books section handles empty collection
    Given I am logged in
    And my books collection is empty
    When I view the books statistics section
    Then I see a message indicating no books are available
    And no statistics are displayed for books

  # --- Loading and error states ---

  Scenario: Statistics page shows loading indicator
    Given I am logged in
    When the statistics page is fetching data
    Then a loading indicator is visible
    And the statistics sections are not shown until data has loaded

  Scenario: Statistics page handles API error
    Given I am logged in
    When the API request to fetch statistics fails
    Then an error message is displayed
    And a retry option is available
```

---

## [ABM-035] Bug: Books Dashboard Shows "—" for Genre on All Books

**Status:** Done
**Priority:** Medium

### Business Problem
The Books dashboard displays "—" for the genre column on every book, even though genres exist in Hardcover. This makes it impossible to filter or browse my collection by genre. The `Book` entity already has a `Genre` column, but it is never populated during sync.

### Root Cause
The Hardcover API returns `cached_tags` as a `json!` blob (not a string array). Rather than parsing this structure, the sync service was modified to skip the field entirely, hardcoding `Genre = null` in `BooksSyncService`.

### Fix Required
1. Query the Hardcover API to determine the actual structure of `cached_tags` (likely `{"Genre": ["Fiction"], "Mood": [...]}` or similar)
2. Update `BooksSyncService` to parse the first genre value from `cached_tags` during sync
3. Re-sync books to populate the `Genre` column

### Acceptance Criteria
```gherkin
Feature: Books display genre from Hardcover

  Scenario: Book with genre in Hardcover shows genre in dashboard
    Given a book in Hardcover has cached_tags containing a genre
    When the book is synced to the local database
    Then the book's Genre column is populated with the first genre value
    And the Books dashboard displays that genre instead of "—"

  Scenario: Book without genre in Hardcover shows placeholder
    Given a book in Hardcover has no genre in cached_tags
    When the book is synced to the local database
    Then the book's Genre column remains null
    And the Books dashboard displays "—" for that book
```

---

## [ABM-036] Make External API Integrations Optional

**Status:** Done
**Priority:** Medium

### Business Problem
I want to run All By Myshelf with any combination of external integrations configured — Discogs only, Hardcover only, both, or neither — without the application failing to start. Currently, if either `Discogs:PersonalAccessToken` or `Hardcover:ApiToken` is missing from user-secrets, the application fails on startup due to `ValidateOnStart`. This makes it impossible to use the app for only one collection type or to demo the UI without any API credentials.

Future integrations (e.g., other book or music APIs) should follow the same pattern: optional configuration with graceful degradation.

### Implementation Notes
- Remove or make conditional the `ValidateOnStart` behavior on `HardcoverOptions` and `DiscogsOptions`
- The sync endpoints already return `TokenNotConfigured` (503) when credentials are missing; startup validation is the only blocker
- Expose a `GET /api/v1/config/features` endpoint returning which integrations are active (have valid credentials)
- Frontend should conditionally show/hide navigation items and dashboard sections based on the features endpoint
- Unconfigured sections should either be hidden entirely or display a "not configured" message — never crash or show raw errors

### Acceptance Criteria
```gherkin
Feature: Optional external API integrations

  # --- Startup behavior ---

  Scenario: Application starts with no integrations configured
    Given neither Discogs nor Hardcover credentials are in user-secrets
    When the application starts
    Then the application starts successfully without errors
    And no sync operations are attempted

  Scenario: Application starts with only Discogs configured
    Given the Discogs personal access token is in user-secrets
    And the Hardcover API token is NOT in user-secrets
    When the application starts
    Then the application starts successfully without errors
    And the Discogs integration is available
    And the Hardcover integration is unavailable

  Scenario: Application starts with only Hardcover configured
    Given the Hardcover API token is in user-secrets
    And the Discogs personal access token is NOT in user-secrets
    When the application starts
    Then the application starts successfully without errors
    And the Hardcover integration is available
    And the Discogs integration is unavailable

  Scenario: Application starts with both integrations configured
    Given both Discogs and Hardcover credentials are in user-secrets
    When the application starts
    Then the application starts successfully without errors
    And both integrations are available

  # --- Features endpoint ---

  Scenario: Features endpoint returns active integrations
    Given the Discogs integration is configured
    And the Hardcover integration is NOT configured
    When I call GET /api/v1/config/features
    Then the response includes discogs as active
    And the response includes hardcover as inactive

  # --- Frontend behavior ---

  Scenario: Navigation hides unconfigured integrations
    Given the Discogs integration is configured
    And the Hardcover integration is NOT configured
    When I view the application
    Then the Records navigation item is visible
    And the Books navigation item is hidden or disabled

  Scenario: Dashboard shows only configured sections
    Given the Discogs integration is configured
    And the Hardcover integration is NOT configured
    When I navigate to the dashboard
    Then the Records section is displayed
    And the Books section is either hidden or shows "Not configured"

  # --- Sync endpoints ---

  Scenario: Sync endpoint for unconfigured integration returns 503
    Given the Hardcover integration is NOT configured
    When I call POST /api/v1/books/sync
    Then the response status is 503 Service Unavailable
    And the response body indicates the integration is not configured
```

---

## [ABM-037] Global Sync Progress Indicator

**Status:** Done
**Priority:** Medium

### Business Problem
When I trigger a sync on the Records or Books page, I am stuck on that page until the sync completes because navigating away would lose visibility into the sync progress. I want to be able to navigate freely throughout the application while a sync runs in the background, with the progress indicator visible regardless of which page I am on. This will let me continue browsing my collection or checking statistics without losing track of ongoing sync operations.

### Acceptance Criteria
```gherkin
Feature: Global sync progress indicator

  Scenario: Sync progress remains visible after navigating away
    Given I am on the Records page
    And I trigger a sync
    And the sync is in progress
    When I navigate to the Books page
    Then the sync progress indicator is still visible
    And the progress continues to update

  Scenario: Sync progress is visible on any page
    Given a Records sync is running in the background
    When I navigate to the Statistics page
    Then the sync progress indicator is visible
    And I can see the current sync status

  Scenario: Multiple syncs show combined progress
    Given a Records sync is running in the background
    And I trigger a Books sync
    When I navigate to any page
    Then the progress indicator shows both sync operations
    And each sync displays its individual progress

  Scenario: Sync completes while on a different page
    Given I am on the Records page
    And I trigger a sync
    And I navigate to the Books page
    When the sync completes
    Then I receive a notification that the sync completed
    And the progress indicator is dismissed

  Scenario: Sync fails while on a different page
    Given I am on the Records page
    And I trigger a sync
    And I navigate to the Statistics page
    When the sync fails
    Then I receive a notification that the sync failed
    And the error message is visible

  Scenario: Progress indicator does not interfere with navigation
    Given a sync is running in the background
    When I use the navigation menu
    Then navigation works normally
    And the progress indicator remains visible but unobtrusive
```

---

## [ABM-038] BoardGameGeek Collection Integration

**Status:** Backlog
**Priority:** Medium

### Business Problem
I want to see my board game collection from BoardGameGeek alongside my records (Discogs) and books (Hardcover) in a single unified dashboard. BoardGameGeek is the primary platform I use to track my board games, and having to visit a separate site to view that collection breaks the "all by myshelf" experience. By integrating with the BGG XML API, I can have a complete view of all my collections in one place.

### Acceptance Criteria
```gherkin
Feature: BoardGameGeek collection integration

  Scenario: Configure BGG username
    Given I have a BoardGameGeek account with username "myusername"
    When I configure my BGG username in the application settings
    Then the application stores the username securely
    And the username is available to the BGG sync service

  Scenario: Sync board game collection from BoardGameGeek
    Given my BGG username has been configured
    And my BGG collection contains board games
    When I trigger a BGG collection sync
    Then the sync runs in the background
    And I can see sync progress
    And board games are imported into the local database

  Scenario: View board games on the dashboard
    Given my BGG collection has been synced
    When I navigate to the Board Games page
    Then I see a paginated list of my board games
    And each game displays its name, year published, and thumbnail image
    And I can sort and filter the list

  Scenario: Board game detail view
    Given my BGG collection has been synced
    When I click on a board game in the list
    Then I see the detail view for that game
    And I see game information including player count, play time, and complexity rating

  Scenario: BGG API is unavailable
    Given my BGG username has been configured
    When I trigger a sync
    And the BGG API is unavailable
    Then the sync fails gracefully
    And I see an error message indicating the API is unavailable
    And my existing local data is preserved

  Scenario: BGG username is not configured
    Given my BGG username has NOT been configured
    When I navigate to the Board Games page
    Then I see a message indicating that BGG integration needs to be configured
    And I am prompted to enter my BGG username
```

---

## [ABM-039] Configuration & Settings Page

**Status:** Backlog
**Priority:** High

### Business Problem
External API tokens (Discogs PAT, Discogs username, Hardcover API token, and future integrations like BoardGameGeek) are currently stored via `dotnet user-secrets`, which requires CLI access and makes the app harder to set up. Moving these to a database-backed configuration page lets me manage tokens through the UI. All sensitive values must be encrypted at rest using AES-256-GCM with a master key sourced from an environment variable (or AWS Secrets Manager in the future). Auth0 settings remain in dotnet user-secrets since they are required for the app to start. Additionally, the app currently follows the OS color scheme preference with no user override. A dark mode setting (Light / Dark / OS default) should be part of this configuration area.

### Acceptance Criteria
```gherkin
Feature: Configuration & settings page

  Scenario: User navigates to the settings page
    Given the user is authenticated
    When they navigate to the Settings page
    Then they see fields for each external API token (Discogs PAT, Discogs Username, Hardcover API Token)
    And they see a theme selector with options: Light, Dark, OS Default
    And the current values are pre-populated (tokens masked)

  Scenario: User saves an API token
    Given the user enters a new Discogs personal access token
    When they click Save
    Then the token is encrypted with AES-256-GCM before being stored in the database
    And the encryption master key is sourced from an environment variable
    And a success confirmation is shown

  Scenario: User updates the theme preference
    Given the user selects "Dark" from the theme selector
    When they click Save
    Then the application immediately switches to dark mode
    And the preference persists across sessions

  Scenario: Theme is set to OS Default
    Given the user selects "OS Default" from the theme selector
    When they click Save
    Then the application follows the operating system's color scheme preference

  Scenario: Tokens are never exposed in plaintext
    Given encrypted tokens are stored in the database
    When the settings page loads
    Then token fields display masked values (e.g., "••••••••abc")
    And the full plaintext token is never sent to the frontend

  Scenario: Application reads tokens from the database at runtime
    Given tokens have been saved via the settings page
    When a sync operation is triggered
    Then the application decrypts the token from the database using the master key
    And uses it to authenticate with the external API

  Scenario: Fallback to dotnet user-secrets when no database token exists
    Given no token has been saved via the settings page
    And a token exists in dotnet user-secrets
    When a sync operation is triggered
    Then the application uses the user-secrets token

  Scenario: Feature flags reflect configured services
    Given the user has saved a Hardcover API token via settings
    And no Discogs token is configured anywhere
    When GET /api/v1/config/features is called
    Then HardcoverEnabled is true
    And DiscogsEnabled is false
```

---

## [ABM-040] Sync Dropdown Button with Multi-Service Support

**Status:** Backlog
**Priority:** Medium

### Business Problem
The current toolbar has separate "Sync Records" and "Sync Books" buttons, which doesn't scale as more integrations are added (e.g., BoardGameGeek). A single dropdown sync button consolidates sync actions and dynamically shows only the services that are configured. The dropdown defaults to syncing the current service based on context (Records page → Discogs, Books page → Hardcover), with a "Sync All" option available.

### Acceptance Criteria
```gherkin
Feature: Sync dropdown button with multi-service support

  Scenario: Sync button displays as a dropdown
    Given the user is on any page
    When they look at the toolbar
    Then they see a single "Sync" dropdown button instead of separate sync buttons

  Scenario: Dropdown shows only configured services
    Given the Discogs token is configured
    And the Hardcover token is not configured
    When the user opens the sync dropdown
    Then "Sync Records" is shown
    And "Sync Books" is not shown
    And "Sync All" is shown

  Scenario: Dropdown shows all configured services
    Given both Discogs and Hardcover tokens are configured
    When the user opens the sync dropdown
    Then "Sync Records", "Sync Books", and "Sync All" are shown

  Scenario: Default sync action on Records page
    Given the user is on the Records collection page
    When they click the sync button (not the dropdown arrow)
    Then a Discogs sync is triggered

  Scenario: Default sync action on Books page
    Given the user is on the Books page
    When they click the sync button (not the dropdown arrow)
    Then a Hardcover sync is triggered

  Scenario: Default sync action on non-service pages
    Given the user is on the Statistics, Pick, Maintenance, or Store Finder page
    When they click the sync button (not the dropdown arrow)
    Then a "Sync All" operation is triggered (syncing all configured services)

  Scenario: Sync All triggers all configured services
    Given both Discogs and Hardcover are configured
    When the user selects "Sync All" from the dropdown
    Then both Discogs and Hardcover syncs are triggered concurrently

  Scenario: Progress indicator during sync
    Given a sync is in progress for one or more services
    Then the sync button shows a spinner
    And the dropdown indicates which services are currently syncing

  Scenario: Service options update after configuration change
    Given the user adds a new API token via the settings page
    When they open the sync dropdown
    Then the newly configured service appears as a sync option
```

Note: This item depends on the Configuration & Settings Page backlog item (for dynamic service detection based on configured tokens). The dropdown options are driven by the existing GET /api/v1/config/features endpoint.

---

## [ABM-041] Display Marketplace Pricing on Release Detail View

**Status:** Backlog
**Priority:** Low

### Business Problem
The release detail view already stores three marketplace price fields (lowest, median, highest) from Discogs, but they are not displayed anywhere in the UI. Showing the pricing gives a quick sense of a record's market value. The format should be concise: show the median price prominently with the range in parentheses, e.g., "$38 ($37 - $45)". Drop decimal places using standard rounding rules (e.g., $37.50 rounds to $38, $37.49 rounds to $37).

### Acceptance Criteria
```gherkin
Feature: Display marketplace pricing on release detail view

  Scenario: Release has all three price fields populated
    Given a release has lowestPrice, medianPrice, and highestPrice
    When I view the release detail page
    Then I see a "Marketplace Pricing" section
    And the pricing is displayed as "{median} ({low} - {high})"
    For example: "$38 ($37 - $45)"

  Scenario: Prices are displayed as whole dollars
    Given a release has medianPrice 37.50, lowestPrice 19.99, highestPrice 44.51
    When I view the release detail page
    Then the pricing is displayed as "$38 ($20 - $45)"
    And decimals are dropped using standard rounding rules

  Scenario: Release has no pricing data
    Given a release has null lowestPrice, medianPrice, and highestPrice
    When I view the release detail page
    Then the "Marketplace Pricing" section is not shown

  Scenario: Release has partial pricing data
    Given a release has some price fields null and others populated
    When I view the release detail page
    Then the "Marketplace Pricing" section is not shown
    Because all three values are required to display the range format
```

---

## [ABM-042] Context-Aware Random Picker

**Status:** Backlog
**Priority:** Medium

### Business Problem
The "Pick" (random) button currently appears in the global toolbar on every page, but it only picks a random vinyl record from the Discogs collection. When the user is on the Books page, they'd expect "Pick" to choose a random book from Hardcover, not a random record. The random picker should be context-aware: on the Records page it picks a random record, on the Books page it picks a random book, and on non-service pages it picks from any collection randomly. This requires adding a random book endpoint on the backend and updating the random picker component to be service-aware.

### Acceptance Criteria
```gherkin
Feature: Context-aware random picker

  Scenario: Random pick from Records page
    Given the user is on the Records collection page
    When they click the Pick button
    Then a random record from the Discogs collection is displayed

  Scenario: Random pick from Books page
    Given the user is on the Books page
    When they click the Pick button
    Then a random book from the Hardcover collection is displayed

  Scenario: Random pick from a non-service page
    Given the user is on the Statistics, Maintenance, or Store Finder page
    When they click the Pick button
    Then a random item is picked from any configured collection

  Scenario: Random book endpoint exists
    Given the Hardcover collection contains books
    When GET /api/v1/books/random is called
    Then a randomly selected book is returned with full detail

  Scenario: Random book with no books in collection
    Given the Hardcover collection is empty
    When GET /api/v1/books/random is called
    Then the response is HTTP 404 Not Found

  Scenario: Random pick only considers configured services
    Given only Discogs is configured (no Hardcover token)
    When the user clicks Pick from a non-service page
    Then a random record is picked (books are not considered)

  Scenario: Pick button hidden when no services configured
    Given no external API tokens are configured
    Then the Pick button is not shown in the toolbar
```

---

## [ABM-043] Maintenance Page Shows Too Many Records (Overly Aggressive Incomplete Filter)

**Status:** Backlog
**Priority:** High

### Business Problem
The maintenance page is meant to surface records with genuinely incomplete data that the user should fix on Discogs, but it currently lists nearly every record. The root cause is that the "incomplete" filter checks for null on six fields: CoverImageUrl, Genre, HighestPrice, LowestPrice, MedianPrice, and Year. Since marketplace pricing data (HighestPrice, LowestPrice, MedianPrice) is frequently unavailable from Discogs — even for records with otherwise complete data — almost every record matches the filter. Pricing availability is outside the user's control, so it should not flag a record as needing attention. The filter should only consider fields the user can actually fix on Discogs: Genre, Year, and CoverImageUrl. Pricing fields should be excluded from the incomplete check (though they can still appear as informational "missing" chips if desired).

### Acceptance Criteria
```gherkin
Feature: Maintenance page incomplete filter fix

  Scenario: Record with complete user-editable fields is not shown
    Given a release has Genre, Year, and CoverImageUrl populated
    And HighestPrice, LowestPrice, and MedianPrice are null
    When I view the maintenance page
    Then the release is NOT listed as incomplete

  Scenario: Record missing Genre is shown
    Given a release has null Genre
    When I view the maintenance page
    Then the release is listed with "Genre" in its missing fields

  Scenario: Record missing Year is shown
    Given a release has null Year
    When I view the maintenance page
    Then the release is listed with "Year" in its missing fields

  Scenario: Record missing CoverImageUrl is shown
    Given a release has null CoverImageUrl
    When I view the maintenance page
    Then the release is listed with "Cover Art" in its missing fields

  Scenario: All records have user-editable fields complete
    Given all releases have Genre, Year, and CoverImageUrl populated
    When I view the maintenance page
    Then the empty state message "All records have complete data." is shown

  Scenario: Missing pricing is shown as informational only
    Given a release has null pricing fields but Genre, Year, and CoverImageUrl are populated
    When I view the maintenance page
    Then the release is NOT listed as incomplete
```
