#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"
require_env_vars PORTAL_ENV_DIR POSTGRES_DB POSTGRES_APP_USER POSTGRES_APP_PASSWORD POSTGRES_RADIUS_USER POSTGRES_RADIUS_PASSWORD

log_step "Запуск PostgreSQL"
systemctl enable postgresql
systemctl restart postgresql

log_step "Создание ролей PostgreSQL и базы данных"
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

log_step "Подготовка example для ручного создания superadmin"
install -m 0640 -o root -g root "${VPN_HOST_ROOT}/postgresql/020_seed_superadmin.sql.example" \
    "${PORTAL_ENV_DIR}/seed-superadmin.sql.example"

printf '\nИнициализация PostgreSQL завершена. Примените EF migrations отдельно через migration program перед запуском API.\n'
