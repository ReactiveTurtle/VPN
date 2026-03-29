# Deployment

## Current Assets

Deployment assets already exist under `deploy/`.

- `deploy/systemd/`
- `deploy/nginx/`
- `deploy/env/`
- `deploy/remote/`
- `.github/workflows/`

VPN host bootstrap assets now also live under `infrastructure/vpn-host/`.

## Shape

- API is intended to run on Linux under `systemd`.
- `nginx` is expected to front the application.
- Frontend build output is served as static files from the API host.
- CI and deploy workflows package and ship the application.

## Ubuntu 24.04 Single-Host Topology

The current target deployment for the first production-oriented stage is one `Ubuntu 24.04` server running:

- `strongSwan`
- `FreeRADIUS`
- `PostgreSQL`
- `VpnPortal.Api`
- static SPA assets
- `nginx`

Use `infrastructure/vpn-host/README.md` and the `bootstrap/*.sh` scripts to prepare that host.

The portal application delivery assets under `deploy/` remain the source for packaging and release rollout, while `infrastructure/vpn-host/` covers host preparation and local service integration.

## PostgreSQL Bootstrap

The VPN host bootstrap explicitly installs and initializes `PostgreSQL` because the database is a shared dependency for:

- portal data
- audit data
- target FreeRADIUS policy data
- target VPN session/accounting data

The bootstrap creates separate database roles for the portal application and FreeRADIUS.

## Reference

See these references:

- `deploy/README.md` for API and SPA deployment packaging
- `infrastructure/vpn-host/README.md` for first-time host preparation
