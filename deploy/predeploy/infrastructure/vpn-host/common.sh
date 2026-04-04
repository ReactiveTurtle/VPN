#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"
VPN_HOST_ROOT="${REPO_ROOT}/infrastructure/vpn-host"
DEFAULT_ENV_FILE="/etc/vpnportal/vpn-host.prod.env"

infer_target() {
    local env_file="$1"
    local env_basename

    if [[ -n "${TARGET:-}" ]]; then
        printf '%s' "${TARGET}"
        return
    fi

    if [[ -n "${ASPNETCORE_ENVIRONMENT:-}" ]]; then
        printf '%s' "${ASPNETCORE_ENVIRONMENT}"
        return
    fi

    env_basename="$(basename "${env_file}")"

    case "${env_basename}" in
        *stage*)
            printf 'stage'
            ;;
        *prod*)
            printf 'prod'
            ;;
        *)
            printf 'Не удалось определить target deployment по %s. Укажите TARGET=stage|prod в bootstrap env-файле.\n' "${env_file}" >&2
            exit 1
            ;;
    esac
}

normalize_env() {
    TARGET="$(infer_target "${VPN_HOST_ENV_FILE}")"

    case "${TARGET}" in
        prod|stage)
            ;;
        *)
            printf 'Неподдерживаемый target deployment: %s\n' "${TARGET}" >&2
            exit 1
            ;;
    esac

    ASPNETCORE_ENVIRONMENT="${TARGET}"
    PORTAL_ENV_DIR="${PORTAL_ENV_DIR:-/etc/vpnportal}"

    if [[ "${TARGET}" == "prod" ]]; then
        PORTAL_DEPLOY_ROOT="${PORTAL_DEPLOY_ROOT:-/opt/vpnportal}"
        PORTAL_ENV_FILE="${PORTAL_ENV_FILE:-${PORTAL_ENV_DIR}/vpnportal.prod.container.env}"
        Email__FromName="${Email__FromName:-VPN Portal}"
    else
        PORTAL_DEPLOY_ROOT="${PORTAL_DEPLOY_ROOT:-/opt/vpnportal-stage}"
        PORTAL_ENV_FILE="${PORTAL_ENV_FILE:-${PORTAL_ENV_DIR}/vpnportal.stage.container.env}"
        Email__FromName="${Email__FromName:-VPN Portal Stage}"
    fi

    POSTGRES_DB="${POSTGRES_DB:-vpn_portal}"
    POSTGRES_APP_USER="${POSTGRES_APP_USER:-vpn_portal_app}"
    POSTGRES_RADIUS_USER="${POSTGRES_RADIUS_USER:-vpn_portal_radius}"
    RADIUS_CLIENT_ADDRESS="${RADIUS_CLIENT_ADDRESS:-127.0.0.1}"
    Email__Port="${Email__Port:-587}"
    VpnRuntime__DisconnectScriptPath="${VpnRuntime__DisconnectScriptPath:-/usr/local/lib/vpnportal/disconnect-session.sh}"

    if [[ -n "${PUBLIC_BASE_URL:-}" && -z "${Email__PublicBaseUrl:-}" ]]; then
        Email__PublicBaseUrl="${PUBLIC_BASE_URL}"
    fi

    if [[ -n "${PUBLIC_BASE_URL:-}" && -z "${INTERNAL_API_BASE_URL:-}" ]]; then
        INTERNAL_API_BASE_URL="${PUBLIC_BASE_URL}"
    fi

    if [[ -n "${VPN_SERVER_ADDRESS:-}" && -z "${VpnAccess__ServerAddress:-}" ]]; then
        VpnAccess__ServerAddress="${VPN_SERVER_ADDRESS}"
    fi

    if [[ -n "${VPN_SERVER_ADDRESS:-}" && -z "${STRONGSWAN_SERVER_ID:-}" ]]; then
        STRONGSWAN_SERVER_ID="${VPN_SERVER_ADDRESS}"
    fi

    export TARGET
    export ASPNETCORE_ENVIRONMENT
    export PORTAL_ENV_DIR
    export PORTAL_DEPLOY_ROOT
    export PORTAL_ENV_FILE
    export INTERNAL_API_BASE_URL
    export POSTGRES_DB
    export POSTGRES_APP_USER
    export POSTGRES_RADIUS_USER
    export RADIUS_CLIENT_ADDRESS
    export STRONGSWAN_SERVER_ID
    export Email__PublicBaseUrl
    export Email__Port
    export Email__FromName
    export VpnAccess__ServerAddress
    export VpnRuntime__DisconnectScriptPath
}

require_env_vars() {
    local missing=0
    local name value

    for name in "$@"; do
        value="${!name:-}"

        if [[ -z "${value}" ]]; then
            printf 'Отсутствует обязательная переменная окружения: %s\n' "${name}" >&2
            missing=1
        fi
    done

    if [[ "${missing}" -ne 0 ]]; then
        exit 1
    fi
}

load_env() {
    local env_file="${1:-${DEFAULT_ENV_FILE}}"

    if [[ ! -f "${env_file}" ]]; then
        printf 'Env-файл не найден: %s\n' "${env_file}" >&2
        exit 1
    fi

    set -a
    # shellcheck disable=SC1090
    source "${env_file}"
    set +a

    export VPN_HOST_ENV_FILE="${env_file}"

    normalize_env
}

require_root() {
    if [[ "${EUID}" -ne 0 ]]; then
        printf 'Запустите этот скрипт от root.\n' >&2
        exit 1
    fi
}

ensure_directory() {
    local path="$1"
    local owner="$2"
    local mode="$3"

    install -d -m "${mode}" -o "${owner%%:*}" -g "${owner##*:}" "${path}"
}

backup_file() {
    local path="$1"

    if [[ -f "${path}" ]]; then
        ensure_directory /var/backups/vpnportal root:root 0750
        cp -a "${path}" "/var/backups/vpnportal/$(basename "${path}").$(date +%Y%m%d%H%M%S).bak"
    fi
}

render_template() {
    local template_path="$1"
    local destination_path="$2"

    backup_file "${destination_path}"
    envsubst < "${template_path}" > "${destination_path}"
}

log_step() {
    printf '\n==> %s\n' "$1"
}
