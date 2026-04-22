-- 005_audit_entry_embeddings.sql
-- Vector embeddings for semantic search. Stored in a separate table so they can be regenerated
-- (e.g. on model change) without touching the immutable audit_entries table.

-- Target for composite FK from audit_entry_embeddings — preserves tenant boundary.
ALTER TABLE audit_entries
    ADD CONSTRAINT audit_entries_tenant_id_key UNIQUE (tenant_id, id);

CREATE TABLE audit_entry_embeddings (
    audit_entry_id  UUID         NOT NULL,
    tenant_id       UUID         NOT NULL,
    embedding       VECTOR(1536) NOT NULL,
    model           VARCHAR(100) NOT NULL,
    created_at      TIMESTAMP    NOT NULL,
    PRIMARY KEY (audit_entry_id),
    CONSTRAINT audit_entry_embeddings_entry_fk
        FOREIGN KEY (tenant_id, audit_entry_id)
        REFERENCES audit_entries (tenant_id, id)
        ON DELETE CASCADE
);

-- Approximate-nearest-neighbour search via cosine distance.
-- lists=100 suits ~100K rows; tune upward as the table grows.
CREATE INDEX idx_audit_entry_embeddings_vector
    ON audit_entry_embeddings
    USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100);
