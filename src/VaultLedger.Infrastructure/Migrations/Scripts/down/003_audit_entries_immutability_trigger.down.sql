-- 003_audit_entries_immutability_trigger.down.sql

DROP TRIGGER IF EXISTS audit_entries_no_update_delete ON audit_entries;
DROP FUNCTION IF EXISTS prevent_audit_entry_modification();
