#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VPN_HOST_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
REPO_ROOT="$(cd "${VPN_HOST_ROOT}/../.." && pwd)"
DEFAULT_ENV_FILE="/etc/vpnportal/vpn-host.env"

load_env() {
    local env_file="${1:-${DEFAULT_ENV_FILE}}"

    if [[ ! -f "${env_file}" ]]; then
        printf 'Environment file not found: %s\n' "${env_file}" >&2
        exit 1
    fi

    set -a
    # shellcheck disable=SC1090
    source "${env_file}"
    set +a
}

require_root() {
    if [[ "${EUID}" -ne 0 ]]; then
        printf 'Run this script as root.\n' >&2
        exit 1
    fi
}

ensure_directory() {
    local path="$1"
    local owner="$2"
    local mode="$3"

    install -d -m "${mode}" -o "${owner%%:*}" -g "${owner##*:}" "${path}"
}

backup_file() {
    local path="$1"

    if [[ -f "${path}" ]]; then
        ensure_directory /var/backups/vpnportal root:root 0750
        cp -a "${path}" "/var/backups/vpnportal/$(basename "${path}").$(date +%Y%m%d%H%M%S).bak"
    fi
}

render_template() {
    local template_path="$1"
    local destination_path="$2"

    backup_file "${destination_path}"
    envsubst < "${template_path}" > "${destination_path}"
}

log_step() {
    printf '\n==> %s\n' "$1"
}
