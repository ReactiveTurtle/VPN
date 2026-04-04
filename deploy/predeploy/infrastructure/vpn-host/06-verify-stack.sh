#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"
require_env_vars POSTGRES_DB

log_step "Проверка доступности PostgreSQL"
sudo -u postgres psql -d "${POSTGRES_DB}" -c 'select current_database(), current_user;'

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
