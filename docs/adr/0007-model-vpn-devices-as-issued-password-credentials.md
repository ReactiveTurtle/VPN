# ADR 0007: Model VPN Devices as Issued Password Credentials

## Context

The product brief requires device limits, device revocation, and device-aware VPN access control.

With password-based `IKEv2` access, native clients do not provide a reliable hardware fingerprint that the portal can safely treat as the canonical identity of a device during first connection.

An auto-bind approach based only on a shared username and password is too weak to represent a durable device identity.

## Decision

Represent a VPN device in the MVP and initial production design as an issued password-based device credential.

Each active VPN device receives its own credential lifecycle:

- issue
- reveal once to the user
- rotate
- revoke

Count active device credentials against `max_devices`.

Treat device revocation as revocation of that issued VPN credential.

## Consequences

- Device lifecycle becomes explicit even before certificate-based VPN access is introduced.
- User onboarding requires a portal step to issue a device credential before VPN use.
- FreeRADIUS integration can reason about a concrete device credential instead of inferring device identity from first login attempts.
- Future migration to certificates can preserve the same device lifecycle concepts.
