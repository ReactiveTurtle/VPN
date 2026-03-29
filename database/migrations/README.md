# Database Migrations

This directory contains incremental PostgreSQL schema migrations applied after `database/001_schema.sql`.

## Rules

- Name files with a zero-padded numeric prefix, for example `003_add_runtime_disconnect_audit.sql`.
- Keep migrations append-only.
- Do not rewrite or delete an already applied migration.
- Prefer idempotent PostgreSQL statements when practical.

## Execution Model

When `Database:Provider = PostgreSql` and `Database:InitializeOnStartup = true`, the application bootstrapper now:

1. Ensures `schema_migrations` exists.
2. Applies `database/001_schema.sql` once and records it as `001_schema.sql`.
3. Applies any `*.sql` files in this directory in filename order.
4. Optionally applies `database/002_seed_dev.sql` for local/demo data.

## Why This Exists

The repository currently uses Dapper and handwritten SQL repositories rather than an Entity Framework Core `DbContext` model.

Because the data access layer is already SQL-first, a lightweight SQL migration runner is the smallest way to make schema evolution reproducible without introducing a second schema source of truth.
