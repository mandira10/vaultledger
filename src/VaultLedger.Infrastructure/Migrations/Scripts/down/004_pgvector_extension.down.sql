-- 004_pgvector_extension.down.sql
-- Fails if any column still uses the VECTOR type — roll back 005 first.

DROP EXTENSION IF EXISTS vector;
