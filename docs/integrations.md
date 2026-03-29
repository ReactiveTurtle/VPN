# Integrations

## strongSwan

- Intended as the IKEv2 VPN endpoint.
- Expected to delegate authentication and accounting to FreeRADIUS.

## FreeRADIUS

- Intended as the AAA layer.
- Expected to validate credentials and enforce trust/device policy through PostgreSQL-backed data.

## PostgreSQL

- Source of truth for portal state and VPN policy data.
- Used by the application directly and expected to support VPN-side lookups.

## Email

- SMTP is the production-oriented delivery path.
- Console email service exists as a fallback for development and simplified environments.

## Integration Status

- Portal-side persistence and workflow logic are present.
- External VPN and AAA server configuration is still an operational integration concern outside this codebase.
