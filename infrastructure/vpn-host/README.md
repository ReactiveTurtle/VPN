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

## Supported OS

- `Ubuntu 24.04`

## Files

- `env/vpn-host.env.example` - example environment variables consumed by bootstrap scripts
- `bootstrap/` - idempotent `bash` scripts for package installation and host setup
- `runbooks/` - server-side verification guides for the current runtime flow
- `postgresql/` - SQL assets for roles and manual superadmin creation example
- `strongswan/` - template configuration files
- `freeradius/` - template configuration files
- `tools/` - host-side helper scripts that should also be versioned in this repository

## Script Order

Run the scripts in this order as `root`:

1. `bootstrap/01-install-packages.sh`
2. `bootstrap/02-create-users-and-directories.sh`
3. `bootstrap/03-install-and-init-postgres.sh`
4. `bootstrap/04-configure-strongswan.sh`
5. `bootstrap/05-configure-freeradius.sh`
6. `bootstrap/06-configure-portal-host.sh`
7. `bootstrap/07-verify-stack.sh`
8. `bootstrap/08-smoke-test-portal.sh`

Each script accepts an optional path to the environment file. If omitted, it defaults to `/etc/vpnportal/vpn-host.env`.

Example:

```bash
sudo ./infrastructure/vpn-host/bootstrap/01-install-packages.sh ./infrastructure/vpn-host/env/vpn-host.env.example
```

## Manual Inputs

Fill a real environment file before applying configurations:

- host public IP or DNS name
- portal public URL
- PostgreSQL passwords
- RADIUS shared secret
- strongSwan certificate paths
- SMTP settings for the portal env file

## Expected Host Layout

- `/etc/vpnportal/` - portal and bootstrap env files
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

1. `bootstrap/03-install-and-init-postgres.sh` creates roles and the database only.
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

- Non-destructive smoke check: `bootstrap/08-smoke-test-portal.sh`
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
