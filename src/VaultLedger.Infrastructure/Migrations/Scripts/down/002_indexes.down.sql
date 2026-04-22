-- 002_indexes.down.sql

DROP INDEX IF EXISTS idx_memberships_user;
DROP INDEX IF EXISTS idx_cases_inbox;
DROP INDEX IF EXISTS idx_cases_entity;
DROP INDEX IF EXISTS idx_entries_case;
DROP INDEX IF EXISTS idx_reviews_status;
DROP INDEX IF EXISTS idx_events_pending;
DROP INDEX IF EXISTS idx_ai_interactions_tenant_created;
DROP INDEX IF EXISTS idx_ai_interactions_type;
DROP INDEX IF EXISTS idx_ai_quotas_tenant_period;
