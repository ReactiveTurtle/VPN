# Deployment Notes

This repository is packaged by GitHub Actions and deployed over SSH.

## GitHub environments

Create two GitHub Environments if you want separated deployments:

- `staging`
- `production`

Store environment-specific secrets in each environment.

## Required GitHub Secrets

- `DEPLOY_HOST`
- `DEPLOY_PORT`
- `DEPLOY_USER`
- `DEPLOY_PATH`
- `DEPLOY_SSH_PRIVATE_KEY`
- `DEPLOY_COMMAND`

Recommended:

- use `staging` secrets for branch deployments
- use `production` secrets for tag/manual production deployments

## Target host layout

- app root: `/opt/vpnportal`
- API files: `/opt/vpnportal/current/api`
- static SPA: `/opt/vpnportal/current/api/wwwroot`
- deploy-managed operational tools: `/usr/local/bin`
- bootstrap-managed VPN runtime helpers: `/usr/local/lib/vpnportal`

## First-time server prep

1. Install `.NET 10 runtime`, `nginx`, and `systemd` support.
2. Create deployment directory, for example `/opt/vpnportal`.
3. Copy `deploy/systemd/vpnportal-api.production.service` and/or `deploy/systemd/vpnportal-api.staging.service` to `/etc/systemd/system/` and adjust paths/user if needed.
4. Copy `deploy/nginx/vpnportal.conf` to your nginx sites config and update `server_name`.
5. Copy `deploy/env/vpnportal.production.env.example` or `deploy/env/vpnportal.staging.env.example` into `/etc/vpnportal/` and fill real values.
6. Bootstrap the VPN host separately with `infrastructure/vpn-host/README.md` if this server also runs `strongSwan`, `FreeRADIUS`, and PostgreSQL.
7. Install `deploy/remote/deploy-package.sh` on the server, for example at `/opt/vpnportal/bin/deploy-package.sh`, and make it executable.
8. Ensure `/usr/local/bin` is writable by `sudo install` from the deploy command if you want packaged operational tools refreshed automatically.
9. Run the schema migration program before the first API start: `dotnet run --project src/VpnPortal.Migrations`.
10. Create the first `superadmin` manually using `docs/runbooks/create-first-superadmin.md`.
11. Configure `DEPLOY_COMMAND`, for example `/opt/vpnportal/bin/deploy-package.sh`.
12. Enable the correct systemd service.

## Workflow behavior

- `ci.yml` builds backend and frontend on push/PR.
- `deploy.yml` publishes the API, copies Angular build output to `wwwroot`, versions the package with a git tag or commit SHA, uploads a tarball to the server, and then calls your remote deployment command.
- The remote command is responsible for unpacking the package, switching the current release, refreshing executable files from packaged `infrastructure/vpn-host/tools/` into `/usr/local/bin`, and restarting services.
- Runtime helpers under `/usr/local/lib/vpnportal` are not refreshed by the remote deploy script; they are managed by the VPN host bootstrap scripts under `infrastructure/vpn-host/bootstrap/`.
- Database schema changes are applied separately through `VpnPortal.Migrations`; the deploy script does not migrate the database automatically.

## Current Packaging Scope

The deployment package currently includes:

- published API output
- built Angular SPA output copied into `wwwroot`
- `deploy/` assets for systemd/nginx/env/remote deploy
- `infrastructure/vpn-host/tools/` for deploy-managed operational tools such as `vpn-speed.py`

The package does not replace bootstrap-managed runtime helpers such as:

- `/usr/local/lib/vpnportal/forward-accounting-event.sh`
- `/usr/local/lib/vpnportal/disconnect-session.sh`

## Deployment strategy

- push to `main` or `master` -> package and deploy to `staging`
- push tag like `v1.2.0` -> package and deploy to `production`
- manual `workflow_dispatch` -> choose `staging` or `production`
