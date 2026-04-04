#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"
require_env_vars PORTAL_DEPLOY_ROOT PORTAL_ENV_DIR

log_step "Создание директорий деплоя"
ensure_directory "${PORTAL_DEPLOY_ROOT}" root:root 0755
ensure_directory "${PORTAL_DEPLOY_ROOT}/bin" root:root 0755
ensure_directory "${PORTAL_DEPLOY_ROOT}/releases" root:root 0755
ensure_directory "${PORTAL_DEPLOY_ROOT}/current" root:root 0755
ensure_directory "${PORTAL_ENV_DIR}" root:root 0750
ensure_directory /var/backups/vpnportal root:root 0750

printf '\nПодготовка директорий завершена.\n'
