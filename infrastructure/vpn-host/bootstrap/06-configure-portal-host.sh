#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"

log_step "Writing production environment file for portal"
cat > "${PORTAL_ENV_FILE}" <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5000
EOF

chmod 0640 "${PORTAL_ENV_FILE}"

log_step "Portal host configuration prepared"
printf 'Portal env file written to %s\n' "${PORTAL_ENV_FILE}"
printf 'App settings secrets should be rendered into appsettings.{Environment}.json during deploy.\n'
printf 'Copy deploy/systemd and deploy/nginx assets manually if they are not already present.\n'
