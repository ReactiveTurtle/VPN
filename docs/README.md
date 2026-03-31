# VPN Portal Documentation

This directory contains project documentation and architecture decision records for the VPN portal.

## Documents

- `product-overview.md` - product scope, roles, and core workflows
- `architecture.md` - current system architecture and implementation shape
- `backend.md` - backend layers, services, and API responsibilities
- `frontend.md` - frontend structure and current UX surface
- `database.md` - database schema and entity responsibilities
- `security.md` - security controls and known constraints
- `integrations.md` - strongSwan, FreeRADIUS, PostgreSQL, SMTP integration boundaries
- `deployment.md` - deployment topology and operational notes
- `predeploy/` - prerequisites and first-time host preparation before first deployment
- `roadmap.md` - implemented vs missing capabilities relative to the brief
- `adr/` - architecture decision records

Repository-level infrastructure bootstrap assets for the VPN host live under `infrastructure/vpn-host/`.

## Documentation Rules

- Document the current repository state first.
- When target-state design differs from implemented code, state that explicitly.
- Keep ADRs in `docs/adr/` and use them for durable technical decisions.
