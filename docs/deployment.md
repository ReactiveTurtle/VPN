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

Operational helper scripts that live on the VPN host should also be tracked under `infrastructure/vpn-host/tools/`. The current helper is `vpn-speed.py`, intended to be installed at `/usr/local/bin/vpn-speed.py`, updated from the packaged repository copy during each deploy, and run with `sudo /usr/local/bin/vpn-speed.py` for live VPN session throughput monitoring.

## Current Runtime Contract

The current target runtime flow on the host is:

- `strongSwan` terminates IKEv2
- `FreeRADIUS` validates password-based device credentials from PostgreSQL
- `FreeRADIUS` forwards blocked new-IP events into `POST /api/internal/radius/auth-events`
- `FreeRADIUS` forwards accounting events into `POST /api/internal/radius/accounting-events`
- the portal updates `vpn_sessions` and `ip_change_confirmations`
- admin disconnect can request best-effort runtime session teardown through `/usr/local/lib/vpnportal/disconnect-session.sh`

## Host-Installed Runtime Helpers

The bootstrap currently expects these host-local runtime helpers:

- `/usr/local/lib/vpnportal/forward-auth-event.sh`
- `/usr/local/lib/vpnportal/forward-accounting-event.sh`
- `/usr/local/lib/vpnportal/disconnect-session.sh`
- `/usr/local/bin/vpn-speed.py`

These helpers are versioned in the repository and should be treated as part of the deployment contract for the VPN host.

## Verification

Use these assets after bootstrap and deploy:

- `infrastructure/vpn-host/bootstrap/07-verify-stack.sh`
- `infrastructure/vpn-host/bootstrap/08-smoke-test-portal.sh`
- `infrastructure/vpn-host/runbooks/verify-vpn-runtime-flow.md`

## Schema Application

The API does not apply database schema changes on startup.

Apply schema changes explicitly by running the separate migration program before starting or upgrading the API:

- `dotnet run --project src/VpnPortal.Migrations`

After the schema exists, create the first `superadmin` manually using `docs/runbooks/create-first-superadmin.md`.

## PostgreSQL Bootstrap

The VPN host bootstrap explicitly installs and initializes `PostgreSQL` because the database is a shared dependency for:

- portal data
- audit data
- target FreeRADIUS policy data
- target VPN session/accounting data

The bootstrap creates separate database roles for the portal application and FreeRADIUS, but schema creation itself is performed by the migration program rather than the API or bootstrap shell script.

## Reference

See these references:

- `deploy/README.md` for API and SPA deployment packaging
- `infrastructure/vpn-host/README.md` for first-time host preparation
- `infrastructure/vpn-host/runbooks/verify-vpn-runtime-flow.md` for end-to-end runtime validation
- `docs/runbooks/create-first-superadmin.md` for first admin creation
