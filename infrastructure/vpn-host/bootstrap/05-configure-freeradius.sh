#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"

log_step "Rendering FreeRADIUS configuration"
render_template "${VPN_HOST_ROOT}/freeradius/clients.conf.template" /etc/freeradius/3.0/clients.conf
render_template "${VPN_HOST_ROOT}/freeradius/mods-available/sql.template" /etc/freeradius/3.0/mods-available/sql
render_template "${VPN_HOST_ROOT}/freeradius/sites-available/default.template" /etc/freeradius/3.0/sites-available/default
ensure_directory /usr/local/lib/vpnportal root:root 0755
render_template "${VPN_HOST_ROOT}/freeradius/scripts/forward-accounting-event.sh.template" /usr/local/lib/vpnportal/forward-accounting-event.sh
chmod 0755 /usr/local/lib/vpnportal/forward-accounting-event.sh

ln -sf /etc/freeradius/3.0/mods-available/sql /etc/freeradius/3.0/mods-enabled/sql

log_step "Validating FreeRADIUS configuration"
freeradius -CX

log_step "Restarting FreeRADIUS"
systemctl restart freeradius

printf '\nFreeRADIUS configuration updated.\n'
