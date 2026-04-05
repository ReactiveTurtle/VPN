#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "${SCRIPT_DIR}/common.sh"

require_root
load_env "${1:-}"
require_env_vars VPN_SERVER_ADDRESS STRONGSWAN_CERT_PATH STRONGSWAN_KEY_PATH STRONGSWAN_CA_CERT_PATH

CA_KEY_PATH="/etc/ipsec.d/private/ca-key.pem"

log_step "Подготовка strongSwan PKI"
ensure_directory "/etc/ipsec.d/private" root:root 0700
ensure_directory "/etc/ipsec.d/certs" root:root 0755
ensure_directory "/etc/ipsec.d/cacerts" root:root 0755

server_cert_exists=0
server_key_exists=0
ca_cert_exists=0
ca_key_exists=0

[[ -f "${STRONGSWAN_CERT_PATH}" ]] && server_cert_exists=1
[[ -f "${STRONGSWAN_KEY_PATH}" ]] && server_key_exists=1
[[ -f "${STRONGSWAN_CA_CERT_PATH}" ]] && ca_cert_exists=1
[[ -f "${CA_KEY_PATH}" ]] && ca_key_exists=1

existing_count=$((server_cert_exists + server_key_exists + ca_cert_exists + ca_key_exists))

if [[ "${existing_count}" -eq 4 ]]; then
    log_step "Используются существующие strongSwan сертификаты и ключи"
    printf 'Сертификат сервера: %s\n' "${STRONGSWAN_CERT_PATH}"
    printf 'Ключ сервера: %s\n' "${STRONGSWAN_KEY_PATH}"
    printf 'CA сертификат: %s\n' "${STRONGSWAN_CA_CERT_PATH}"
    exit 0
fi

if [[ "${existing_count}" -ne 0 ]]; then
    printf 'Обнаружен неполный комплект strongSwan PKI-файлов.\n' >&2
    printf 'Ожидались: %s, %s, %s и %s\n' "${STRONGSWAN_CERT_PATH}" "${STRONGSWAN_KEY_PATH}" "${STRONGSWAN_CA_CERT_PATH}" "${CA_KEY_PATH}" >&2
    exit 1
fi

log_step "Генерация CA и серверного сертификата strongSwan"
pki --gen --type rsa --size 4096 --outform pem > "${CA_KEY_PATH}"
pki --self --ca --lifetime 3650 --in "${CA_KEY_PATH}" --type rsa --dn "CN=VPN Portal CA" --outform pem > "${STRONGSWAN_CA_CERT_PATH}"
pki --gen --type rsa --size 4096 --outform pem > "${STRONGSWAN_KEY_PATH}"
pki --pub --in "${STRONGSWAN_KEY_PATH}" --type rsa | pki --issue \
    --lifetime 3650 \
    --cacert "${STRONGSWAN_CA_CERT_PATH}" \
    --cakey "${CA_KEY_PATH}" \
    --dn "CN=${VPN_SERVER_ADDRESS}" \
    --san "${VPN_SERVER_ADDRESS}" \
    --flag serverAuth \
    --flag ikeIntermediate \
    --outform pem > "${STRONGSWAN_CERT_PATH}"

chmod 0600 "${CA_KEY_PATH}" "${STRONGSWAN_KEY_PATH}"
chmod 0644 "${STRONGSWAN_CA_CERT_PATH}" "${STRONGSWAN_CERT_PATH}"

printf '\nstrongSwan PKI подготовлен.\n'
printf 'Сертификат сервера: %s\n' "${STRONGSWAN_CERT_PATH}"
printf 'Ключ сервера: %s\n' "${STRONGSWAN_KEY_PATH}"
printf 'CA сертификат: %s\n' "${STRONGSWAN_CA_CERT_PATH}"
