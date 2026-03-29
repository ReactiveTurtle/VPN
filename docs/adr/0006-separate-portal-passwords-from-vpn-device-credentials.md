# ADR 0006: Separate Portal Passwords from VPN Device Credentials

## Context

The portal uses password-based browser authentication and stores user passwords with `Argon2id`.

The planned VPN path uses `strongSwan` with password-based access delegated through `FreeRADIUS`. That path should not depend directly on the same stored password representation that the portal uses for browser login.

Using one user-facing password for both portal login and VPN access is acceptable, but reusing one stored verifier for both flows is not.

## Decision

Keep one password for portal login and store it only as an `Argon2id` hash.

Do not use the portal password record directly for VPN authentication.

Model VPN access as a separate credential domain with per-device VPN credentials managed independently from the portal password.

## Consequences

- The portal can keep a modern password hashing model without forcing the VPN path to reuse that verifier directly.
- Password changes in the portal and lifecycle changes in VPN credentials become separate operations.
- The system avoids a single shared password artifact being treated as both a browser secret and a device access secret.
- Additional schema, API, and RADIUS integration work is required to manage the VPN credential lifecycle.
