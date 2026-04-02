#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"
require_env_vars INTERNAL_API_BASE_URL InternalApi__SharedSecret

base_url="${INTERNAL_API_BASE_URL%/}"

log_step "Checking public system status endpoint"
curl --fail --silent --show-error "${base_url}/api/system/status"

log_step "Checking database status endpoint"
curl --fail --silent --show-error "${base_url}/api/system/database"

log_step "Checking that internal API secret is configured"
if [[ -z "${InternalApi__SharedSecret:-}" ]]; then
    printf 'InternalApi__SharedSecret is empty.\n' >&2
    exit 1
fi

log_step "Smoke test completed"
printf 'Portal status endpoints are reachable at %s\n' "${base_url}"
