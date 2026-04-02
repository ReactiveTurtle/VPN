#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

TARGET="prod"
SERVER_NAME="_"
INSTALL_PACKAGES=1
ENABLE_NGINX=1
VPN_HOST_ENV_FILE=""
DEPLOY_USER="deploy"

usage() {
    cat <<'EOF'
Usage:
  sudo ./deploy/predeploy/prepare-app-host.sh [options]

Options:
  --target <prod|stage>          Deployment target. Default: prod
  --server-name <value>          nginx server_name value. Default: _
  --deploy-user <value>          User to add to docker group. Default: deploy
  --skip-packages                Do not install nginx or Docker packages
  --skip-enable-nginx            Do not enable/restart nginx
  --vpn-host-env <path>          Run infrastructure/vpn-host bootstrap with this env file
  --help                         Show this help

What the script does:
  - installs nginx, Docker Engine, and Docker Compose plugin on Ubuntu via apt
  - creates deployment directories under /opt and /etc/vpnportal
  - creates the target container env file from the example if it does not exist yet
  - renders nginx config with the target root and upstream port
  - optionally validates and runs the repository VPN host bootstrap flow

What the script does not do:
  - configure SSH access
  - inject GitHub deployment secrets into the Docker runtime env file
  - run database migrations
  - create the first superadmin
  - bootstrap strongSwan, FreeRADIUS, or PostgreSQL unless --vpn-host-env is provided
EOF
}

require_root() {
    if [[ "${EUID}" -ne 0 ]]; then
        printf 'Run this script as root.\n' >&2
        exit 1
    fi
}

log_step() {
    printf '\n==> %s\n' "$1"
}

escape_sed_replacement() {
    printf '%s' "$1" | sed -e 's/[\\&]/\\&/g'
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --target)
            TARGET="${2:?target value is required}"
            shift 2
            ;;
        --server-name)
            SERVER_NAME="${2:?server name value is required}"
            shift 2
            ;;
        --deploy-user)
            DEPLOY_USER="${2:?deploy user value is required}"
            shift 2
            ;;
        --skip-packages)
            INSTALL_PACKAGES=0
            shift
            ;;
        --skip-enable-nginx)
            ENABLE_NGINX=0
            shift
            ;;
        --vpn-host-env)
            VPN_HOST_ENV_FILE="${2:?vpn host env path is required}"
            shift 2
            ;;
        --help)
            usage
            exit 0
            ;;
        *)
            printf 'Unknown argument: %s\n' "$1" >&2
            usage >&2
            exit 1
            ;;
    esac
done

case "$TARGET" in
    prod)
        APP_ROOT="/opt/vpnportal"
        ENV_EXAMPLE="${SCRIPT_DIR}/env/vpnportal.prod.container.env.example"
        ENV_FILE="/etc/vpnportal/vpnportal.prod.container.env"
        NGINX_SITE_NAME="vpnportal.conf"
        APP_PORT="5000"
        ;;
    stage)
        APP_ROOT="/opt/vpnportal-stage"
        ENV_EXAMPLE="${SCRIPT_DIR}/env/vpnportal.stage.container.env.example"
        ENV_FILE="/etc/vpnportal/vpnportal.stage.container.env"
        NGINX_SITE_NAME="vpnportal-stage.conf"
        APP_PORT="5001"
        ;;
    *)
        printf 'Unsupported target: %s\n' "$TARGET" >&2
        exit 1
        ;;
esac

NGINX_AVAILABLE_PATH="/etc/nginx/sites-available/${NGINX_SITE_NAME}"
NGINX_ENABLED_PATH="/etc/nginx/sites-enabled/${NGINX_SITE_NAME}"

require_root

install_packages() {
    local version_codename docker_keyring docker_repo

    log_step "Installing required apt packages"
    apt-get update
    apt-get install -y ca-certificates curl gnupg nginx

    version_codename="$(. /etc/os-release && printf '%s' "${VERSION_CODENAME}")"
    docker_keyring="/etc/apt/keyrings/docker.asc"
    docker_repo="/etc/apt/sources.list.d/docker.list"

    install -d -m 0755 /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o "$docker_keyring"
    chmod a+r "$docker_keyring"

    printf 'deb [arch=%s signed-by=%s] https://download.docker.com/linux/ubuntu %s stable\n' \
        "$(dpkg --print-architecture)" \
        "$docker_keyring" \
        "$version_codename" > "$docker_repo"

    apt-get update
    apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

    systemctl enable docker
    systemctl start docker

    if id -u "$DEPLOY_USER" >/dev/null 2>&1; then
        usermod -aG docker "$DEPLOY_USER"
    else
        printf 'Deploy user %s does not exist yet, skipping docker group membership.\n' "$DEPLOY_USER"
    fi
}

create_directories() {
    log_step "Creating application directories"
    install -d -m 0755 /etc/vpnportal
    install -d -m 0755 "${APP_ROOT}"
    install -d -m 0755 /usr/local/bin
    install -d -m 0755 /usr/local/lib/vpnportal
}

install_env_file() {
    log_step "Preparing container environment file"

    if [[ -f "${ENV_FILE}" ]]; then
        printf 'Keeping existing env file: %s\n' "${ENV_FILE}"
        return
    fi

    install -m 0640 "${ENV_EXAMPLE}" "${ENV_FILE}"
    printf 'Created env file from example: %s\n' "${ENV_FILE}"
    printf 'Fill real Docker runtime values before the first application rollout.\n'
}

install_nginx_site() {
    local escaped_server_name escaped_proxy_pass temp_file

    log_step "Rendering nginx config"
    escaped_server_name="$(escape_sed_replacement "${SERVER_NAME}")"
    escaped_proxy_pass="$(escape_sed_replacement "http://127.0.0.1:${APP_PORT}")"
    temp_file="$(mktemp)"

    sed \
        -e "s|server_name _;|server_name ${escaped_server_name};|" \
        -e "s|proxy_pass http://127.0.0.1:5000;|proxy_pass ${escaped_proxy_pass};|" \
        "${SCRIPT_DIR}/nginx/vpnportal.conf" > "${temp_file}"

    install -m 0644 "${temp_file}" "${NGINX_AVAILABLE_PATH}"
    rm -f "${temp_file}"
    ln -sfn "${NGINX_AVAILABLE_PATH}" "${NGINX_ENABLED_PATH}"

    nginx -t

    if [[ "${ENABLE_NGINX}" -eq 1 ]]; then
        systemctl enable nginx
        systemctl restart nginx
    fi
}

print_summary() {
    local vpn_host_note="not requested"

    if [[ -n "${VPN_HOST_ENV_FILE}" ]]; then
        vpn_host_note="completed with ${VPN_HOST_ENV_FILE}"
    fi

    cat <<EOF

Preparation complete.

Target: ${TARGET}
App root: ${APP_ROOT}
Env file: ${ENV_FILE}
nginx site: ${NGINX_AVAILABLE_PATH}
VPN host bootstrap: ${vpn_host_note}

Next manual steps:
  1. Configure SSH access for the deployment user.
  2. Configure GitHub Environment Secrets so deploy can render ${ENV_FILE}.
  3. Re-login as ${DEPLOY_USER} or run 'newgrp docker' so docker group membership takes effect.
  4. Ensure DEPLOY_PATH points to ${APP_ROOT} or another writable upload directory on the server.
  5. Run the Docker-based deployment workflow to pull and start the containers.
  6. Create the first superadmin manually after migrations are applied.
EOF
}

run_vpn_host_bootstrap() {
    local bootstrap_dir

    if [[ -z "${VPN_HOST_ENV_FILE}" ]]; then
        return
    fi

    if [[ ! -f "${VPN_HOST_ENV_FILE}" ]]; then
        printf 'VPN host env file not found: %s\n' "${VPN_HOST_ENV_FILE}" >&2
        exit 1
    fi

    bootstrap_dir="${SCRIPT_DIR}/infrastructure/vpn-host"

    log_step "Running VPN host bootstrap"
    "${bootstrap_dir}/00-validate-env.sh" "${VPN_HOST_ENV_FILE}"
    "${bootstrap_dir}/01-install-packages.sh" "${VPN_HOST_ENV_FILE}"
    "${bootstrap_dir}/02-create-users-and-directories.sh" "${VPN_HOST_ENV_FILE}"
    "${bootstrap_dir}/03-install-and-init-postgres.sh" "${VPN_HOST_ENV_FILE}"
    "${bootstrap_dir}/04-configure-freeradius.sh" "${VPN_HOST_ENV_FILE}"
    "${bootstrap_dir}/05-configure-portal-host.sh" "${VPN_HOST_ENV_FILE}"
}

if [[ "${INSTALL_PACKAGES}" -eq 1 ]]; then
    install_packages
fi

create_directories
install_env_file
install_nginx_site
run_vpn_host_bootstrap
print_summary
