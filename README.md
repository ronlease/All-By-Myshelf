# All By Myshelf

All By Myshelf is a personal cataloging system intended to consolidate collection data sourced from third‑party platforms such as Hardcover and Discogs into a single, user‑owned system.

The project is at an early, pre‑implementation stage. No functional application code exists yet.

---

## Problem Statement

Collection data is fragmented across specialized platforms, each optimized for its own domain and workflows. This fragmentation makes cross‑collection analysis, long‑term ownership tracking, and data portability unnecessarily difficult.

All By Myshelf aims to provide a unified catalog backed by a relational data model under the user’s direct control.

---

## Non‑Goals

* Not a replacement for source platforms (e.g. Hardcover, Discogs)
* Not a social or community platform
* Not multi‑tenant
* Not production‑hardened
* Not designed for large‑scale or distributed deployment

These constraints are intentional.

---

## Committed Technology Choices

The following choices are explicitly committed and unlikely to change without strong justification:

### Backend

* **Language:** Python (most recent stable release at time of development)
* **Database:** PostgreSQL

### Frontend

* **Framework:** Angular

The frontend and backend are maintained as **separate repositories**.

All other implementation details remain intentionally undecided.

---

## Planned Architecture (High‑Level)

* Angular frontend providing an administrative UI
* Python async HTTP API
* PostgreSQL as the sole persistence layer
* Data ingestion adapters for external source systems

Specific frameworks, libraries, and deployment mechanisms have not yet been selected.

---

## Data Model (Conceptual)

Initial domain concepts are expected to include:

* **Collection** – a logical grouping (e.g. books, records)
* **Item** – an individual owned object
* **Source** – an external system of record
* **External Reference** – source‑specific identifiers and metadata
* **Tag / Attribute** – user‑defined classification

The schema is intentionally unspecified at this stage and will evolve through experimentation.

---

## Project Status

**Pre-alpha / design phase**

This project exists solely as a learning exercise to regain hands-on proficiency in Python and Angular.

The repository is intentionally structured for critique and experimentation rather than longevity or production use. Friends and peers may contribute for feedback and review.

Current focus areas:

* Repository structure
* Tooling and environment decisions
* Data modeling exploration

No guarantees regarding long-term maintenance.
