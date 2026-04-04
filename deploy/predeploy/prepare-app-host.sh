#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

PREDEPLOY_ENV_FILE=""
INSTALL_PACKAGES=1
ENABLE_NGINX=1
VPN_HOST_ENV_FILE=""

usage() {
    cat <<'EOF'
Usage:
  sudo ./deploy/predeploy/prepare-app-host.sh [options]

Options:
  --predeploy-env <path>         Host-side predeploy env file under /etc/vpnportal
  --skip-packages                Do not install nginx or Docker packages
  --skip-enable-nginx            Do not enable/restart nginx
  --vpn-host-env <path>          Run infrastructure/vpn-host bootstrap with this env file
  --help                         Show this help

What the script does:
  - loads host-level app predeploy settings from the provided env file or auto-detects a single /etc/vpnportal/predeploy.*.env file
  - installs nginx, Docker Engine, and Docker Compose plugin on Ubuntu via apt
  - creates deployment directories under the configured deploy path and /etc/vpnportal
  - creates the target container env file from the matching example if it does not exist yet
  - renders nginx config with the configured server name, site name, and upstream port
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

validate_absolute_path() {
    local name="$1"
    local value="$2"

    case "$value" in
        /*)
            ;;
        *)
            printf '%s must be an absolute path.\n' "$name" >&2
            exit 1
            ;;
    esac
}

validate_numeric() {
    local name="$1"
    local value="$2"

    case "$value" in
        ''|*[!0-9]*)
            printf '%s must be numeric.\n' "$name" >&2
            exit 1
            ;;
    esac
}

load_predeploy_env() {
    local stage_env="/etc/vpnportal/predeploy.stage.env"
    local prod_env="/etc/vpnportal/predeploy.prod.env"

    if [[ -z "${PREDEPLOY_ENV_FILE}" ]]; then
        if [[ -f "${stage_env}" && -f "${prod_env}" ]]; then
            printf '%s\n' 'Multiple predeploy env files found. Pass --predeploy-env explicitly.' >&2
            usage >&2
            exit 1
        fi

        if [[ -f "${stage_env}" ]]; then
            PREDEPLOY_ENV_FILE="${stage_env}"
        elif [[ -f "${prod_env}" ]]; then
            PREDEPLOY_ENV_FILE="${prod_env}"
        else
            printf '%s\n' 'No predeploy env file found. Create /etc/vpnportal/predeploy.stage.env or /etc/vpnportal/predeploy.prod.env, or pass --predeploy-env explicitly.' >&2
            usage >&2
            exit 1
        fi
    fi

    if [[ ! -f "${PREDEPLOY_ENV_FILE}" ]]; then
        printf 'Predeploy env file not found: %s\n' "${PREDEPLOY_ENV_FILE}" >&2
        exit 1
    fi

    # shellcheck disable=SC1090
    set -a && source "${PREDEPLOY_ENV_FILE}" && set +a

    : "${DEPLOY_ENV_NAME:?DEPLOY_ENV_NAME is required}"
    : "${DEPLOY_PATH:?DEPLOY_PATH is required}"
    : "${DEPLOY_USER:?DEPLOY_USER is required}"
    : "${NGINX_PORTAL_SERVER_NAME:?NGINX_PORTAL_SERVER_NAME is required}"
    : "${RUNTIME_ENV_FILE:?RUNTIME_ENV_FILE is required}"
    : "${NGINX_SITE_NAME:?NGINX_SITE_NAME is required}"
    : "${APP_PORT:?APP_PORT is required}"

    case "${DEPLOY_ENV_NAME}" in
        prod)
            ENV_EXAMPLE="${SCRIPT_DIR}/env/vpnportal.prod.container.env.example"
            ;;
        stage)
            ENV_EXAMPLE="${SCRIPT_DIR}/env/vpnportal.stage.container.env.example"
            ;;
        *)
            printf 'Unsupported DEPLOY_ENV_NAME: %s\n' "${DEPLOY_ENV_NAME}" >&2
            exit 1
            ;;
    esac

    validate_absolute_path DEPLOY_PATH "${DEPLOY_PATH}"
    validate_absolute_path RUNTIME_ENV_FILE "${RUNTIME_ENV_FILE}"
    validate_numeric APP_PORT "${APP_PORT}"

    NGINX_AVAILABLE_PATH="/etc/nginx/sites-available/${NGINX_SITE_NAME}"
    NGINX_ENABLED_PATH="/etc/nginx/sites-enabled/${NGINX_SITE_NAME}"
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --predeploy-env)
            PREDEPLOY_ENV_FILE="${2:?predeploy env path is required}"
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

require_root
load_predeploy_env

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
    install -d -m 0755 "$(dirname "${RUNTIME_ENV_FILE}")"
    install -d -m 0755 "${DEPLOY_PATH}"
    install -d -m 0755 /usr/local/bin
    install -d -m 0755 /usr/local/lib/vpnportal
}

install_env_file() {
    log_step "Preparing container environment file"

    if [[ -f "${RUNTIME_ENV_FILE}" ]]; then
        printf 'Keeping existing env file: %s\n' "${RUNTIME_ENV_FILE}"
        return
    fi

    install -m 0640 "${ENV_EXAMPLE}" "${RUNTIME_ENV_FILE}"
    printf 'Created env file from example: %s\n' "${RUNTIME_ENV_FILE}"
    printf 'Fill real Docker runtime values before the first application rollout.\n'
}

install_nginx_site() {
    local escaped_server_name escaped_proxy_pass temp_file

    log_step "Rendering nginx config"
    escaped_server_name="$(escape_sed_replacement "${NGINX_PORTAL_SERVER_NAME}")"
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

Predeploy env: ${PREDEPLOY_ENV_FILE}
Environment: ${DEPLOY_ENV_NAME}
App root: ${DEPLOY_PATH}
Env file: ${RUNTIME_ENV_FILE}
nginx site: ${NGINX_AVAILABLE_PATH}
VPN host bootstrap: ${vpn_host_note}

Next manual steps:
  1. Configure SSH access for the deployment user.
  2. Configure GitHub Environment Secrets so deploy can render ${RUNTIME_ENV_FILE} and refresh ${PREDEPLOY_ENV_FILE}.
  3. Re-login as ${DEPLOY_USER} or run 'newgrp docker' so docker group membership takes effect.
  4. Ensure DEPLOY_PATH remains ${DEPLOY_PATH} in GitHub Environment Secrets and in ${PREDEPLOY_ENV_FILE}.
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
