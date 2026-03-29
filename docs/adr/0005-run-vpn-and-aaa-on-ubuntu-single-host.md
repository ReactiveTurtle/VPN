# ADR 0005: Run VPN and AAA on an Ubuntu Single Host

## Context

The project needs an operationally simple way to run the VPN endpoint, AAA layer, database, and portal while the product is still converging on its final VPN credential model.

The current repository already includes deployment assets for the API and SPA, but it does not yet contain first-class bootstrap assets for `strongSwan`, `FreeRADIUS`, and `PostgreSQL` on the VPN host.

## Decision

Use a single `Ubuntu 24.04` host for the initial production-oriented deployment of:

- `strongSwan`
- `FreeRADIUS`
- `PostgreSQL`
- `VpnPortal.Api`
- static Angular SPA assets
- `nginx`

Store the bootstrap scripts, environment templates, and service configuration templates for that host in the repository under `infrastructure/vpn-host/`.

## Consequences

- Initial deployment is easier to reproduce and document.
- Operational setup becomes versioned alongside the application and schema.
- The host has a larger blast radius than a multi-host deployment.
- Database, RADIUS, and portal secrets must be isolated carefully on the same machine.
- Future separation into dedicated hosts remains possible because the application and schema boundaries stay explicit.
