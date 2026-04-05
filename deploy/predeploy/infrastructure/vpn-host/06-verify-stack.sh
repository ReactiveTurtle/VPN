#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"
require_env_vars POSTGRES_DB POSTGRES_APP_USER POSTGRES_RADIUS_USER

assert_single_result() {
    local description="$1"
    local sql="$2"
    local expected="${3:-1}"
    local result

    result="$(sudo -u postgres psql -tAc "${sql}")"
    result="$(printf '%s' "${result}" | tr -d '[:space:]')"

    if [[ "${result}" != "${expected}" ]]; then
        printf 'Проверка не пройдена: %s\n' "${description}" >&2
        exit 1
    fi
}

log_step "Проверка доступности PostgreSQL"
sudo -u postgres psql -d "${POSTGRES_DB}" -c 'select current_database(), current_user;'

log_step "Проверка ролей PostgreSQL и доступа к базе"
assert_single_result \
    "роль приложения ${POSTGRES_APP_USER} должна существовать" \
    "SELECT 1 FROM pg_roles WHERE rolname = '${POSTGRES_APP_USER}'"
assert_single_result \
    "роль FreeRADIUS ${POSTGRES_RADIUS_USER} должна существовать" \
    "SELECT 1 FROM pg_roles WHERE rolname = '${POSTGRES_RADIUS_USER}'"
assert_single_result \
    "база данных ${POSTGRES_DB} должна существовать" \
    "SELECT 1 FROM pg_database WHERE datname = '${POSTGRES_DB}'"
assert_single_result \
    "роль ${POSTGRES_APP_USER} должна иметь CONNECT к ${POSTGRES_DB}" \
    "SELECT CASE WHEN has_database_privilege('${POSTGRES_APP_USER}', '${POSTGRES_DB}', 'CONNECT') THEN 1 ELSE 0 END"
assert_single_result \
    "роль ${POSTGRES_RADIUS_USER} должна иметь CONNECT к ${POSTGRES_DB}" \
    "SELECT CASE WHEN has_database_privilege('${POSTGRES_RADIUS_USER}', '${POSTGRES_DB}', 'CONNECT') THEN 1 ELSE 0 END"

log_step "Проверка systemd units"
systemctl --no-pager --full status postgresql strongswan-starter freeradius nginx || true

log_step "Проверка конфигурации FreeRADIUS"
freeradius -CX

log_step "Проверка состояния strongSwan"
ipsec statusall || true

log_step "Проверка helper для forwarding accounting"
test -x /usr/local/lib/vpnportal/forward-accounting-event.sh

log_step "Проверка runtime helper для disconnect"
test -x /usr/local/lib/vpnportal/disconnect-session.sh

log_step "Список listening sockets"
ss -lntup

printf '\nПроверка завершена. Изучите вывод команд для дальнейших действий.\n'
