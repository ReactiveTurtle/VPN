# Create First Superadmin

The first `superadmin` is created manually.

## When To Use This

Use this runbook after:

1. PostgreSQL roles and database are created.
2. The EF migration program has already applied the schema.

## Required Data

- admin username
- Argon2id password hash

## Current Argon2id Parameters

The application currently verifies hashes generated with these defaults:

- algorithm: `argon2id`
- memory: `65536`
- iterations: `3`
- parallelism: `1`
- hash length: `32`
- salt length: `16`

## Insert Example

Run `psql` on the trusted server and insert the row manually:

```sql
INSERT INTO superadmins (username, password_hash, created_at)
VALUES ('replace-me', 'replace-with-argon2id-hash', NOW());
```

## Notes

- Do not create superadmins through the public portal.
- Do not store plaintext passwords in the repository.
- Keep this operation limited to trusted administrative access on the server.
