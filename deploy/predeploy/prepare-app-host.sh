#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

TARGET="production"
SERVER_NAME="_"
INSTALL_PACKAGES=1
ENABLE_NGINX=1
ENABLE_SERVICE=1
VPN_HOST_ENV_FILE=""

usage() {
    cat <<'EOF'
Usage:
  sudo ./deploy/predeploy/prepare-app-host.sh [options]

Options:
  --target <production|staging>  Deployment target. Default: production
  --server-name <value>          nginx server_name value. Default: _
  --skip-packages                Do not install nginx or ASP.NET runtime
  --skip-enable-nginx            Do not enable/restart nginx
  --skip-enable-service          Do not enable the systemd service
  --vpn-host-env <path>          Run infrastructure/vpn-host bootstrap with this env file
  --help                         Show this help

What the script does:
  - installs nginx and ASP.NET Core runtime on Ubuntu via apt
  - creates deployment directories under /opt and /etc/vpnportal
  - installs the target systemd unit
  - creates the target runtime env file from the example if it does not exist yet
  - renders nginx config with the target root and upstream port
  - optionally runs the repository VPN host bootstrap flow

What the script does not do:
  - configure SSH access
  - inject GitHub deployment secrets into appsettings.{Environment}.json
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
        --skip-packages)
            INSTALL_PACKAGES=0
            shift
            ;;
        --skip-enable-nginx)
            ENABLE_NGINX=0
            shift
            ;;
        --skip-enable-service)
            ENABLE_SERVICE=0
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
    production)
        APP_ROOT="/opt/vpnportal"
        SERVICE_NAME="vpnportal-api"
        SERVICE_TEMPLATE="${REPO_ROOT}/deploy/systemd/vpnportal-api.production.service"
        ENV_EXAMPLE="${REPO_ROOT}/deploy/env/vpnportal.production.env.example"
        ENV_FILE="/etc/vpnportal/vpnportal.production.env"
        NGINX_SITE_NAME="vpnportal.conf"
        APP_PORT="5000"
        ;;
    staging)
        APP_ROOT="/opt/vpnportal-staging"
        SERVICE_NAME="vpnportal-api-staging"
        SERVICE_TEMPLATE="${REPO_ROOT}/deploy/systemd/vpnportal-api.staging.service"
        ENV_EXAMPLE="${REPO_ROOT}/deploy/env/vpnportal.staging.env.example"
        ENV_FILE="/etc/vpnportal/vpnportal.staging.env"
        NGINX_SITE_NAME="vpnportal-staging.conf"
        APP_PORT="5001"
        ;;
    *)
        printf 'Unsupported target: %s\n' "$TARGET" >&2
        exit 1
        ;;
esac

APP_CURRENT_ROOT="${APP_ROOT}/current/api"
APP_PUBLIC_ROOT="${APP_CURRENT_ROOT}/wwwroot"
SYSTEMD_TARGET="/etc/systemd/system/${SERVICE_NAME}.service"
NGINX_AVAILABLE_PATH="/etc/nginx/sites-available/${NGINX_SITE_NAME}"
NGINX_ENABLED_PATH="/etc/nginx/sites-enabled/${NGINX_SITE_NAME}"

require_root

install_packages() {
    local version_id repo_package

    log_step "Installing required apt packages"
    apt-get update
    apt-get install -y ca-certificates curl gnupg

    if ! dpkg -s aspnetcore-runtime-10.0 >/dev/null 2>&1; then
        version_id="$(. /etc/os-release && printf '%s' "${VERSION_ID}")"
        repo_package="/tmp/packages-microsoft-prod.deb"
        curl -fsSL -o "$repo_package" "https://packages.microsoft.com/config/ubuntu/${version_id}/packages-microsoft-prod.deb"
        dpkg -i "$repo_package"
        rm -f "$repo_package"
        apt-get update
    fi

    apt-get install -y aspnetcore-runtime-10.0 nginx
}

create_directories() {
    log_step "Creating application directories"
    install -d -m 0755 /etc/vpnportal
    install -d -m 0755 "${APP_ROOT}"
    install -d -m 0755 "${APP_ROOT}/releases"
    install -d -m 0755 /usr/local/bin
    install -d -m 0755 /usr/local/lib/vpnportal
}

install_service() {
    log_step "Installing systemd unit"
    install -m 0644 "${SERVICE_TEMPLATE}" "${SYSTEMD_TARGET}"
    systemctl daemon-reload

    if [[ "${ENABLE_SERVICE}" -eq 1 ]]; then
        systemctl enable "${SERVICE_NAME}"
    fi
}

install_env_file() {
    log_step "Preparing environment file"

    if [[ -f "${ENV_FILE}" ]]; then
        printf 'Keeping existing env file: %s\n' "${ENV_FILE}"
        return
    fi

    install -m 0640 "${ENV_EXAMPLE}" "${ENV_FILE}"
    printf 'Created env file from example: %s\n' "${ENV_FILE}"
    printf 'This file only carries runtime process variables such as ASPNETCORE environment settings.\n'
}

install_nginx_site() {
    local escaped_server_name escaped_public_root escaped_proxy_pass temp_file

    log_step "Rendering nginx config"
    escaped_server_name="$(escape_sed_replacement "${SERVER_NAME}")"
    escaped_public_root="$(escape_sed_replacement "${APP_PUBLIC_ROOT}")"
    escaped_proxy_pass="$(escape_sed_replacement "http://127.0.0.1:${APP_PORT}")"
    temp_file="$(mktemp)"

    sed \
        -e "s|server_name _;|server_name ${escaped_server_name};|" \
        -e "s|root /opt/vpnportal/current/api/wwwroot;|root ${escaped_public_root};|" \
        -e "s|proxy_pass http://127.0.0.1:5000;|proxy_pass ${escaped_proxy_pass};|" \
        "${REPO_ROOT}/deploy/nginx/vpnportal.conf" > "${temp_file}"

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
Service: ${SERVICE_NAME}
Env file: ${ENV_FILE}
nginx site: ${NGINX_AVAILABLE_PATH}
VPN host bootstrap: ${vpn_host_note}

Next manual steps:
  1. Configure SSH access for the deployment user.
  2. Configure GitHub deployment secrets for appsettings rendering.
  3. Ensure DEPLOY_PATH points to ${APP_ROOT} or another writable upload directory on the server.
  4. Run schema migration before the first real API start.
  5. Create the first superadmin manually.
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

    bootstrap_dir="${REPO_ROOT}/infrastructure/vpn-host/bootstrap"

    log_step "Running VPN host bootstrap"
    "${bootstrap_dir}/01-install-packages.sh" "${VPN_HOST_ENV_FILE}"
    "${bootstrap_dir}/02-create-users-and-directories.sh" "${VPN_HOST_ENV_FILE}"
    "${bootstrap_dir}/03-install-and-init-postgres.sh" "${VPN_HOST_ENV_FILE}"
    "${bootstrap_dir}/04-configure-strongswan.sh" "${VPN_HOST_ENV_FILE}"
    "${bootstrap_dir}/05-configure-freeradius.sh" "${VPN_HOST_ENV_FILE}"
    "${bootstrap_dir}/06-configure-portal-host.sh" "${VPN_HOST_ENV_FILE}"
}

if [[ "${INSTALL_PACKAGES}" -eq 1 ]]; then
    install_packages
fi

create_directories
install_service
install_env_file
install_nginx_site
run_vpn_host_bootstrap
print_summary
