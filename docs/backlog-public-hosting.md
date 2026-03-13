# All By Myshelf — Public Hosting Backlog

This backlog tracks features and requirements that would be necessary if All By Myshelf were
ever hosted publicly as a multi-user SaaS application. These items are deferred from the main
backlog since the app is currently a single-user application.

---

## [PUB-001] Encrypt API Tokens at Rest (AES-256-GCM)

**Status:** Backlog
**Priority:** High

### Business Problem
If All By Myshelf is hosted publicly as a multi-user SaaS, the database will contain API tokens (Discogs PAT, Hardcover API token, etc.) for many users. Storing these tokens as plaintext creates significant security liability: a database breach would expose every user's credentials to third-party services. Encryption at rest using AES-256-GCM with proper key management ensures that even if the database is compromised, the tokens remain protected.

### Acceptance Criteria
```gherkin
Feature: Encrypt API tokens at rest

  Scenario: Token is encrypted before storage
    Given a user saves an API token via the settings page
    When the token is persisted to the database
    Then the token is encrypted using AES-256-GCM
    And the stored value is not the plaintext token
    And a unique nonce/IV is generated for each encryption operation

  Scenario: Master encryption key is sourced from secure configuration
    Given the application is starting
    When it initializes the encryption service
    Then the master key is read from an environment variable (local) or AWS Secrets Manager (cloud)
    And the application fails to start if the master key is not configured
    And the master key is never logged or exposed in error messages

  Scenario: Token is decrypted at runtime for API calls
    Given an encrypted token is stored in the database
    When a sync operation requires the token
    Then the token is decrypted in memory using the master key
    And the decrypted value is used to authenticate with the external API
    And the decrypted value is not persisted anywhere

  Scenario: Key rotation is supported
    Given a new master key version is deployed
    When an encrypted token is accessed
    Then the application attempts decryption with the current key first
    And falls back to previous key versions if decryption fails
    And successfully decrypted tokens are re-encrypted with the current key on next save

  Scenario: Plaintext tokens are never exposed via API
    Given encrypted tokens are stored in the database
    When GET /api/v1/settings is called
    Then token fields return masked values only (e.g., "************abc")
    And the response never contains the full plaintext or encrypted token
    And the response never contains the encryption key or nonce

  Scenario: Tokens cannot be retrieved in bulk
    Given multiple users have stored API tokens
    When an authenticated request is made
    Then only the requesting user's tokens are accessible
    And no API endpoint allows listing or exporting tokens for multiple users
```

---

## [PUB-002] Multi-Tenancy and Per-User Data Isolation

**Status:** Backlog
**Priority:** High

### Business Problem
A single-user application assumes all data belongs to one user. In a multi-user SaaS, each user's collection (records, books, etc.) must be completely isolated from other users. Without proper tenant isolation, users could accidentally or maliciously access each other's data, violating privacy and trust.

### Acceptance Criteria
```gherkin
Feature: Multi-tenancy and per-user data isolation

  Scenario: All data entities are scoped to a user
    Given the database schema
    When any collection entity is created (Release, Book, etc.)
    Then it includes a non-nullable UserId foreign key
    And the UserId references the authenticated user

  Scenario: Queries automatically filter by current user
    Given a user is authenticated
    When they request their collection via any API endpoint
    Then only records belonging to that user are returned
    And records belonging to other users are never included

  Scenario: User cannot access another user's data by ID
    Given User A owns a release with ID 123
    And User B is authenticated
    When User B requests GET /api/v1/releases/123
    Then the API returns 404 Not Found
    And no information about User A's release is leaked

  Scenario: Sync operations are isolated per user
    Given User A triggers a Discogs sync
    When the sync completes
    Then only User A's collection is updated
    And User B's collection is unaffected

  Scenario: Global query filter enforces isolation at EF Core level
    Given EF Core is configured with a global query filter for UserId
    When any query is executed
    Then the filter is applied automatically
    And developers cannot accidentally bypass it without explicit opt-out
```

---

## [PUB-003] API Rate Limiting

**Status:** Backlog
**Priority:** High

### Business Problem
A public SaaS is exposed to abuse: denial-of-service attacks, credential stuffing, or simply overenthusiastic API consumers. Rate limiting protects the service from being overwhelmed, ensures fair usage across users, and prevents a single bad actor from degrading the experience for everyone.

### Acceptance Criteria
```gherkin
Feature: API rate limiting

  Scenario: Authenticated requests are rate limited per user
    Given a user is authenticated
    When they exceed the allowed request rate (e.g., 100 requests/minute)
    Then subsequent requests return HTTP 429 Too Many Requests
    And the response includes a Retry-After header
    And the user's requests are accepted again after the cooldown period

  Scenario: Unauthenticated requests are rate limited by IP
    Given an unauthenticated request is made
    When the IP address exceeds the allowed request rate
    Then subsequent requests return HTTP 429 Too Many Requests
    And the response includes a Retry-After header

  Scenario: Rate limit headers are included in responses
    Given any API request is made
    When the response is returned
    Then it includes X-RateLimit-Limit (max requests allowed)
    And it includes X-RateLimit-Remaining (requests left in window)
    And it includes X-RateLimit-Reset (timestamp when limit resets)

  Scenario: Sync endpoints have stricter limits
    Given sync operations are resource-intensive
    When a user triggers a sync
    Then they are limited to a lower rate (e.g., 1 sync per 5 minutes)
    And attempting another sync within the window returns HTTP 429

  Scenario: Rate limits are configurable per tier (future)
    Given a premium user tier exists
    When a premium user makes requests
    Then they have higher rate limits than free-tier users
```

---

## [PUB-004] User Registration and Onboarding Flow

**Status:** Backlog
**Priority:** Medium

### Business Problem
A single-user app assumes the user is pre-configured. A public SaaS must allow new users to sign up, verify their identity, and be guided through initial setup (connecting external services, configuring preferences). A smooth onboarding experience reduces friction and increases user retention.

### Acceptance Criteria
```gherkin
Feature: User registration and onboarding

  Scenario: New user signs up via Auth0
    Given a visitor is not authenticated
    When they click "Sign Up"
    Then they are redirected to Auth0's signup flow
    And upon successful signup they are redirected back to the application

  Scenario: New user record is created on first login
    Given a user completes Auth0 authentication for the first time
    When they are redirected back to the application
    Then a new user record is created in the database
    And default preferences are initialized
    And the user is directed to the onboarding flow

  Scenario: Onboarding wizard guides API token setup
    Given a new user has no API tokens configured
    When they land on the application for the first time
    Then they see an onboarding wizard
    And the wizard prompts them to connect Discogs, Hardcover, etc.
    And they can skip services they do not use

  Scenario: User can complete onboarding later
    Given a user skips the onboarding wizard
    When they navigate to Settings
    Then they can configure API tokens at any time
    And the application functions with limited features until tokens are added

  Scenario: Returning user bypasses onboarding
    Given a user has completed onboarding previously
    When they log in
    Then they are taken directly to their dashboard
    And the onboarding wizard is not shown
```

---

## [PUB-005] GDPR Compliance and Data Deletion

**Status:** Backlog
**Priority:** Medium

### Business Problem
Operating a SaaS with European users requires compliance with GDPR. Users have the right to access their data, export it in a portable format, and request complete deletion. Failure to comply can result in significant fines and loss of user trust.

### Acceptance Criteria
```gherkin
Feature: GDPR compliance and data deletion

  Scenario: User can export their data
    Given a user is authenticated
    When they request a data export from Settings
    Then the application generates a downloadable archive (ZIP)
    And the archive contains all their collections, settings, and preferences
    And the data is in a portable format (JSON or CSV)

  Scenario: User can delete their account
    Given a user is authenticated
    When they request account deletion from Settings
    Then they are prompted to confirm the irreversible action
    And upon confirmation, all their data is permanently deleted
    And their API tokens are securely wiped
    And their Auth0 account link is removed

  Scenario: Deleted data is unrecoverable
    Given a user has deleted their account
    When any attempt is made to access their former data
    Then no records are returned
    And no backups contain identifiable user data after retention period

  Scenario: Data deletion is logged for compliance
    Given a user requests account deletion
    When the deletion completes
    Then an audit log entry is created (without PII)
    And the log records the timestamp and anonymized user reference
    And the log is retained for compliance purposes

  Scenario: Privacy policy is accessible
    Given any visitor (authenticated or not)
    When they navigate to /privacy
    Then they see the current privacy policy
    And the policy explains data collection, usage, and deletion rights
```

---
