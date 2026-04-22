-- 001_initial_schema.down.sql
-- Drop tables in reverse FK order so children go before parents.

DROP TABLE IF EXISTS ai_usage_quotas;
DROP TABLE IF EXISTS ai_interactions;
DROP TABLE IF EXISTS integration_events;
DROP TABLE IF EXISTS compliance_reviews;
DROP TABLE IF EXISTS audit_entries;
DROP TABLE IF EXISTS cases;
DROP TABLE IF EXISTS entities;
DROP TABLE IF EXISTS tenant_integrations;
DROP TABLE IF EXISTS tenant_memberships;
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS tenants;
