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
- `APP_DATABASE_CONNECTION_STRING`
- `APP_EMAIL_HOST`
- `APP_EMAIL_USERNAME`
- `APP_EMAIL_PASSWORD`
- `APP_EMAIL_FROM_EMAIL`
- `APP_EMAIL_FROM_NAME`
- `APP_EMAIL_PUBLIC_BASE_URL`
- `APP_INTERNAL_API_SHARED_SECRET`
- `APP_VPN_ACCESS_SERVER_ADDRESS`
- `APP_VPN_RUNTIME_DISCONNECT_SCRIPT_PATH`

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

You can automate most application-host setup steps with:

- `sudo ./deploy/predeploy/prepare-app-host.sh --target production --server-name vpn.example.com`

This helper installs base packages, prepares directories, installs the systemd unit, renders nginx config, and writes the example env file when missing.

If the same server also acts as the VPN host, you can additionally pass `--vpn-host-env /etc/vpnportal/vpn-host.env` to run the repository bootstrap flow for `strongSwan`, `FreeRADIUS`, and `PostgreSQL`.

It does not configure SSH access, configure GitHub deployment secrets, run schema migrations, or create the first `superadmin`.

## Runtime Configuration

The repository now keeps only the base `appsettings.json` files in git.

Environment-specific `appsettings.{Environment}.json` files are ignored by git and rendered during `deploy.yml` from GitHub environment secrets. These rendered files are included in the deployment package.

The checked-in files under `deploy/env/` now carry only process-level runtime variables such as `ASPNETCORE_ENVIRONMENT` and `ASPNETCORE_URLS`. They are no longer the source of truth for application secrets.

1. Install `.NET 10 runtime`, `nginx`, and `systemd` support.
2. Create deployment directory, for example `/opt/vpnportal`.
3. Copy `deploy/systemd/vpnportal-api.production.service` and/or `deploy/systemd/vpnportal-api.staging.service` to `/etc/systemd/system/` and adjust paths/user if needed.
4. Copy `deploy/nginx/vpnportal.conf` to your nginx sites config and update `server_name`.
5. Copy `deploy/env/vpnportal.production.env.example` or `deploy/env/vpnportal.staging.env.example` into `/etc/vpnportal/`.
6. Bootstrap the VPN host separately with `infrastructure/vpn-host/README.md` if this server also runs `strongSwan`, `FreeRADIUS`, and PostgreSQL.
7. Ensure `DEPLOY_PATH` already exists on the server and is writable by the deployment user so the uploaded package can be stored there.
8. Ensure `/usr/local/bin` is writable by `sudo install` from the packaged deploy script if you want packaged operational tools refreshed automatically.
9. Run the schema migration program before the first API start: `dotnet run --project src/VpnPortal.Migrations`.
10. Create the first `superadmin` manually using `docs/runbooks/create-first-superadmin.md`.
11. Enable the correct systemd service.

The packaged deploy script now creates target app roots such as `/opt/vpnportal` and `/opt/vpnportal-staging` together with their `releases/` directories when they do not exist yet.

## Workflow behavior

- `ci.yml` builds backend and frontend on push/PR.
- `deploy.yml` publishes the API, copies Angular build output to `wwwroot`, versions the package with a git tag or commit SHA, uploads a tarball to the server, extracts the packaged `deploy/deploy-package.sh` to a temporary location on the host, and runs it there.
- The packaged deploy script is responsible for unpacking the package, switching the current release, refreshing executable files from packaged `infrastructure/vpn-host/tools/` into `/usr/local/bin`, and restarting services.
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
