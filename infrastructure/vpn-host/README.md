# VPN Host Bootstrap

This directory contains bootstrap assets for a single-host `Ubuntu 24.04` deployment that runs:

- `strongSwan`
- `FreeRADIUS`
- `PostgreSQL`
- `VpnPortal.Api`
- static Angular SPA assets
- `nginx`

## Status

These scripts install and prepare the host, database roles, and service templates.

They now include a concrete `FreeRADIUS` policy template that reads `vpn_device_credentials.radius_nt_hash` from PostgreSQL for password-based device credentials.

They also include the current runtime helper layer for:

- accounting event forwarding
- best-effort runtime session disconnect on the VPN host

They still do not complete the full VPN integration by themselves, because runtime accounting, final device lifecycle enforcement, and production tuning still need to be validated on the target host.

Use these assets to make the server bootstrap reproducible and to establish the target operational layout.

After bootstrap, the standard `deploy.yml` workflow reapplies the repository version of the `strongSwan` configuration on each deploy when the matching host bootstrap env file exists under `/etc/vpnportal/`.

## Supported OS

- `Ubuntu 24.04`

## Files

- `env/vpn-host.prod.env.example` - example bootstrap environment variables for `prod`
- `env/vpn-host.stage.env.example` - example bootstrap environment variables for `stage`
- `deploy/predeploy/infrastructure/vpn-host/` - idempotent `bash` scripts for package installation and host setup
- `runbooks/` - server-side verification guides for the current runtime flow
- `postgresql/` - SQL assets for roles and manual superadmin creation example
- `strongswan/` - template configuration files
- `freeradius/` - template configuration files
- `tools/` - host-side helper scripts that should also be versioned in this repository

## Predeploy Order

Run these predeploy scripts in this order as `root`:

1. `deploy/predeploy/infrastructure/vpn-host/00-validate-env.sh`
2. `deploy/predeploy/infrastructure/vpn-host/01-install-packages.sh`
3. `deploy/predeploy/infrastructure/vpn-host/02-create-users-and-directories.sh`
4. `deploy/predeploy/infrastructure/vpn-host/03-install-and-init-postgres.sh`
5. `deploy/predeploy/infrastructure/vpn-host/04-configure-freeradius.sh`
6. `deploy/predeploy/infrastructure/vpn-host/05-configure-portal-host.sh`
7. `deploy/predeploy/infrastructure/vpn-host/06-verify-stack.sh`

There is no separate predeploy strongSwan configuration step. Predeploy only validates the bootstrap env in step `00` and installs the `strongSwan` packages as part of `deploy/predeploy/infrastructure/vpn-host/01-install-packages.sh`.

Post-deploy runtime verification lives in `deploy/host/verify-portal-runtime.sh` and should run only after the first application deploy has completed and the API is already reachable.

## Deploy-Time strongSwan Rollout

`deploy/host/apply-strongswan-config.sh` is a deploy-only host step. `deploy.yml` applies it from the current release snapshot when the matching `/etc/vpnportal/vpn-host.<target>.env` file exists.

Each script accepts an optional path to the bootstrap environment file. If omitted, it defaults to `/etc/vpnportal/vpn-host.prod.env`. Use an explicit path such as `/etc/vpnportal/vpn-host.stage.env` when bootstrapping `stage`.

Example:

```bash
sudo ./deploy/predeploy/infrastructure/vpn-host/00-validate-env.sh ./infrastructure/vpn-host/env/vpn-host.stage.env.example
```

## Manual Inputs

Fill a real bootstrap environment file before applying configurations. For the full per-step variable matrix, see `docs/predeploy/variables.md`.

- deployment target via `TARGET=prod|stage`, or use an env filename that lets the loader infer `stage` or `prod`
- public portal URL via `PUBLIC_BASE_URL`
- VPN endpoint via `VPN_SERVER_ADDRESS`
- PostgreSQL passwords
- RADIUS shared secret
- strongSwan certificate paths
- runtime email settings via `Email__Host`, `Email__Username`, `Email__Password`, and `Email__FromEmail`

The bootstrap loader derives the usual `prod`/`stage` values automatically. By default it computes:

- `ASPNETCORE_ENVIRONMENT`
- `Email__PublicBaseUrl` from `PUBLIC_BASE_URL`
- `INTERNAL_API_BASE_URL` from `PUBLIC_BASE_URL` unless overridden
- `VpnAccess__ServerAddress` from `VPN_SERVER_ADDRESS` unless overridden
- `STRONGSWAN_SERVER_ID` from `VPN_SERVER_ADDRESS` unless overridden
- `PORTAL_DEPLOY_ROOT`
- `PORTAL_ENV_DIR`
- `PORTAL_ENV_FILE`
- `POSTGRES_DB`
- `POSTGRES_APP_USER`
- `POSTGRES_RADIUS_USER`
- `RADIUS_CLIENT_ADDRESS`
- `VpnRuntime__DisconnectScriptPath`
- `Email__Port`
- `Email__FromName`

You only need to set the derived values explicitly when your host layout or naming differs from the defaults.

Required bootstrap variables:

- `TARGET`, or an env filename that lets the loader infer `stage` or `prod`
- `PUBLIC_BASE_URL`
- `VPN_SERVER_ADDRESS`
- `InternalApi__SharedSecret`
- `POSTGRES_APP_PASSWORD`
- `POSTGRES_RADIUS_PASSWORD`
- `RADIUS_SHARED_SECRET`
- `STRONGSWAN_CERT_PATH`
- `STRONGSWAN_KEY_PATH`
- `STRONGSWAN_RIGHT_SOURCE_IP`
- `STRONGSWAN_DNS`
- `Email__Host`
- `Email__Username`
- `Email__Password`
- `Email__FromEmail`

Optional overrides remain available for non-default layouts:

- `INTERNAL_API_BASE_URL`
- `PORTAL_DEPLOY_ROOT`
- `PORTAL_ENV_DIR`
- `PORTAL_ENV_FILE`
- `POSTGRES_DB`
- `POSTGRES_APP_USER`
- `POSTGRES_RADIUS_USER`
- `RADIUS_CLIENT_ADDRESS`
- `STRONGSWAN_SERVER_ID`
- `STRONGSWAN_CERT_PATH`
- `STRONGSWAN_KEY_PATH`
- `STRONGSWAN_CA_CERT_PATH`
- `STRONGSWAN_RIGHT_SOURCE_IP`
- `STRONGSWAN_DNS`
- `VpnAccess__ServerAddress`
- `VpnRuntime__DisconnectScriptPath`
- `Email__Port`
- `Email__FromName`

## Expected Host Layout

- `/etc/vpnportal/` - bootstrap env files and runtime container env files
- `/opt/vpnportal/` - application deployment root
- `/etc/strongswan.conf` and `/etc/ipsec.conf` - strongSwan configuration
- `/etc/freeradius/3.0/` - FreeRADIUS configuration
- `/usr/local/lib/vpnportal/forward-accounting-event.sh` - canonical accounting forwarder
- `/usr/local/lib/vpnportal/disconnect-session.sh` - best-effort runtime session disconnect helper
- `/usr/local/bin/vpn-speed.py` - optional helper for live VPN session throughput monitoring

## Current Runtime Flow

1. The portal issues a per-device VPN credential.
2. `strongSwan` passes IKEv2 password-based authentication to `FreeRADIUS`.
3. The first successful connection is allowed without a bound device IP; `FreeRADIUS` then enforces `active`, device-bound source IP, and `max_devices` policy against PostgreSQL.
4. `FreeRADIUS` forwards accounting events into the internal portal API.
5. The portal updates `vpn_sessions` and binds the first observed source IP to the device.
6. Later source-IP changes stay blocked until the user unbinds the old IP in the portal.
7. Admin disconnect can request best-effort runtime termination through the host-side disconnect helper.

## Schema And Admin Bootstrap

1. `deploy/predeploy/infrastructure/vpn-host/03-install-and-init-postgres.sh` creates roles and the database only.
2. Apply the schema with `dotnet run --project src/VpnPortal.Migrations`.
3. Create the first `superadmin` manually using `docs/runbooks/create-first-superadmin.md`.

## Operational Helpers

The repository also tracks small server-side helper scripts that are useful during live operations.

### `vpn-speed.py`

- Source in repo: `infrastructure/vpn-host/tools/vpn-speed.py`
- Target host path: `/usr/local/bin/vpn-speed.py`
- Run as: `sudo /usr/local/bin/vpn-speed.py`
- Deployment behavior: the standard remote deploy script replaces the host copy with the packaged repo version on each deploy.
- Purpose: render active `strongSwan` IKEv2 sessions with client IP, VPN IP, ping, RX/TX rate, total traffic, and uptime.

Example install on the server:

```bash
sudo install -m 0755 infrastructure/vpn-host/tools/vpn-speed.py /usr/local/bin/vpn-speed.py
```

## Validation Runbook

- Host predeploy verification: `deploy/predeploy/infrastructure/vpn-host/06-verify-stack.sh`
- Post-deploy portal runtime verification: `deploy/host/verify-portal-runtime.sh`
- End-to-end runtime validation: `runbooks/verify-vpn-runtime-flow.md`

## Verification

The final verification script checks:

- systemd service state
- PostgreSQL local connectivity
- `strongSwan` status
- `freeradius -CX` config validation
- installed runtime helpers for accounting, auth forwarding, and disconnect
- expected open sockets

## Security Notes

- Keep PostgreSQL on localhost unless you have a strong reason not to.
- Use separate database roles for the portal application and FreeRADIUS.
- Keep bootstrap env files and generated secrets out of git.
- Review backup copies under `/var/backups/vpnportal/` before overwriting service configs.
