#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"

validate_http_url() {
    local name="$1"
    local value="$2"

    case "${value}" in
        http://*|https://*)
            ;;
        *)
            printf 'Переменная окружения %s должна начинаться с http:// или https://\n' "${name}" >&2
            exit 1
            ;;
    esac
}

validate_absolute_path() {
    local name="$1"
    local value="$2"

    case "${value}" in
        /*)
            ;;
        *)
            printf 'Переменная окружения %s должна содержать абсолютный путь\n' "${name}" >&2
            exit 1
            ;;
    esac
}

log_step "Проверка bootstrap env для VPN host"

require_env_vars \
    PUBLIC_BASE_URL \
    VPN_SERVER_ADDRESS \
    InternalApi__SharedSecret \
    POSTGRES_APP_PASSWORD \
    POSTGRES_RADIUS_PASSWORD \
    RADIUS_SHARED_SECRET \
    STRONGSWAN_CERT_PATH \
    STRONGSWAN_KEY_PATH \
    STRONGSWAN_RIGHT_SOURCE_IP \
    STRONGSWAN_DNS \
    Email__Host \
    Email__Username \
    Email__Password \
    Email__FromEmail

validate_http_url PUBLIC_BASE_URL "${PUBLIC_BASE_URL}"

if [[ -n "${INTERNAL_API_BASE_URL:-}" ]]; then
    validate_http_url INTERNAL_API_BASE_URL "${INTERNAL_API_BASE_URL}"
fi

validate_absolute_path STRONGSWAN_CERT_PATH "${STRONGSWAN_CERT_PATH}"
validate_absolute_path STRONGSWAN_KEY_PATH "${STRONGSWAN_KEY_PATH}"
validate_absolute_path VpnRuntime__DisconnectScriptPath "${VpnRuntime__DisconnectScriptPath}"

cat <<EOF

Bootstrap env корректен.

Проверены обязательные входные параметры:
  - PUBLIC_BASE_URL
  - VPN_SERVER_ADDRESS
  - InternalApi__SharedSecret
  - POSTGRES_APP_PASSWORD
  - POSTGRES_RADIUS_PASSWORD
  - RADIUS_SHARED_SECRET
  - STRONGSWAN_CERT_PATH
  - STRONGSWAN_KEY_PATH
  - STRONGSWAN_RIGHT_SOURCE_IP
  - STRONGSWAN_DNS
  - Email__Host
  - Email__Username
  - Email__Password
  - Email__FromEmail

Вычисленные значения:
  - TARGET=${TARGET}
  - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
  - PORTAL_DEPLOY_ROOT=${PORTAL_DEPLOY_ROOT}
  - PORTAL_ENV_DIR=${PORTAL_ENV_DIR}
  - PORTAL_ENV_FILE=${PORTAL_ENV_FILE}
  - POSTGRES_DB=${POSTGRES_DB}
  - POSTGRES_APP_USER=${POSTGRES_APP_USER}
  - POSTGRES_RADIUS_USER=${POSTGRES_RADIUS_USER}
  - INTERNAL_API_BASE_URL=${INTERNAL_API_BASE_URL}
  - VpnAccess__ServerAddress=${VpnAccess__ServerAddress}
  - VpnRuntime__DisconnectScriptPath=${VpnRuntime__DisconnectScriptPath}
  - STRONGSWAN_SERVER_ID=${STRONGSWAN_SERVER_ID}
  - RADIUS_CLIENT_ADDRESS=${RADIUS_CLIENT_ADDRESS}
  - Email__PublicBaseUrl=${Email__PublicBaseUrl}
  - Email__Port=${Email__Port}
  - Email__FromName=${Email__FromName}
EOF
