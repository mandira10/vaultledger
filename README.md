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

VaultLedger uses EF Core migrations to manage schema changes.

### Migration Commands

```bash
# Create a new migration
dotnet ef migrations add <MigrationName> \
  --project src/VaultLedger.Infrastructure \
  --startup-project src/VaultLedger.API

# Apply pending migrations
dotnet ef database update \
  --project src/VaultLedger.Infrastructure \
  --startup-project src/VaultLedger.API

# Generate SQL script (for review before applying to production)
dotnet ef migrations script \
  --project src/VaultLedger.Infrastructure \
  --startup-project src/VaultLedger.API \
  --output migrations.sql

# Revert to a specific migration
dotnet ef database update <PreviousMigrationName> \
  --project src/VaultLedger.Infrastructure \
  --startup-project src/VaultLedger.API

# Remove the last migration (if not yet applied)
dotnet ef migrations remove \
  --project src/VaultLedger.Infrastructure \
  --startup-project src/VaultLedger.API
```

### Migration Naming Convention

Use descriptive, timestamped names:

```
InitialCreate              — first migration with all 9 tables
AddCasesPriorityIndex      — adds a specific index
AlterEntityAddPhoneColumn  — schema change to existing table
```

### Migration Strategy

1. **Development:** migrations auto-apply on startup via `context.Database.Migrate()`
2. **Production:** generate SQL scripts, review, then apply manually or via CI pipeline
3. **Rollback:** always test rollback by reverting to the previous migration before deploying
4. **Review:** every migration should be reviewed as a SQL script (`migrations script`) before merging

### What the Initial Migration Creates

The `InitialCreate` migration sets up:
- All 9 tables with proper column types and constraints
- Composite foreign keys on `cases`, `audit_entries` and `compliance_reviews`
- Unique constraints scoped to tenant (`tenant_id + field`)
- All indexes listed in the [Indexes](#indexes) section
- Enum fields stored as strings with CHECK constraints

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
