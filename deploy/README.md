# Deployment Notes

This repository archives a source snapshot in GitHub Actions and deploys it over SSH with `docker compose build` on the target host.

## GitHub environments

Create two GitHub Environments if you want separated deployments:

- `stage`
- `prod`

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

- use `stage` secrets for branch deployments
- use `prod` secrets for tag/manual production deployments

## Target host layout

- app root: `/opt/vpnportal`
- compose file: `/opt/vpnportal/docker-compose.yml`
- runtime env file: `/etc/vpnportal/vpnportal.prod.container.env`
- container loopback port: `127.0.0.1:5000` for prod, `127.0.0.1:5001` for stage
- deploy-managed operational tools: `/usr/local/bin`
- bootstrap-managed VPN runtime helpers: `/usr/local/lib/vpnportal`

## First-time server prep

You can automate most application-host setup steps with:

- `sudo /opt/vpnportal/predeploy/prepare-app-host.sh --target prod --server-name vpn.example.com`

This helper installs base packages, including Docker Engine, Docker Compose plugin, and nginx, prepares directories, adds the deploy user to the `docker` group, renders nginx config, and writes the example container env file when missing.

If the same server also acts as the VPN host, you can additionally pass `--vpn-host-env /etc/vpnportal/vpn-host.env` to run the repository bootstrap flow for `strongSwan`, `FreeRADIUS`, and `PostgreSQL`.

It does not configure SSH access, configure GitHub deployment secrets, run schema migrations outside the deploy workflow, or create the first `superadmin`.

## Runtime Configuration

The repository keeps only the base `appsettings.json` files in git.

Environment-specific runtime values are rendered from GitHub Environment Secrets during `deploy.yml` into a host-side container env file under `/etc/vpnportal/`.

The checked-in files under `deploy/predeploy/env/*.container.env.example` are templates only. They are not the source of truth for production secrets.

Runtime application secrets such as database credentials, SMTP credentials, `InternalApi__SharedSecret`, and `VpnAccess__ServerAddress` should be stored in GitHub Environment Secrets and materialized onto the host-side container env file during deploy:

- `/etc/vpnportal/vpnportal.prod.container.env`
- `/etc/vpnportal/vpnportal.stage.container.env`

1. Install `Docker Engine`, `Docker Compose plugin`, `nginx`, and `systemd` support.
2. Create deployment directory, for example `/opt/vpnportal`.
3. Copy `deploy/predeploy/nginx/vpnportal.conf` to your nginx sites config and update `server_name`.
4. Copy `deploy/predeploy/env/vpnportal.prod.container.env.example` or `deploy/predeploy/env/vpnportal.stage.container.env.example` into `/etc/vpnportal/`.
5. Copy `deploy/docker/docker-compose.yml` into `DEPLOY_PATH`.
6. Bootstrap the VPN host separately with `infrastructure/vpn-host/README.md` and `deploy/predeploy/infrastructure/vpn-host/*.sh` if this server also runs `strongSwan`, `FreeRADIUS`, and PostgreSQL.
7. Ensure `DEPLOY_PATH` already exists on the server and is writable by the deployment user.
8. Ensure the application database connection string uses `host.docker.internal` instead of `localhost` if PostgreSQL remains on the host.
9. Run the schema migration program before the first API start: `dotnet run --project src/VpnPortal.Migrations`.
10. Create the first `superadmin` manually using `docs/runbooks/create-first-superadmin.md`.

If the deploy user was just added to the `docker` group, re-login before running Docker commands under that account.

## Workflow behavior

- `ci.yml` builds backend and frontend on push/PR.
- `deploy.yml` archives the current repository source, uploads the source snapshot to the target host, renders a runtime env file from GitHub Environment Secrets, installs that file under `/etc/vpnportal/`, and then runs `docker compose build` remotely.
- `docker compose run --rm migrations` applies schema changes before `docker compose up -d api` updates the application container.
- Runtime helpers under `/usr/local/lib/vpnportal` remain host-managed and are mounted into the app container read-only.

## Current Image Scope

The Docker rollout currently includes:

- host-built `vpnportal-api` image with published API output and built Angular SPA copied into `wwwroot`
- host-built `vpnportal-migrations` image for explicit schema application
- `deploy/docker/docker-compose.yml` on the host

The Docker rollout does not replace bootstrap-managed runtime helpers such as:

- `/usr/local/lib/vpnportal/forward-accounting-event.sh`
- `/usr/local/lib/vpnportal/disconnect-session.sh`

## Deployment strategy

- push to `main` or `master` -> build images and deploy to `stage`
- push tag like `v1.2.0` -> build images and deploy to `prod`
- manual `workflow_dispatch` -> choose `stage` or `prod`
