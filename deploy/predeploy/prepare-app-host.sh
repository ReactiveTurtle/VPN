#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

PREDEPLOY_ENV_FILE=""
INSTALL_PACKAGES=1
ENABLE_NGINX=1
VPN_HOST_ENV_FILE=""
LAST_COMPLETED_STEP=""

usage() {
    cat <<'EOF'
Использование:
  sudo ./deploy/predeploy/prepare-app-host.sh [опции]

Опции:
  --predeploy-env <path>         Host-side predeploy env-файл в /etc/vpnportal
  --skip-packages                Не устанавливать nginx и Docker-пакеты
  --skip-enable-nginx            Не включать и не перезапускать nginx
  --vpn-host-env <path>          Переопределить путь к env-файлу bootstrap VPN host
  --help                         Показать эту справку

Что делает скрипт:
  - загружает host-level настройки app predeploy из указанного env-файла или автоматически находит один /etc/vpnportal/predeploy.*.env
  - устанавливает nginx, Docker Engine и Docker Compose plugin через apt на Ubuntu
  - создает директории деплоя по настроенному пути и в /etc/vpnportal
  - создает целевой env-файл контейнера из подходящего example, если его еще нет
  - рендерит nginx-конфиг с настроенными server name, именем site и upstream-портом
  - автоматически находит env-файл bootstrap VPN host для того же окружения и запускает repository bootstrap flow шагами `00-06`

Что скрипт не делает:
  - не настраивает SSH-доступ
  - не подставляет GitHub deployment secrets в runtime env-файл Docker без deploy workflow
  - не запускает миграции базы данных
  - не создает первого superadmin
  - не применяет схему приложения
EOF
}

require_root() {
    if [[ "${EUID}" -ne 0 ]]; then
        printf 'Запустите этот скрипт от root.\n' >&2
        exit 1
    fi
}

log_step() {
    printf '\n==> %s\n' "$1"
}

run_step() {
    local step_name="$1"
    shift

    printf '\n---- %s: старт\n' "${step_name}"

    if ! "$@"; then
        printf 'ОШИБКА: %s не пройден. Следующие шаги не будут выполняться.\n' "${step_name}" >&2
        exit 1
    fi

    LAST_COMPLETED_STEP="${step_name}"
    printf '---- %s: завершен\n' "${step_name}"
}

escape_sed_replacement() {
    printf '%s' "$1" | sed -e 's/[\\&]/\\&/g'
}

validate_absolute_path() {
    local name="$1"
    local value="$2"

    case "$value" in
        /*)
            ;;
        *)
            printf 'Переменная %s должна содержать абсолютный путь.\n' "$name" >&2
            exit 1
            ;;
    esac
}

validate_numeric() {
    local name="$1"
    local value="$2"

    case "$value" in
        ''|*[!0-9]*)
            printf 'Переменная %s должна быть числом.\n' "$name" >&2
            exit 1
            ;;
    esac
}

load_predeploy_env() {
    local stage_env="/etc/vpnportal/predeploy.stage.env"
    local prod_env="/etc/vpnportal/predeploy.prod.env"

    if [[ -z "${PREDEPLOY_ENV_FILE}" ]]; then
        if [[ -f "${stage_env}" && -f "${prod_env}" ]]; then
            printf '%s\n' 'Найдено несколько predeploy env-файлов. Укажите --predeploy-env явно.' >&2
            usage >&2
            exit 1
        fi

        if [[ -f "${stage_env}" ]]; then
            PREDEPLOY_ENV_FILE="${stage_env}"
        elif [[ -f "${prod_env}" ]]; then
            PREDEPLOY_ENV_FILE="${prod_env}"
        else
            printf '%s\n' 'Predeploy env-файл не найден. Создайте /etc/vpnportal/predeploy.stage.env или /etc/vpnportal/predeploy.prod.env либо передайте --predeploy-env явно.' >&2
            usage >&2
            exit 1
        fi
    fi

    if [[ ! -f "${PREDEPLOY_ENV_FILE}" ]]; then
        printf 'Predeploy env-файл не найден: %s\n' "${PREDEPLOY_ENV_FILE}" >&2
        exit 1
    fi

    # shellcheck disable=SC1090
    set -a && source "${PREDEPLOY_ENV_FILE}" && set +a

    : "${DEPLOY_ENV_NAME:?DEPLOY_ENV_NAME is required}"
    : "${DEPLOY_PATH:?DEPLOY_PATH is required}"
    : "${DEPLOY_USER:?DEPLOY_USER is required}"
    : "${NGINX_PORTAL_SERVER_NAME:?NGINX_PORTAL_SERVER_NAME is required}"
    : "${RUNTIME_ENV_FILE:?RUNTIME_ENV_FILE is required}"
    : "${NGINX_SITE_NAME:?NGINX_SITE_NAME is required}"
    : "${APP_PORT:?APP_PORT is required}"

    case "${DEPLOY_ENV_NAME}" in
        prod)
            ENV_EXAMPLE="${SCRIPT_DIR}/env/vpnportal.prod.container.env.example"
            ;;
        stage)
            ENV_EXAMPLE="${SCRIPT_DIR}/env/vpnportal.stage.container.env.example"
            ;;
        *)
            printf 'Неподдерживаемое значение DEPLOY_ENV_NAME: %s\n' "${DEPLOY_ENV_NAME}" >&2
            exit 1
            ;;
    esac

    validate_absolute_path DEPLOY_PATH "${DEPLOY_PATH}"
    validate_absolute_path RUNTIME_ENV_FILE "${RUNTIME_ENV_FILE}"
    validate_numeric APP_PORT "${APP_PORT}"

    NGINX_AVAILABLE_PATH="/etc/nginx/sites-available/${NGINX_SITE_NAME}"
    NGINX_ENABLED_PATH="/etc/nginx/sites-enabled/${NGINX_SITE_NAME}"

    if [[ -z "${VPN_HOST_ENV_FILE}" ]]; then
        VPN_HOST_ENV_FILE="/etc/vpnportal/vpn-host.${DEPLOY_ENV_NAME}.env"
    fi
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --predeploy-env)
            PREDEPLOY_ENV_FILE="${2:?predeploy env path is required}"
            shift 2
            ;;
        --skip-packages)
            INSTALL_PACKAGES=0
            shift
            ;;
        --skip-enable-nginx)
            ENABLE_NGINX=0
            shift
            ;;
        --vpn-host-env)
            VPN_HOST_ENV_FILE="${2:?vpn host env path is required}"
            shift 2
            ;;
        --help)
            usage
            exit 0
            ;;
        *)
            printf 'Неизвестный аргумент: %s\n' "$1" >&2
            usage >&2
            exit 1
            ;;
    esac
done

require_root
load_predeploy_env

install_packages() {
    local version_codename docker_keyring docker_repo

    log_step "Установка необходимых apt-пакетов"
    apt-get update
    apt-get install -y ca-certificates curl gnupg nginx

    version_codename="$(. /etc/os-release && printf '%s' "${VERSION_CODENAME}")"
    docker_keyring="/etc/apt/keyrings/docker.asc"
    docker_repo="/etc/apt/sources.list.d/docker.list"

    install -d -m 0755 /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o "$docker_keyring"
    chmod a+r "$docker_keyring"

    printf 'deb [arch=%s signed-by=%s] https://download.docker.com/linux/ubuntu %s stable\n' \
        "$(dpkg --print-architecture)" \
        "$docker_keyring" \
        "$version_codename" > "$docker_repo"

    apt-get update
    apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

    systemctl enable docker
    systemctl start docker

    if id -u "$DEPLOY_USER" >/dev/null 2>&1; then
        usermod -aG docker "$DEPLOY_USER"
    else
        printf 'Пользователь deploy %s еще не существует, добавление в группу docker пропущено.\n' "$DEPLOY_USER"
    fi
}

create_directories() {
    log_step "Создание директорий приложения"
    install -d -m 0755 /etc/vpnportal
    install -d -m 0755 "$(dirname "${RUNTIME_ENV_FILE}")"
    install -d -m 0755 "${DEPLOY_PATH}"
    install -d -m 0755 /usr/local/bin
    install -d -m 0755 /usr/local/lib/vpnportal
}

install_env_file() {
    log_step "Подготовка env-файла контейнера"

    if [[ -f "${RUNTIME_ENV_FILE}" ]]; then
        printf 'Существующий env-файл сохранен: %s\n' "${RUNTIME_ENV_FILE}"
        return
    fi

    install -m 0640 "${ENV_EXAMPLE}" "${RUNTIME_ENV_FILE}"
    printf 'Создан env-файл из example: %s\n' "${RUNTIME_ENV_FILE}"
    printf 'Заполните реальные значения Docker runtime перед первым rollout приложения.\n'
}

install_nginx_site() {
    local escaped_server_name escaped_proxy_pass temp_file

    log_step "Рендер nginx-конфига"
    escaped_server_name="$(escape_sed_replacement "${NGINX_PORTAL_SERVER_NAME}")"
    escaped_proxy_pass="$(escape_sed_replacement "http://127.0.0.1:${APP_PORT}")"
    temp_file="$(mktemp)"

    sed \
        -e "s|server_name _;|server_name ${escaped_server_name};|" \
        -e "s|proxy_pass http://127.0.0.1:5000;|proxy_pass ${escaped_proxy_pass};|" \
        "${SCRIPT_DIR}/nginx/vpnportal.conf" > "${temp_file}"

    install -m 0644 "${temp_file}" "${NGINX_AVAILABLE_PATH}"
    rm -f "${temp_file}"
    ln -sfn "${NGINX_AVAILABLE_PATH}" "${NGINX_ENABLED_PATH}"

    nginx -t

    if [[ "${ENABLE_NGINX}" -eq 1 ]]; then
        systemctl enable nginx
        systemctl restart nginx
    fi
}

print_summary() {
    local vpn_host_note="выполнен с ${VPN_HOST_ENV_FILE}"
    local smoke_test_note="доступна после первого deploy и запуска API через deploy/host/verify-portal-runtime.sh"
    local postgres_note="роли PostgreSQL и доступ к базе проверены в шагах 03 и 06"

    cat <<EOF

Подготовка завершена.

Predeploy env: ${PREDEPLOY_ENV_FILE}
Окружение: ${DEPLOY_ENV_NAME}
Корень приложения: ${DEPLOY_PATH}
Env-файл: ${RUNTIME_ENV_FILE}
nginx site: ${NGINX_AVAILABLE_PATH}
Bootstrap VPN host: ${vpn_host_note}
Проверки PostgreSQL: ${postgres_note}
Smoke test портала: ${smoke_test_note}
Последний завершенный шаг: ${LAST_COMPLETED_STEP}

Следующие ручные шаги:
  1. Настройте SSH-доступ для пользователя деплоя.
  2. Настройте GitHub Environment Secrets, чтобы deploy мог рендерить ${RUNTIME_ENV_FILE} и обновлять ${PREDEPLOY_ENV_FILE}.
  3. Перелогиньтесь под ${DEPLOY_USER} или выполните 'newgrp docker', чтобы применилось членство в группе docker.
  4. Убедитесь, что DEPLOY_PATH остается равным ${DEPLOY_PATH} и в GitHub Environment Secrets, и в ${PREDEPLOY_ENV_FILE}.
  5. Запустите Docker-based deployment workflow, чтобы скачать и поднять контейнеры.
  6. Создайте первого superadmin вручную после применения миграций.
EOF
}

run_vpn_host_bootstrap() {
    local bootstrap_dir

    if [[ ! -f "${VPN_HOST_ENV_FILE}" ]]; then
        printf 'Обязательный VPN host env-файл не найден: %s\n' "${VPN_HOST_ENV_FILE}" >&2
        printf 'prepare-app-host.sh использует окружение из %s и требует matching vpn-host.<env>.env для полного predeploy.\n' "${PREDEPLOY_ENV_FILE}" >&2
        exit 1
    fi

    bootstrap_dir="${SCRIPT_DIR}/infrastructure/vpn-host"

    log_step "Запуск bootstrap VPN host"
    run_step "Шаг 00 - проверка bootstrap env" "${bootstrap_dir}/00-validate-env.sh" "${VPN_HOST_ENV_FILE}"
    run_step "Шаг 01 - установка пакетов VPN host" "${bootstrap_dir}/01-install-packages.sh" "${VPN_HOST_ENV_FILE}"
    run_step "Шаг 02 - создание директорий VPN host" "${bootstrap_dir}/02-create-users-and-directories.sh" "${VPN_HOST_ENV_FILE}"
    run_step "Шаг 03 - инициализация PostgreSQL" "${bootstrap_dir}/03-install-and-init-postgres.sh" "${VPN_HOST_ENV_FILE}"
    run_step "Шаг 04 - настройка FreeRADIUS" "${bootstrap_dir}/04-configure-freeradius.sh" "${VPN_HOST_ENV_FILE}"
    run_step "Шаг 05 - подготовка runtime env портала" "${bootstrap_dir}/05-configure-portal-host.sh" "${VPN_HOST_ENV_FILE}"
    run_step "Шаг 06 - итоговая проверка host stack" "${bootstrap_dir}/06-verify-stack.sh" "${VPN_HOST_ENV_FILE}"
}

if [[ "${INSTALL_PACKAGES}" -eq 1 ]]; then
    run_step "App host - установка базовых пакетов" install_packages
fi

run_step "App host - создание директорий" create_directories
run_step "App host - подготовка runtime env" install_env_file
run_step "App host - рендер nginx-конфига" install_nginx_site
run_step "VPN host - bootstrap 00-06" run_vpn_host_bootstrap
print_summary
