#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/../predeploy/infrastructure/vpn-host/common.sh"

require_root
load_env "${1:-}"
require_env_vars STRONGSWAN_SERVER_ID STRONGSWAN_CERT_PATH STRONGSWAN_KEY_PATH STRONGSWAN_RIGHT_SOURCE_IP STRONGSWAN_DNS RADIUS_CLIENT_ADDRESS RADIUS_SHARED_SECRET

log_step "Rendering strongSwan configuration"
render_template "${VPN_HOST_ROOT}/strongswan/ipsec.conf.template" /etc/ipsec.conf
render_template "${VPN_HOST_ROOT}/strongswan/strongswan.conf.template" /etc/strongswan.conf
ensure_directory /usr/local/lib/vpnportal root:root 0755
render_template "${VPN_HOST_ROOT}/strongswan/scripts/disconnect-session.sh.template" /usr/local/lib/vpnportal/disconnect-session.sh
chmod 0755 /usr/local/lib/vpnportal/disconnect-session.sh

log_step "Restarting strongSwan"
systemctl restart strongswan-starter

printf '\nstrongSwan configuration updated.\n'
