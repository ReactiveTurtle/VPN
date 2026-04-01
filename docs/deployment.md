# Deployment

## Current Assets

Deployment assets already exist under `deploy/`.

- `deploy/docker/`
- `deploy/predeploy/nginx/`
- `deploy/predeploy/env/`
- `.github/workflows/`

VPN host bootstrap assets now also live under `infrastructure/vpn-host/`.

## Shape

- API is intended to run in Docker.
- `nginx` is expected to remain on the host and reverse proxy into the API container.
- Frontend build output is served from the API container.
- CI builds and pushes Docker images, then deploys them over SSH with `docker compose`.

## Predeploy

First-time preparation for a new machine is documented separately under `docs/predeploy/`.

Read these documents before the first deployment to a fresh host:

- `docs/predeploy/ssh-access.md`
- `docs/predeploy/new-host-checklist.md`

## Ubuntu 24.04 Single-Host Topology

The current target deployment model for `stage` and `prod` is one `Ubuntu 24.04` server running:

- `strongSwan`
- `FreeRADIUS`
- `PostgreSQL`
- `nginx`
- Docker Engine / Docker Compose plugin
- `VpnPortal.Api` container

Use `infrastructure/vpn-host/README.md` for the current target host layout and `deploy/predeploy/infrastructure/vpn-host/*.sh` for the repository's predeploy bootstrap scripts.

The portal application delivery assets under `deploy/` remain the source for container rollout, while `infrastructure/vpn-host/` covers host preparation and local service integration.

Operational helper scripts that live on the VPN host should also be tracked under `infrastructure/vpn-host/tools/`. The current helper is `vpn-speed.py`, intended to be installed at `/usr/local/bin/vpn-speed.py` and run with `sudo /usr/local/bin/vpn-speed.py` for live VPN session throughput monitoring.

## Current Runtime Contract

The current target runtime flow on the host is:

- `strongSwan` terminates IKEv2
- `FreeRADIUS` validates password-based device credentials from PostgreSQL
- `FreeRADIUS` forwards accounting events into `POST /api/internal/radius/accounting-events`
- the portal container updates `vpn_sessions` and auto-binds the first source IP to the device
- admin disconnect can request best-effort runtime session teardown through `/usr/local/lib/vpnportal/disconnect-session.sh`

## Host-Installed Runtime Helpers

The bootstrap currently expects these host-local runtime helpers:

- `/usr/local/lib/vpnportal/forward-accounting-event.sh`
- `/usr/local/lib/vpnportal/disconnect-session.sh`
- `/usr/local/bin/vpn-speed.py`

These helpers are versioned in the repository and should be treated as part of the deployment contract for the VPN host.

## Verification

Use these assets after bootstrap and deploy:

- `deploy/predeploy/infrastructure/vpn-host/07-verify-stack.sh`
- `deploy/predeploy/infrastructure/vpn-host/08-smoke-test-portal.sh`
- `infrastructure/vpn-host/runbooks/verify-vpn-runtime-flow.md`

## Schema Application

The API does not apply database schema changes on startup.

Apply schema changes explicitly by running the separate migration program before starting or upgrading the API:

- `dotnet run --project src/VpnPortal.Migrations`

After the schema exists, create the first `superadmin` manually using `docs/runbooks/create-first-superadmin.md`.

The same program can also generate an `Argon2id` password hash for the first `superadmin`:

- `dotnet run --project src/VpnPortal.Migrations -- hash-password "<plaintext>"`

## PostgreSQL Bootstrap

The VPN host bootstrap explicitly installs and initializes `PostgreSQL` because the database is a shared dependency for:

- portal data
- audit data
- target FreeRADIUS policy data
- target VPN session/accounting data

The bootstrap creates separate database roles for the portal application and FreeRADIUS, but schema creation itself is performed by the migration program rather than the API or bootstrap shell script.

## Reference

See these references:

- `deploy/README.md` for Docker image rollout and host preparation
- `infrastructure/vpn-host/README.md` for first-time host preparation
- `infrastructure/vpn-host/runbooks/verify-vpn-runtime-flow.md` for end-to-end runtime validation
- `docs/runbooks/create-first-superadmin.md` for first admin creation
