-- 005_audit_entry_embeddings.down.sql

DROP INDEX IF EXISTS idx_audit_entry_embeddings_vector;
DROP TABLE IF EXISTS audit_entry_embeddings;
ALTER TABLE audit_entries DROP CONSTRAINT IF EXISTS audit_entries_tenant_id_key;
