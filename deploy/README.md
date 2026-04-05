# Deployment Notes

This repository archives a source snapshot in GitHub Actions and deploys it over SSH with `docker compose build` on the target host.

## GitHub environments

Create two GitHub Environments if you want separated deployments:

- `stage`
- `prod`

Store environment-specific secrets in each environment.

## Required GitHub Environment Secrets

Configure these secrets separately in each GitHub Environment.

For example:

- `stage` should point `DEPLOY_HOST`, `DEPLOY_PATH`, and the application secrets at the staging server
- `prod` should point `DEPLOY_HOST`, `DEPLOY_PATH`, and the application secrets at the production server

Recommended `stage` deploy secret values:

- `DEPLOY_HOST=<staging-host-or-ip>`
- `DEPLOY_PORT=<staging-ssh-port>`
- `DEPLOY_USER=deploy`
- `DEPLOY_PATH=/opt/vpnportal-stage`
- `DEPLOY_SSH_PRIVATE_KEY=<private key for the stage deploy user>`

`DEPLOY_COMMAND` is not used by the current `deploy.yml` workflow and does not need to be configured.

Important:

- save `DEPLOY_PATH` without trailing whitespace or hidden line-ending characters
- a stray `\r`, newline, or trailing space in the GitHub Environment secret can make the first `scp` upload fail with `dest open ... No such file or directory`, even when the target directory already exists on the host

- `DEPLOY_HOST`
- `DEPLOY_PORT`
- `DEPLOY_USER`
- `DEPLOY_PATH`
- `DEPLOY_SSH_PRIVATE_KEY`
- `DATABASE__CONNECTIONSTRING`
- `EMAIL__HOST`
- `EMAIL__USERNAME`
- `EMAIL__PASSWORD`
- `EMAIL__FROMEMAIL`
- `EMAIL__FROMNAME`
- `EMAIL__PUBLICBASEURL`
- `INTERNALAPI__SHAREDSECRET`
- `VPNACCESS__SERVERADDRESS`
- `VPNRUNTIME__DISCONNECTSCRIPTPATH`

Recommended:

- use `stage` secrets for branch deployments
- use `prod` secrets for tag/manual production deployments

## Target host layout

- app root: `/opt/vpnportal`
- compose file: `/opt/vpnportal/docker-compose.yml`
- app predeploy env file: `/etc/vpnportal/predeploy.prod.env` or `/etc/vpnportal/predeploy.stage.env`
- runtime env file: `/etc/vpnportal/vpnportal.prod.container.env`
- container loopback port: `127.0.0.1:5000` for prod, `127.0.0.1:5001` for stage
- deploy-managed operational tools: `/usr/local/bin`
- bootstrap-managed VPN runtime helpers: `/usr/local/lib/vpnportal`

## First-time server prep

You can automate most application-host setup steps with:

- `sudo /opt/vpnportal/predeploy/prepare-app-host.sh --predeploy-env /etc/vpnportal/predeploy.prod.env`

This helper installs base packages, including Docker Engine, Docker Compose plugin, and nginx, prepares directories, adds the deploy user from the predeploy env file to the `docker` group, renders nginx config, and writes the example container env file when missing.

If the same server also acts as the VPN host, `prepare-app-host.sh` automatically requires the matching `/etc/vpnportal/vpn-host.prod.env` or `/etc/vpnportal/vpn-host.stage.env` file for the selected predeploy environment and runs the repository bootstrap flow for `strongSwan`, `FreeRADIUS`, and `PostgreSQL`.

`prepare-app-host.sh` now reads `/etc/vpnportal/predeploy.prod.env` or `/etc/vpnportal/predeploy.stage.env` as the host-side source of truth for app predeploy settings such as `DEPLOY_PATH`, `NGINX_PORTAL_SERVER_NAME`, `RUNTIME_ENV_FILE`, `NGINX_SITE_NAME`, and `APP_PORT`.

If only one of those files exists on the host, `prepare-app-host.sh` can auto-detect it without `--predeploy-env`. If both files exist, pass `--predeploy-env` explicitly.

It does not configure SSH access, configure GitHub deployment secrets, run schema migrations outside the deploy workflow, or create the first `superadmin`.

## Runtime Configuration

The repository keeps only the base `appsettings.json` files in git.

Environment-specific runtime values are rendered from GitHub Environment Secrets during `deploy.yml` into a host-side container env file under `/etc/vpnportal/`.

The checked-in files under `deploy/predeploy/env/*.container.env.example` are templates only. They are not the source of truth for production secrets.

The checked-in files under `deploy/predeploy/env/predeploy.*.env.example` document the required host-side predeploy shape for first-time manual setup. The regular deploy workflow refreshes `/etc/vpnportal/predeploy.<env>.env` on the host and currently sets `NGINX_PORTAL_SERVER_NAME` from `DEPLOY_HOST`.

Runtime application secrets in GitHub Environment Secrets should use uppercase names, such as `DATABASE__CONNECTIONSTRING`, `EMAIL__PASSWORD`, `INTERNALAPI__SHAREDSECRET`, and `VPNACCESS__SERVERADDRESS`. During deploy, the workflow materializes them into the host-side container env file using the runtime .NET configuration keys like `Database__ConnectionString` and `Email__Password`:

- `/etc/vpnportal/vpnportal.prod.container.env`
- `/etc/vpnportal/vpnportal.stage.container.env`

The rendered host-side env file also sets `ASPNETCORE_ENVIRONMENT` explicitly to `prod` or `stage` to match the deployment target names.

1. Install `Docker Engine`, `Docker Compose plugin`, `nginx`, and `systemd` support.
2. Create deployment directory, for example `/opt/vpnportal`.
3. Copy `deploy/predeploy/env/predeploy.prod.env.example` or `deploy/predeploy/env/predeploy.stage.env.example` into `/etc/vpnportal/predeploy.<env>.env` and fill the real host-level values if you are preparing the server before the first workflow-managed upload.
4. Run `prepare-app-host.sh` or `prepare-app-host.sh --predeploy-env /etc/vpnportal/predeploy.<env>.env` so the server layout matches the same host-level values that deploy will use later.
5. Copy `deploy/docker/docker-compose.yml` into `DEPLOY_PATH` only if you are preparing the server fully by hand before the workflow starts managing releases.
6. Bootstrap the VPN host separately with `infrastructure/vpn-host/README.md` and the documented predeploy scripts if this server also runs `strongSwan`, `FreeRADIUS`, and PostgreSQL. `strongSwan` config rollout itself is handled later by `deploy/host/apply-strongswan-config.sh` during `deploy.yml`.
7. Ensure `DEPLOY_PATH` already exists on the server and is writable by the deployment user.
8. Ensure the application database connection string uses `host.docker.internal` instead of `localhost` if PostgreSQL remains on the host.
9. Run the schema migration program before the first API start: `dotnet run --project src/VpnPortal.Migrations`.
10. Create the first `superadmin` manually using `docs/runbooks/create-first-superadmin.md`.

If the deploy user was just added to the `docker` group, re-login before running Docker commands under that account.

## Workflow behavior

- `ci.yml` builds backend and frontend on push/PR.
- `deploy.yml` archives the current repository source, uploads the source snapshot to the target host, refreshes `/etc/vpnportal/predeploy.<env>.env`, renders the runtime env file from GitHub Environment Secrets, installs that file under `/etc/vpnportal/`, and then runs `docker compose build` remotely.
- When `/etc/vpnportal/vpn-host.stage.env` or `/etc/vpnportal/vpn-host.prod.env` exists for the current target, `deploy.yml` runs `deploy/host/apply-strongswan-config.sh` to reapply the repository version of the `strongSwan` configuration before updating the API container.
- `docker compose run --rm migrations` applies schema changes before `docker compose up -d api` updates the application container.
- After the API starts, `deploy.yml` runs `deploy/host/verify-portal-runtime.sh` when the matching `/etc/vpnportal/vpn-host.<env>.env` file exists.
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
