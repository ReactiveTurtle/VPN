#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"
require_env_vars PORTAL_ENV_FILE ASPNETCORE_ENVIRONMENT POSTGRES_DB POSTGRES_APP_USER POSTGRES_APP_PASSWORD Email__Host Email__Port Email__Username Email__Password Email__FromEmail Email__FromName Email__PublicBaseUrl InternalApi__SharedSecret VpnAccess__ServerAddress VpnRuntime__DisconnectScriptPath

log_step "Запись runtime env-файла портала"
cat > "${PORTAL_ENV_FILE}" <<EOF
ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
Database__ConnectionString=Host=host.docker.internal;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_APP_USER};Password=${POSTGRES_APP_PASSWORD}
Email__Enabled=true
Email__Provider=Smtp
Email__Host=${Email__Host}
Email__Port=${Email__Port}
Email__UseSsl=true
Email__Username=${Email__Username}
Email__Password=${Email__Password}
Email__FromEmail=${Email__FromEmail}
Email__FromName=${Email__FromName}
Email__PublicBaseUrl=${Email__PublicBaseUrl}
InternalApi__SharedSecret=${InternalApi__SharedSecret}
VpnAccess__ServerAddress=${VpnAccess__ServerAddress}
VpnRuntime__DisconnectScriptPath=${VpnRuntime__DisconnectScriptPath}
EOF

chmod 0640 "${PORTAL_ENV_FILE}"

log_step "Конфигурация host для портала подготовлена"
printf 'Env-файл портала записан в %s\n' "${PORTAL_ENV_FILE}"
printf 'Этот env-файл предназначен для Docker-based rollout и предполагает, что host services доступны из контейнера как host.docker.internal.\n'
printf 'Скопируйте deploy/docker/docker-compose.yml и deploy/predeploy/nginx/vpnportal.conf вручную, если их еще нет.\n'
