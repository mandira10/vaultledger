-- 001_initial_schema.sql
-- All 11 tables for VaultLedger with FKs and CHECK constraints.
-- Composite FKs enforce tenant boundary at the DB level.
-- Indexes live in 002; immutability trigger in 003; pgvector in 004/005.

CREATE TABLE tenants (
    id          UUID PRIMARY KEY,
    name        VARCHAR(200) NOT NULL,
    plan        VARCHAR(20)  NOT NULL CHECK (plan IN ('Free', 'Pro', 'Enterprise')),
    created_at  TIMESTAMP    NOT NULL
);

-- Global identity — no tenant_id on purpose; memberships link users to tenants.
CREATE TABLE users (
    id             UUID PRIMARY KEY,
    email          VARCHAR(320) NOT NULL,
    name           VARCHAR(200) NOT NULL,
    password_hash  VARCHAR(200) NOT NULL,
    created_at     TIMESTAMP    NOT NULL,
    CONSTRAINT users_email_unique UNIQUE (email)
);

CREATE TABLE tenant_memberships (
    id          UUID PRIMARY KEY,
    tenant_id   UUID        NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    user_id     UUID        NOT NULL REFERENCES users(id)   ON DELETE RESTRICT,
    role        VARCHAR(20) NOT NULL CHECK (role IN ('Admin', 'Approver', 'Auditor')),
    created_at  TIMESTAMP   NOT NULL,
    CONSTRAINT tenant_memberships_tenant_user_unique UNIQUE (tenant_id, user_id)
);

CREATE TABLE tenant_integrations (
    id            UUID PRIMARY KEY,
    tenant_id     UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    provider      VARCHAR(20)  NOT NULL CHECK (provider IN ('Slack', 'Email', 'Webhook')),
    endpoint_url  VARCHAR(500),
    api_key_enc   VARCHAR(500) NOT NULL,
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMP    NOT NULL,
    CONSTRAINT tenant_integrations_tenant_provider_unique UNIQUE (tenant_id, provider)
);

-- Compliance subjects (entity_type distinguishes people/companies/vendors).
-- Alternate key (tenant_id, id) is the target for composite FK from cases.
CREATE TABLE entities (
    id            UUID        NOT NULL,
    tenant_id     UUID        NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name          VARCHAR(300) NOT NULL,
    entity_type   VARCHAR(20)  NOT NULL CHECK (entity_type IN ('Individual', 'Company', 'Vendor')),
    reference_id  VARCHAR(100),
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMP    NOT NULL,
    PRIMARY KEY (id),
    CONSTRAINT entities_tenant_id_key UNIQUE (tenant_id, id),
    CONSTRAINT entities_tenant_reference_unique UNIQUE (tenant_id, reference_id)
);

CREATE TABLE cases (
    id             UUID        NOT NULL,
    tenant_id      UUID        NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    entity_id      UUID        NOT NULL,
    title          VARCHAR(300) NOT NULL,
    status         VARCHAR(20)  NOT NULL CHECK (status IN ('Open', 'UnderReview', 'Closed')),
    priority       VARCHAR(20)  NOT NULL CHECK (priority IN ('Low', 'Medium', 'High', 'Critical')),
    last_entry_at  TIMESTAMP,
    created_at     TIMESTAMP    NOT NULL,
    PRIMARY KEY (id),
    CONSTRAINT cases_tenant_id_key UNIQUE (tenant_id, id),
    CONSTRAINT cases_entity_fk FOREIGN KEY (tenant_id, entity_id)
        REFERENCES entities(tenant_id, id) ON DELETE RESTRICT
);

-- Immutability is also enforced by a trigger in 003 and by absence of an
-- update command in the application layer.
CREATE TABLE audit_entries (
    id          UUID PRIMARY KEY,
    tenant_id   UUID        NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    case_id     UUID        NOT NULL,
    created_by  UUID        NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    entry_type  VARCHAR(20) NOT NULL CHECK (entry_type IN ('Observation', 'Finding', 'Recommendation', 'Action')),
    body        TEXT        NOT NULL,
    severity    VARCHAR(20) NOT NULL CHECK (severity IN ('Info', 'Low', 'Medium', 'High', 'Critical')),
    created_at  TIMESTAMP   NOT NULL,
    CONSTRAINT audit_entries_case_fk FOREIGN KEY (tenant_id, case_id)
        REFERENCES cases(tenant_id, id) ON DELETE RESTRICT
);

CREATE TABLE compliance_reviews (
    id           UUID PRIMARY KEY,
    tenant_id    UUID        NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    case_id      UUID        NOT NULL,
    reviewed_by  UUID                 REFERENCES users(id) ON DELETE RESTRICT,
    summary      TEXT        NOT NULL,
    status       VARCHAR(20) NOT NULL CHECK (status IN ('Pending', 'Approved', 'Rejected', 'NeedsRevision')),
    comments     TEXT,
    created_at   TIMESTAMP   NOT NULL,
    reviewed_at  TIMESTAMP,
    CONSTRAINT compliance_reviews_case_fk FOREIGN KEY (tenant_id, case_id)
        REFERENCES cases(tenant_id, id) ON DELETE RESTRICT
);

CREATE TABLE integration_events (
    id                 UUID PRIMARY KEY,
    tenant_id          UUID         NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    provider           VARCHAR(20)  NOT NULL CHECK (provider IN ('Slack', 'Email', 'Webhook')),
    external_id        VARCHAR(200) NOT NULL,
    raw_payload        JSONB        NOT NULL,
    processing_status  VARCHAR(20)  NOT NULL CHECK (processing_status IN ('Pending', 'Processed', 'Failed')),
    created_at         TIMESTAMP    NOT NULL,
    CONSTRAINT integration_events_external_unique UNIQUE (external_id)
);

CREATE TABLE ai_interactions (
    id                 UUID PRIMARY KEY,
    tenant_id          UUID          NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    user_id            UUID          NOT NULL REFERENCES users(id)   ON DELETE RESTRICT,
    interaction_type   VARCHAR(30)   NOT NULL CHECK (interaction_type IN (
        'DraftAuditEntry', 'SummarizeReview', 'Embed',
        'SemanticSearch', 'PiiScan', 'DataQuestion'
    )),
    model_provider     VARCHAR(20)   NOT NULL CHECK (model_provider IN ('Anthropic', 'OpenAI')),
    model              VARCHAR(100)  NOT NULL,
    prompt_tokens      INTEGER       NOT NULL,
    completion_tokens  INTEGER       NOT NULL,
    cost_usd           NUMERIC(10, 6) NOT NULL,
    created_at         TIMESTAMP     NOT NULL
);

CREATE TABLE ai_usage_quotas (
    id            UUID PRIMARY KEY,
    tenant_id     UUID           NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    period_start  TIMESTAMP      NOT NULL,
    token_budget  BIGINT         NOT NULL,
    tokens_used   BIGINT         NOT NULL DEFAULT 0,
    cost_usd      NUMERIC(10, 6) NOT NULL DEFAULT 0,
    created_at    TIMESTAMP      NOT NULL
);
