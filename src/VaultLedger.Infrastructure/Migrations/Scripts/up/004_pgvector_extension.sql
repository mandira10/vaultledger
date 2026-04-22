-- 004_pgvector_extension.sql
-- Enables pgvector on this database. The binary ships with the pgvector/pgvector:pg16 image.

CREATE EXTENSION IF NOT EXISTS vector;
