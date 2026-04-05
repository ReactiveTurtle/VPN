#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/../predeploy/infrastructure/vpn-host/common.sh"

require_root
load_env "${1:-}"
require_env_vars INTERNAL_API_BASE_URL InternalApi__SharedSecret

base_url="${INTERNAL_API_BASE_URL%/}"

log_step "Проверка публичного endpoint system status"
curl --fail --silent --show-error "${base_url}/api/system/status"

log_step "Проверка endpoint статуса базы данных"
curl --fail --silent --show-error "${base_url}/api/system/database"

log_step "Проверка, что internal API secret настроен"
if [[ -z "${InternalApi__SharedSecret:-}" ]]; then
    printf 'InternalApi__SharedSecret пуст.\n' >&2
    exit 1
fi

log_step "Проверка runtime портала завершена"
printf 'Status endpoints портала доступны по адресу %s\n' "${base_url}"
