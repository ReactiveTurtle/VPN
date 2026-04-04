#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"

log_step "Обновление apt metadata"
apt-get update

log_step "Установка пакетов VPN host"
DEBIAN_FRONTEND=noninteractive apt-get install -y \
    gettext-base \
    libcharon-extra-plugins \
    nginx \
    postgresql \
    postgresql-client \
    strongswan \
    strongswan-pki \
    freeradius \
    freeradius-postgresql

log_step "Включение базовых сервисов"
systemctl enable postgresql
systemctl enable strongswan-starter || true
systemctl enable freeradius
systemctl enable nginx

printf '\nУстановка пакетов завершена.\n'
