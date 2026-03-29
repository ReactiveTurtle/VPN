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
Database__Provider=PostgreSql
Database__ConnectionString=Host=127.0.0.1;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_APP_USER};Password=${POSTGRES_APP_PASSWORD}
Email__Enabled=true
Email__Provider=Smtp
Email__Host=${SMTP_HOST}
Email__Port=${SMTP_PORT}
Email__UseSsl=true
Email__Username=${SMTP_USERNAME}
Email__Password=${SMTP_PASSWORD}
Email__FromEmail=${SMTP_FROM_EMAIL}
Email__FromName=${SMTP_FROM_NAME}
Email__PublicBaseUrl=${PORTAL_PUBLIC_URL}
InternalApi__SharedSecret=${INTERNAL_API_SHARED_SECRET}
EOF

chmod 0640 "${PORTAL_ENV_FILE}"

log_step "Portal host configuration prepared"
printf 'Portal env file written to %s\n' "${PORTAL_ENV_FILE}"
printf 'Copy deploy/systemd and deploy/nginx assets manually if they are not already present.\n'
