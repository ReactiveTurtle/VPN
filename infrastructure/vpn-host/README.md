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

They do not by themselves complete the final VPN authentication policy, because the application-side `per-device VPN credential` schema and API flow described in ADR `0006` and ADR `0007` still need to be implemented in code.

Use these assets to make the server bootstrap reproducible and to establish the target operational layout.

## Supported OS

- `Ubuntu 24.04`

## Files

- `env/vpn-host.env.example` - example environment variables consumed by bootstrap scripts
- `bootstrap/` - idempotent `bash` scripts for package installation and host setup
- `postgresql/` - SQL assets for roles and manual superadmin seeding
- `strongswan/` - template configuration files
- `freeradius/` - template configuration files

## Script Order

Run the scripts in this order as `root`:

1. `bootstrap/01-install-packages.sh`
2. `bootstrap/02-create-users-and-directories.sh`
3. `bootstrap/03-install-and-init-postgres.sh`
4. `bootstrap/04-configure-strongswan.sh`
5. `bootstrap/05-configure-freeradius.sh`
6. `bootstrap/06-configure-portal-host.sh`
7. `bootstrap/07-verify-stack.sh`

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

## Verification

The final verification script checks:

- systemd service state
- PostgreSQL local connectivity
- `strongSwan` status
- `freeradius -CX` config validation
- expected open sockets

## Security Notes

- Keep PostgreSQL on localhost unless you have a strong reason not to.
- Use separate database roles for the portal application and FreeRADIUS.
- Keep bootstrap env files and generated secrets out of git.
- Review backup copies under `/var/backups/vpnportal/` before overwriting service configs.
