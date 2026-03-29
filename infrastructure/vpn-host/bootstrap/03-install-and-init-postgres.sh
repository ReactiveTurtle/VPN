#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"

log_step "Starting PostgreSQL"
systemctl enable postgresql
systemctl restart postgresql

log_step "Creating PostgreSQL roles and database"
sudo -u postgres psql -v ON_ERROR_STOP=1 \
    -v app_user="${POSTGRES_APP_USER}" \
    -v app_password="${POSTGRES_APP_PASSWORD}" \
    -v radius_user="${POSTGRES_RADIUS_USER}" \
    -v radius_password="${POSTGRES_RADIUS_PASSWORD}" \
    -f "${VPN_HOST_ROOT}/postgresql/010_roles.sql"

if ! sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname = '${POSTGRES_DB}'" | grep -q 1; then
    sudo -u postgres createdb --owner="${POSTGRES_APP_USER}" "${POSTGRES_DB}"
fi

sudo -u postgres psql -v ON_ERROR_STOP=1 -d postgres <<EOF
GRANT CONNECT ON DATABASE ${POSTGRES_DB} TO ${POSTGRES_APP_USER};
GRANT CONNECT ON DATABASE ${POSTGRES_DB} TO ${POSTGRES_RADIUS_USER};
EOF

log_step "Applying application schema"
PGPASSWORD="${POSTGRES_APP_PASSWORD}" psql -v ON_ERROR_STOP=1 -h 127.0.0.1 -U "${POSTGRES_APP_USER}" -d "${POSTGRES_DB}" -f "${REPO_ROOT}/database/001_schema.sql"

log_step "Granting baseline access for FreeRADIUS"
sudo -u postgres psql -v ON_ERROR_STOP=1 -d "${POSTGRES_DB}" <<EOF
GRANT USAGE ON SCHEMA public TO ${POSTGRES_RADIUS_USER};
GRANT SELECT ON ALL TABLES IN SCHEMA public TO ${POSTGRES_RADIUS_USER};
GRANT SELECT ON ALL SEQUENCES IN SCHEMA public TO ${POSTGRES_RADIUS_USER};
ALTER DEFAULT PRIVILEGES FOR ROLE ${POSTGRES_APP_USER} IN SCHEMA public GRANT SELECT ON TABLES TO ${POSTGRES_RADIUS_USER};
ALTER DEFAULT PRIVILEGES FOR ROLE ${POSTGRES_APP_USER} IN SCHEMA public GRANT SELECT ON SEQUENCES TO ${POSTGRES_RADIUS_USER};
EOF

log_step "Preparing optional superadmin seed example"
install -m 0640 -o root -g root "${VPN_HOST_ROOT}/postgresql/020_seed_superadmin.sql.example" \
    "${PORTAL_ENV_DIR}/seed-superadmin.sql.example"

printf '\nPostgreSQL initialization completed.\n'
