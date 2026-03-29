# ADR 0004: Separate Devices, IPs, and Sessions

## Context

The simplified schema in the original brief mixed device, IP, and session concerns into one table.

## Decision

Model trusted devices, trusted IPs, session history, and IP confirmation workflow as separate tables.

## Consequences

- Policy state is clearer and more extensible.
- Auditability and future RADIUS integrations are easier to support.
- Querying and operational logic require more repositories and joins, but the model is cleaner.
