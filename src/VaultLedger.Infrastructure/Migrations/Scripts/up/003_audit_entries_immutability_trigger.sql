-- 003_audit_entries_immutability_trigger.sql
-- DB-level enforcement that audit_entries is append-only.
-- Complements: domain (no mutators), application (no update command), API (no PUT/DELETE route).
-- Does NOT cover TRUNCATE or DROP TABLE — those are schema changes reviewed via migration scripts.

CREATE OR REPLACE FUNCTION prevent_audit_entry_modification()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'audit_entries is append-only — UPDATE/DELETE not permitted';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER audit_entries_no_update_delete
    BEFORE UPDATE OR DELETE ON audit_entries
    FOR EACH ROW
    EXECUTE FUNCTION prevent_audit_entry_modification();
