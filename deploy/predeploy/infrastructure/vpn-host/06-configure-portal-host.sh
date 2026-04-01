#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"

log_step "Writing runtime environment file for portal"
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

log_step "Portal host configuration prepared"
printf 'Portal env file written to %s\n' "${PORTAL_ENV_FILE}"
printf 'This env file is intended for Docker-based rollout and assumes host services are reachable as host.docker.internal from the container.\n'
printf 'Copy deploy/docker/docker-compose.yml and deploy/predeploy/nginx/vpnportal.conf manually if they are not already present.\n'
