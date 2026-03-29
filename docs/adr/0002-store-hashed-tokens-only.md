# ADR 0002: Store Hashed Tokens Only

## Context

Activation and confirmation links rely on bearer-style tokens that may be emailed to users.

## Decision

Store token hashes in persistence and compare against hashed incoming token values.

## Consequences

- Raw tokens are not recoverable from the database.
- Token compromise through database read access is reduced.
- Token validation requires a hashing step in application services.
