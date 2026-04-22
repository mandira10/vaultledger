-- 002_indexes.sql
-- Performance indexes for the most frequent query patterns.
-- Uniqueness constraints already defined in 001 are backed by implicit indexes and not duplicated here.

-- Login flow: "show me all tenants this user belongs to".
CREATE INDEX idx_memberships_user
    ON tenant_memberships (user_id);

-- Case list view — the most frequent query in the app.
CREATE INDEX idx_cases_inbox
    ON cases (tenant_id, status, last_entry_at DESC);

-- "All cases for this compliance subject".
CREATE INDEX idx_cases_entity
    ON cases (tenant_id, entity_id);

-- Case detail timeline — loads entries newest first.
CREATE INDEX idx_entries_case
    ON audit_entries (tenant_id, case_id, created_at DESC);

-- Approver dashboard — pending reviews by case.
CREATE INDEX idx_reviews_status
    ON compliance_reviews (tenant_id, status, case_id);

-- Webhook worker poll — only pending events need to be scanned.
CREATE INDEX idx_events_pending
    ON integration_events (tenant_id, processing_status, created_at DESC)
    WHERE processing_status = 'Pending';

-- AI usage dashboard — recent calls per tenant.
CREATE INDEX idx_ai_interactions_tenant_created
    ON ai_interactions (tenant_id, created_at DESC);

-- Cost breakdown by AI capability.
CREATE INDEX idx_ai_interactions_type
    ON ai_interactions (tenant_id, interaction_type, created_at DESC);

-- One quota record per tenant per month.
CREATE UNIQUE INDEX idx_ai_quotas_tenant_period
    ON ai_usage_quotas (tenant_id, period_start);
