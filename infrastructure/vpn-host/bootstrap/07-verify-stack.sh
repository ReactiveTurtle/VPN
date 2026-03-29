#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"

log_step "Checking PostgreSQL connectivity"
sudo -u postgres psql -d "${POSTGRES_DB}" -c 'select current_database(), current_user;'

log_step "Checking systemd units"
systemctl --no-pager --full status postgresql strongswan-starter freeradius nginx || true

log_step "Validating FreeRADIUS configuration"
freeradius -CX

log_step "Checking strongSwan status"
ipsec statusall || true

log_step "Listing listening sockets"
ss -lntup

printf '\nVerification completed. Review command output for follow-up actions.\n'
