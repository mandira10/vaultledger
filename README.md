# VaultLedger

Multi-tenant compliance and audit trail platform built with Next.js, Tailwind, C# / .NET 8, Clean Architecture, EF Core and PostgreSQL. Features role-based access control, immutable audit logging, document management workflows and tenant isolation.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Database Schema](#database-schema)
- [Tenant Isolation](#tenant-isolation)
- [API Endpoints](#api-endpoints)
- [Roles & Permissions](#roles--permissions)
- [Database Migrations](#database-migrations)
- [Environment Variables](#environment-variables)
- [Getting Started](#getting-started)

## Overview

VaultLedger provides organizations with a centralized platform to manage compliance cases, track audit entries and run approval workflows — all within strict tenant boundaries. Every record is scoped to a tenant, audit entries are immutable (append-only) and role-based policies control who can create, review and approve.

### Key Features

- **Multi-tenancy** — shared database with tenant isolation via EF Core global query filters
- **Immutable audit trail** — audit entries cannot be updated or deleted once created
- **Approval workflows** — compliance reviews with pending/approved/rejected lifecycle
- **Role-based access** — admin, approver and auditor roles with distinct permissions
- **Composite foreign keys** — prevent cross-tenant data corruption at the database level

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                    API Layer                        │
│         Controllers · Middleware · Filters          │
├─────────────────────────────────────────────────────┤
│                Application Layer                    │
│      Commands · Queries · Validators · DTOs         │
│              (MediatR + FluentValidation)           │
├─────────────────────────────────────────────────────┤
│              Infrastructure Layer                   │
│    EF Core · DbContext · JWT · Serilog · Services   │
├─────────────────────────────────────────────────────┤
│                 Domain Layer                        │
│       Entities · Enums · Interfaces · ValueObjects  │
└─────────────────────────────────────────────────────┘
```

**Dependency rule:** each layer only depends on the layer directly below it. Domain has zero external dependencies.

**Request pipeline:**

```
Request
  → ExceptionMiddleware (catch errors, return structured JSON)
  → JWT Authentication (validate token)
  → TenantContextMiddleware (extract tenant_id, user_id, role from claims)
  → Authorization (check role-based policies)
  → FluentValidation (validate request body via MediatR pipeline)
  → Controller → MediatR Handler → EF Core → PostgreSQL
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 8 (LTS) |
| Web framework | ASP.NET Core Web API |
| ORM | Entity Framework Core + Npgsql |
| Database | PostgreSQL 16 |
| Migrations | DbUp (hand-written SQL scripts) |
| Architecture | Clean Architecture (4 layers) |
| CQRS | MediatR |
| Validation | FluentValidation |
| Auth | JWT Bearer tokens + BCrypt |
| Logging | Serilog (structured, with correlation IDs) |
| Testing | xUnit + Testcontainers + WebApplicationFactory |
| Frontend | Next.js 14 (App Router) + TypeScript + Tailwind CSS |
| Containerization | Docker + Docker Compose |
| CI/CD | GitHub Actions |
| Docs | Swagger / OpenAPI |

## Database Schema

9 tables organized around multi-tenant compliance tracking.

### Entity Relationship Overview

```
tenants ──┬── tenant_memberships ──── users (global, no tenant_id)
          ├── tenant_integrations
          ├── entities
          ├── cases ──┬── audit_entries
          │           └── compliance_reviews
          └── integration_events
```

### Table Definitions

#### tenants

| Column | Type | Constraints |
|--------|------|-------------|
| id | GUID | PK |
| name | string | NOT NULL |
| plan | string | NOT NULL — `free`, `pro`, `enterprise` |
| created_at | DateTime | NOT NULL, UTC |

#### users

Global identity table — **no tenant_id**. Users can belong to multiple tenants.

| Column | Type | Constraints |
|--------|------|-------------|
| id | GUID | PK |
| email | string | UNIQUE, NOT NULL |
| name | string | NOT NULL |
| password_hash | string | NOT NULL (BCrypt) |
| created_at | DateTime | NOT NULL, UTC |

#### tenant_memberships

Links users to tenants with a role.

| Column | Type | Constraints |
|--------|------|-------------|
| id | GUID | PK |
| tenant_id | GUID | FK → tenants, NOT NULL |
| user_id | GUID | FK → users, NOT NULL |
| role | string | NOT NULL — `admin`, `approver`, `auditor` |
| created_at | DateTime | NOT NULL, UTC |

**Unique:** `(tenant_id, user_id)` — one membership per user per tenant.

#### tenant_integrations

External service credentials per tenant.

| Column | Type | Constraints |
|--------|------|-------------|
| id | GUID | PK |
| tenant_id | GUID | FK → tenants, NOT NULL |
| provider | string | NOT NULL — `slack`, `email`, `webhook` |
| endpoint_url | string | nullable |
| api_key_enc | string | encrypted |
| is_active | bool | NOT NULL, DEFAULT true |
| created_at | DateTime | NOT NULL, UTC |

**Unique:** `(tenant_id, provider)` — one integration per provider per tenant.

#### entities

Companies or people being tracked for compliance.

| Column | Type | Constraints |
|--------|------|-------------|
| id | GUID | PK |
| tenant_id | GUID | FK → tenants, NOT NULL |
| name | string | NOT NULL |
| entity_type | string | NOT NULL — `individual`, `company`, `vendor` |
| reference_id | string | external reference number |
| is_active | bool | NOT NULL, DEFAULT true |
| created_at | DateTime | NOT NULL, UTC |

**Unique:** `(tenant_id, reference_id)` — scoped uniqueness.

#### cases

Groups of related audit entries.

| Column | Type | Constraints |
|--------|------|-------------|
| id | GUID | PK |
| tenant_id | GUID | FK → tenants, NOT NULL |
| entity_id | GUID | composite FK `(tenant_id, entity_id)` → entities `(tenant_id, id)` |
| title | string | NOT NULL |
| status | string | NOT NULL — `open`, `under_review`, `closed` |
| priority | string | NOT NULL — `low`, `medium`, `high`, `critical` |
| last_entry_at | DateTime | nullable, cached for sorting |
| created_at | DateTime | NOT NULL, UTC |

#### audit_entries

**Immutable records** — the core of the system. No update or delete operations allowed.

| Column | Type | Constraints |
|--------|------|-------------|
| id | GUID | PK |
| tenant_id | GUID | FK → tenants, NOT NULL |
| case_id | GUID | composite FK `(tenant_id, case_id)` → cases `(tenant_id, id)` |
| created_by | GUID | FK → users, NOT NULL |
| entry_type | string | NOT NULL — `observation`, `finding`, `recommendation`, `action` |
| body | string | NOT NULL |
| severity | string | NOT NULL — `info`, `low`, `medium`, `high`, `critical` |
| created_at | DateTime | NOT NULL, UTC |

#### compliance_reviews

Approval workflow for case reviews.

| Column | Type | Constraints |
|--------|------|-------------|
| id | GUID | PK |
| tenant_id | GUID | FK → tenants, NOT NULL |
| case_id | GUID | composite FK `(tenant_id, case_id)` → cases `(tenant_id, id)` |
| reviewed_by | GUID | FK → users, nullable |
| summary | string | NOT NULL |
| status | string | NOT NULL — `pending`, `approved`, `rejected`, `needs_revision` |
| comments | string | nullable |
| created_at | DateTime | NOT NULL, UTC |
| reviewed_at | DateTime | nullable |

#### integration_events

External system data log for webhook payloads.

| Column | Type | Constraints |
|--------|------|-------------|
| id | GUID | PK |
| tenant_id | GUID | FK → tenants, NOT NULL |
| provider | string | NOT NULL |
| external_id | string | UNIQUE (deduplication key) |
| raw_payload | jsonb | NOT NULL |
| processing_status | string | NOT NULL — `pending`, `processed`, `failed` |
| created_at | DateTime | NOT NULL, UTC |

### Indexes

```sql
-- Auth and membership
CREATE INDEX idx_memberships_user ON tenant_memberships(user_id);

-- Case listing (most frequent query)
CREATE INDEX idx_cases_inbox ON cases(tenant_id, status, last_entry_at DESC);
CREATE INDEX idx_cases_entity ON cases(tenant_id, entity_id);

-- Audit entry loading
CREATE INDEX idx_entries_case ON audit_entries(tenant_id, case_id, created_at DESC);

-- Review workflow
CREATE INDEX idx_reviews_status ON compliance_reviews(tenant_id, status, case_id);

-- Integration event processing (partial index)
CREATE INDEX idx_events_pending ON integration_events(tenant_id, processing_status, created_at DESC)
  WHERE processing_status = 'pending';
```

## Tenant Isolation

VaultLedger uses a **shared database, shared schema** model with isolation enforced at two levels:

### 1. EF Core Global Query Filters (Application-Level RLS)

Every tenant-scoped entity gets a query filter applied automatically:

```csharp
modelBuilder.Entity<Case>()
    .HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
```

This adds `WHERE tenant_id = @currentTenant` to every query without manual filtering.

### 2. Composite Foreign Keys (Data-Level Protection)

Child tables reference parent tables via `(tenant_id, parent_id)` — not just `parent_id`:

```csharp
modelBuilder.Entity<AuditEntry>()
    .HasOne(e => e.Case)
    .WithMany(c => c.Entries)
    .HasForeignKey(e => new { e.TenantId, e.CaseId })
    .HasPrincipalKey(c => new { c.TenantId, c.Id });
```

This prevents a bug or query filter bypass from creating records that cross tenant boundaries. Even if a query filter is accidentally disabled, the foreign key constraint blocks cross-tenant references.

### Why Both?

| Layer | Protects Against |
|-------|-----------------|
| Query filters | Accidental data leakage in normal operations |
| Composite FKs | Data corruption from bugs, bypassed filters, or direct DB access |

## API Endpoints

All endpoints require JWT authentication unless noted. Tenant context is extracted from the token.

### Auth

| Method | Route | Roles | Description |
|--------|-------|-------|-------------|
| POST | `/api/auth/register` | public | Create account + first tenant |
| POST | `/api/auth/login` | public | Authenticate, receive JWT |
| POST | `/api/auth/switch-tenant` | any | Issue new JWT for different tenant |

### Tenants

| Method | Route | Roles | Description |
|--------|-------|-------|-------------|
| GET | `/api/tenants/current` | any | Get current tenant info |
| PUT | `/api/tenants/current` | admin | Update tenant settings |
| GET | `/api/tenants/current/members` | any | List tenant members |
| POST | `/api/tenants/current/members/invite` | admin | Invite a user to tenant |

### Entities

| Method | Route | Roles | Description |
|--------|-------|-------|-------------|
| GET | `/api/entities` | any | List entities (paginated) |
| GET | `/api/entities/{id}` | any | Get entity by ID |
| POST | `/api/entities` | admin, auditor | Create entity |
| PUT | `/api/entities/{id}` | admin, auditor | Update entity |

### Cases

| Method | Route | Roles | Description |
|--------|-------|-------|-------------|
| GET | `/api/cases` | any | List cases (filter by status, entity, priority) |
| GET | `/api/cases/{id}` | any | Get case by ID |
| POST | `/api/cases` | admin, auditor | Create case |
| PATCH | `/api/cases/{id}/status` | admin, auditor, approver | Update case status |

### Audit Entries

| Method | Route | Roles | Description |
|--------|-------|-------|-------------|
| GET | `/api/cases/{caseId}/entries` | any | List entries for a case |
| POST | `/api/cases/{caseId}/entries` | admin, auditor | Create audit entry |

**No PUT or DELETE** — audit entries are immutable.

### Compliance Reviews

| Method | Route | Roles | Description |
|--------|-------|-------|-------------|
| GET | `/api/cases/{caseId}/reviews` | any | List reviews for a case |
| POST | `/api/cases/{caseId}/reviews` | any | Create a pending review |
| PATCH | `/api/reviews/{id}/approve` | approver, admin | Approve review |
| PATCH | `/api/reviews/{id}/reject` | approver, admin | Reject review |

### Integration Events

| Method | Route | Roles | Description |
|--------|-------|-------|-------------|
| GET | `/api/integration-events` | admin | List events (debugging) |
| POST | `/api/webhooks/{provider}` | public* | Receive webhook payload |

*Webhook endpoint validates a shared secret from the integration's stored credentials.

## Roles & Permissions

| Permission | Admin | Approver | Auditor |
|-----------|-------|----------|---------|
| View all data | Yes | Yes | Yes |
| Create entities | Yes | No | Yes |
| Create cases | Yes | No | Yes |
| Create audit entries | Yes | No | Yes |
| Update case status | Yes | Yes | Yes |
| Create compliance reviews | Yes | Yes | Yes |
| Approve/reject reviews | Yes | Yes | No |
| Manage tenant settings | Yes | No | No |
| Invite members | Yes | No | No |
| View integration events | Yes | No | No |

## Database Migrations

VaultLedger uses a **hybrid migration strategy**: EF Core manages the runtime `DbContext` (query filters, composite foreign keys, entity configuration) while schema evolution is driven by **hand-written, versioned SQL scripts** executed via [DbUp](https://dbup.readthedocs.io/).

### Why Hybrid?

| Concern | Why it matters in compliance/audit domains |
|---------|-------------------------------------------|
| **Auditability** | Every schema change is a reviewable SQL file in version control. Compliance auditors can read it without knowing .NET. |
| **Explicit control** | We write exactly what runs. No ORM "magic" generating unexpected `DROP` statements. |
| **PostgreSQL features** | Partial indexes, triggers, `GIN` indexes on `jsonb` and `CHECK` constraints are first-class — not fighting the ORM. |
| **Zero-downtime patterns** | Expand/contract migrations (see below) require carefully ordered SQL that ORM migrations can't express cleanly. |
| **Rollback strategy** | Every forward migration has a matching `down` script — we can roll back precisely. |

EF Core is still excellent for modelling relationships, query filters and type-safe queries. We keep it for what it's good at and own the schema layer ourselves.

### Directory Layout

```
src/VaultLedger.Infrastructure/Migrations/
├── Scripts/
│   ├── up/
│   │   ├── 001_initial_schema.sql
│   │   ├── 002_indexes.sql
│   │   ├── 003_audit_entries_immutability_trigger.sql
│   │   └── ...
│   └── down/
│       ├── 001_initial_schema.down.sql
│       ├── 002_indexes.down.sql
│       └── ...
└── MigrationRunner.cs          # DbUp configuration + execution
```

DbUp tracks applied scripts in a `schema_migrations` table inside the target database. Each script runs in a transaction. Scripts are embedded as resources so the runner can execute them from a published container.

### Naming Convention

```
<NNN>_<snake_case_description>.sql          # forward migration
<NNN>_<snake_case_description>.down.sql     # rollback
```

- **Sequential numbering** (`001`, `002`, …) — keeps history linear and merge conflicts obvious
- **Snake-case description** — matches PostgreSQL naming style
- **Always paired** — no forward script ships without a matching down script

Examples:
```
001_initial_schema.sql
002_indexes.sql
003_audit_entries_immutability_trigger.sql
004_add_cases_tags_column.sql
005_backfill_cases_last_entry_at.sql
```

### Commands

```bash
# Apply all pending migrations
dotnet run --project src/VaultLedger.Infrastructure -- migrate up

# Roll back the most recent migration (runs matching .down.sql)
dotnet run --project src/VaultLedger.Infrastructure -- migrate down

# Generate a SQL preview of what would run (no execution)
dotnet run --project src/VaultLedger.Infrastructure -- migrate preview

# Check migration status
dotnet run --project src/VaultLedger.Infrastructure -- migrate status
```

In CI/CD, the same commands run against staging/prod with explicit approval gates.

### Environment Strategy

| Environment | How migrations run | Review gate |
|-------------|-------------------|-------------|
| **Local dev** | Auto-applied on API startup (`migrate up`) | None |
| **CI integration tests** | Auto-applied against Testcontainers PostgreSQL | None |
| **Staging** | Run via deployment pipeline before app deploy | PR review |
| **Production** | Manual `migrate up` as a separate pipeline step | PR review + DBA approval |

The API container **does not** run migrations on startup in staging/prod — a separate job does. This prevents race conditions when scaling horizontally and makes migrations a deliberate, observable event.

### Migration Types

**Schema migrations** — pure DDL (`CREATE TABLE`, `ALTER COLUMN`, `CREATE INDEX`). These are fast and reversible.

**Data migrations** — `UPDATE` / `INSERT` for backfills or data reshaping. These are slower and harder to roll back. Rules:
- Always batch (`UPDATE ... WHERE id IN (SELECT id FROM ... LIMIT 10000)`)
- Use a separate migration file from the schema change
- Never in the same script as the schema change that depends on the data

**Index migrations** — on large tables, always use `CREATE INDEX CONCURRENTLY` in PostgreSQL to avoid locking writes. DbUp supports this because each script runs in its own transaction control.

### Zero-Downtime Migrations (Expand / Contract)

For breaking schema changes on a running system, we follow the **expand / contract** pattern across multiple deployments:

**Example: renaming `entities.name` → `entities.display_name`**

```
Deploy 1 — EXPAND
  ├── Migration:  ADD COLUMN display_name; backfill from name; add sync trigger
  └── App code:   writes to BOTH columns, reads from name

Deploy 2 — MIGRATE READS
  └── App code:   writes to both, reads from display_name

Deploy 3 — CONTRACT
  ├── App code:   writes to display_name only
  └── Migration:  DROP trigger; DROP COLUMN name
```

Each deploy is independently rollback-safe. Never combine expand and contract in one deployment.

### Reviewing a Migration

Every migration PR must include:

1. **The forward script** (`NNN_description.sql`)
2. **The rollback script** (`NNN_description.down.sql`)
3. **A brief PR description** covering:
   - What the change does
   - Whether it requires a data backfill
   - Whether it's locking (blocks writes) or non-locking
   - Expected runtime on the largest table
   - Any expand/contract coordination required
4. **Test evidence** that both forward and rollback were tested locally

### Testing Migrations

```bash
# Integration test harness spins up a fresh PostgreSQL container,
# applies all migrations, then runs test suite
dotnet test tests/VaultLedger.IntegrationTests
```

Specific migration tests live in `tests/VaultLedger.IntegrationTests/Migrations/`:
- **Forward application** — each migration applies cleanly on an empty database
- **Idempotency** — running `migrate up` twice is a no-op
- **Rollback** — each forward migration can be rolled back and reapplied
- **Schema drift** — the final schema matches what EF Core expects (compared via `dotnet ef dbcontext script`)

### What the Initial Schema (`001_initial_schema.sql`) Creates

- All 9 tables with explicit column types and constraints
- Composite foreign keys on `cases`, `audit_entries` and `compliance_reviews`
- Unique constraints scoped to tenant (`UNIQUE(tenant_id, field)`)
- Enum fields as `TEXT` with `CHECK` constraints (not PostgreSQL enums — enums are hard to migrate)
- `created_at` columns with `TIMESTAMPTZ NOT NULL DEFAULT NOW() AT TIME ZONE 'UTC'`

### What `003_audit_entries_immutability_trigger.sql` Does

Enforces append-only behaviour at the database level — even a direct SQL `UPDATE` or `DELETE` on `audit_entries` is blocked:

```sql
CREATE OR REPLACE FUNCTION prevent_audit_entry_modification()
RETURNS TRIGGER AS $$
BEGIN
  RAISE EXCEPTION 'audit_entries is append-only';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER audit_entries_no_update
  BEFORE UPDATE OR DELETE ON audit_entries
  FOR EACH ROW EXECUTE FUNCTION prevent_audit_entry_modification();
```

Immutability is enforced at four layers: domain (private setters), application (no update command exists), API (no PUT/DELETE route) and database (this trigger).

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | — |
| `Jwt__Secret` | JWT signing key (min 32 chars) | — |
| `Jwt__Issuer` | JWT issuer claim | `VaultLedger` |
| `Jwt__Audience` | JWT audience claim | `VaultLedger` |
| `Jwt__ExpiryMinutes` | Token expiry in minutes | `60` |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` |

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/products/docker-desktop) + Docker Compose
- [Node.js 20+](https://nodejs.org/) (for frontend)

### Run with Docker Compose

```bash
# Start PostgreSQL and the API
docker-compose up -d

# API available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger
```

### Run Locally

```bash
# Start PostgreSQL
docker-compose up -d postgres

# Run the API
cd src/VaultLedger.API
dotnet run

# Run the frontend (separate terminal)
cd src/vaultledger-ui
npm install
npm run dev
```

### Run Tests

```bash
# All tests (requires Docker for integration tests)
dotnet test

# Unit tests only
dotnet test tests/VaultLedger.UnitTests

# Integration tests only
dotnet test tests/VaultLedger.IntegrationTests
```

## Project Structure

```
src/
├── VaultLedger.Domain/             # Entities, enums, interfaces (zero dependencies)
├── VaultLedger.Application/        # Commands, queries, validators, DTOs (MediatR)
├── VaultLedger.Infrastructure/     # EF Core, JWT, Serilog, external services
├── VaultLedger.API/                # Controllers, middleware, Program.cs
└── vaultledger-ui/                 # Next.js frontend

tests/
├── VaultLedger.UnitTests/          # Domain and application layer tests
└── VaultLedger.IntegrationTests/   # Full API tests with Testcontainers

docker-compose.yml
Dockerfile
.github/workflows/ci.yml
```

## License

Private repository. All rights reserved.
