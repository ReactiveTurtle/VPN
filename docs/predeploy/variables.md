# Predeploy Variables

Этот документ фиксирует, какие переменные нужны для каждого predeploy-шага, откуда они берутся и где именно валидируются.

## Четыре Разных Набора Переменных

### 1. GitHub Environment Secrets

Это переменные для обычного deploy workflow из `.github/workflows/deploy.yml`.

SSH и deploy secrets:

- `DEPLOY_HOST`
- `DEPLOY_PORT`
- `DEPLOY_USER`
- `DEPLOY_PATH`
- `DEPLOY_SERVER_NAME`
- `DEPLOY_SSH_PRIVATE_KEY`

Runtime secrets приложения:

- `DATABASE__CONNECTIONSTRING`
- `EMAIL__HOST`
- `EMAIL__USERNAME`
- `EMAIL__PASSWORD`
- `EMAIL__FROMEMAIL`
- `EMAIL__FROMNAME`
- `EMAIL__PUBLICBASEURL`
- `INTERNALAPI__SHAREDSECRET`
- `VPNACCESS__SERVERADDRESS`
- `VPNRUNTIME__DISCONNECTSCRIPTPATH`

Валидация:

- SSH/deploy secrets проверяются в `.github/workflows/deploy.yml` на шаге `Validate deployment secrets`
- runtime secrets приложения проверяются в `.github/workflows/deploy.yml` на шаге `Validate application secrets`

### 2. Host-Side App Predeploy Env

Это файл `/etc/vpnportal/predeploy.prod.env` или `/etc/vpnportal/predeploy.stage.env`.

Именно этот файл использует `deploy/predeploy/prepare-app-host.sh` как единый источник истины для app predeploy.

Обязательные значения:

- `DEPLOY_ENV_NAME`
- `DEPLOY_PATH`
- `DEPLOY_USER`
- `SERVER_NAME`
- `RUNTIME_ENV_FILE`
- `NGINX_SITE_NAME`
- `APP_PORT`

Этот файл рендерится из GitHub Environment Secrets во время `deploy.yml`, но перед самым первым ручным predeploy его можно создать из `deploy/predeploy/env/predeploy.<env>.env.example`.

### 3. Bootstrap Env VPN-Хоста

Это файл `/etc/vpnportal/vpn-host.prod.env` или `/etc/vpnportal/vpn-host.stage.env`.

Именно этот файл используют шаги `00-07` из `deploy/predeploy/infrastructure/vpn-host/`.

Ручные обязательные значения для bootstrap:

- `PUBLIC_BASE_URL`
- `VPN_SERVER_ADDRESS`
- `InternalApi__SharedSecret`
- `POSTGRES_APP_PASSWORD`
- `POSTGRES_RADIUS_PASSWORD`
- `RADIUS_SHARED_SECRET`
- `STRONGSWAN_CERT_PATH`
- `STRONGSWAN_KEY_PATH`
- `STRONGSWAN_RIGHT_SOURCE_IP`
- `STRONGSWAN_DNS`
- `Email__Host`
- `Email__Username`
- `Email__Password`
- `Email__FromEmail`

`TARGET` рекомендуется задавать явно, но если его нет, bootstrap loader может вывести его из имени env-файла, например `vpn-host.stage.env` или `vpn-host.prod.env`.

Значения, которые loader вычисляет автоматически в `deploy/predeploy/infrastructure/vpn-host/common.sh`:

- `ASPNETCORE_ENVIRONMENT`
- `PORTAL_DEPLOY_ROOT`
- `PORTAL_ENV_DIR`
- `PORTAL_ENV_FILE`
- `POSTGRES_DB`
- `POSTGRES_APP_USER`
- `POSTGRES_RADIUS_USER`
- `RADIUS_CLIENT_ADDRESS`
- `INTERNAL_API_BASE_URL`
- `STRONGSWAN_SERVER_ID`
- `VpnAccess__ServerAddress`
- `VpnRuntime__DisconnectScriptPath`
- `Email__PublicBaseUrl`
- `Email__Port`
- `Email__FromName`

### 4. Runtime Env Контейнера

Это файл `/etc/vpnportal/vpnportal.prod.container.env` или `/etc/vpnportal/vpnportal.stage.container.env`.

Он нужен уже для запуска контейнера приложения. Его либо:

- рендерит GitHub deploy workflow из GitHub Environment Secrets
- либо подготавливает `05-configure-portal-host.sh` во время bootstrap single-host сервера

## Порядок Шагов `00-07`

### Шаг 00. `00-validate-env.sh`

Что делает:

- загружает bootstrap env-файл
- проверяет обязательные ручные переменные
- проверяет базовую корректность URL и путей
- показывает вычисленные значения до начала изменений на хосте

Нужны переменные:

- `PUBLIC_BASE_URL`
- `VPN_SERVER_ADDRESS`
- `InternalApi__SharedSecret`
- `POSTGRES_APP_PASSWORD`
- `POSTGRES_RADIUS_PASSWORD`
- `RADIUS_SHARED_SECRET`
- `STRONGSWAN_CERT_PATH`
- `STRONGSWAN_KEY_PATH`
- `STRONGSWAN_RIGHT_SOURCE_IP`
- `STRONGSWAN_DNS`
- `Email__Host`
- `Email__Username`
- `Email__Password`
- `Email__FromEmail`

Что дополнительно валидируется:

- `PUBLIC_BASE_URL` должен начинаться с `http://` или `https://`
- `INTERNAL_API_BASE_URL`, если задан явно, должен начинаться с `http://` или `https://`
- `STRONGSWAN_CERT_PATH`, `STRONGSWAN_KEY_PATH` и `VpnRuntime__DisconnectScriptPath` должны быть absolute path

Где валидация:

- `deploy/predeploy/infrastructure/vpn-host/00-validate-env.sh`

### Шаг 01. `01-install-packages.sh`

Что делает:

- ставит `strongSwan`, `FreeRADIUS`, `PostgreSQL`, `nginx` и вспомогательные пакеты

Нужны переменные:

- отдельных обязательных переменных у шага нет
- нужен корректный bootstrap env-файл, который можно загрузить через `load_env`

Где валидация:

- существование env-файла и корректность target проверяет `load_env` из `common.sh`

### Шаг 02. `02-create-users-and-directories.sh`

Что делает:

- создаёт deployment layout и каталоги под env-файлы и backups

Нужны переменные:

- `PORTAL_DEPLOY_ROOT`
- `PORTAL_ENV_DIR`

Обычно задаются автоматически из `TARGET`.

Где валидация:

- `require_env_vars PORTAL_DEPLOY_ROOT PORTAL_ENV_DIR`

### Шаг 03. `03-install-and-init-postgres.sh`

Что делает:

- создаёт PostgreSQL roles и базу данных
- копирует пример SQL для ручного создания первого `superadmin`

Нужны переменные:

- `PORTAL_ENV_DIR`
- `POSTGRES_DB`
- `POSTGRES_APP_USER`
- `POSTGRES_APP_PASSWORD`
- `POSTGRES_RADIUS_USER`
- `POSTGRES_RADIUS_PASSWORD`

Часть значений вычисляется автоматически, но пароли должны быть заданы вручную.

Где валидация:

- `require_env_vars PORTAL_ENV_DIR POSTGRES_DB POSTGRES_APP_USER POSTGRES_APP_PASSWORD POSTGRES_RADIUS_USER POSTGRES_RADIUS_PASSWORD`

### Шаг 04. `04-configure-freeradius.sh`

Что делает:

- рендерит конфигурацию `FreeRADIUS`
- ставит helper `/usr/local/lib/vpnportal/forward-accounting-event.sh`
- прогоняет `freeradius -CX`

Нужны переменные:

- `VPN_HOST_ENV_FILE`
- `POSTGRES_DB`
- `POSTGRES_RADIUS_USER`
- `POSTGRES_RADIUS_PASSWORD`
- `RADIUS_CLIENT_ADDRESS`
- `RADIUS_SHARED_SECRET`
- `INTERNAL_API_BASE_URL`
- `InternalApi__SharedSecret`

`RADIUS_CLIENT_ADDRESS` и `INTERNAL_API_BASE_URL` обычно вычисляются автоматически, если не переопределены.

Где валидация:

- `require_env_vars VPN_HOST_ENV_FILE POSTGRES_DB POSTGRES_RADIUS_USER POSTGRES_RADIUS_PASSWORD RADIUS_CLIENT_ADDRESS RADIUS_SHARED_SECRET INTERNAL_API_BASE_URL InternalApi__SharedSecret`

### Шаг 05. `05-configure-portal-host.sh`

Что делает:

- пишет runtime env-файл контейнера приложения

Нужны переменные:

- `PORTAL_ENV_FILE`
- `ASPNETCORE_ENVIRONMENT`
- `POSTGRES_DB`
- `POSTGRES_APP_USER`
- `POSTGRES_APP_PASSWORD`
- `Email__Host`
- `Email__Port`
- `Email__Username`
- `Email__Password`
- `Email__FromEmail`
- `Email__FromName`
- `Email__PublicBaseUrl`
- `InternalApi__SharedSecret`
- `VpnAccess__ServerAddress`
- `VpnRuntime__DisconnectScriptPath`

Часть значений вычисляется автоматически, но SMTP-хост, SMTP-учётка, SMTP-пароль, адрес отправителя и shared secret должны быть заданы вручную.

Где валидация:

- `require_env_vars PORTAL_ENV_FILE ASPNETCORE_ENVIRONMENT POSTGRES_DB POSTGRES_APP_USER POSTGRES_APP_PASSWORD Email__Host Email__Port Email__Username Email__Password Email__FromEmail Email__FromName Email__PublicBaseUrl InternalApi__SharedSecret VpnAccess__ServerAddress VpnRuntime__DisconnectScriptPath`

### Шаг 06. `06-verify-stack.sh`

Что делает:

- проверяет локальную доступность `PostgreSQL`
- показывает статус systemd units
- проверяет `freeradius -CX`
- проверяет runtime helpers и открытые сокеты

Нужны переменные:

- `POSTGRES_DB`

Где валидация:

- `require_env_vars POSTGRES_DB`

### Шаг 07. `07-smoke-test-portal.sh`

Что делает:

- проверяет `api/system/status`
- проверяет `api/system/database`

Нужны переменные:

- `INTERNAL_API_BASE_URL`
- `InternalApi__SharedSecret`

Важно:

- этот шаг имеет смысл только после первого deploy и запуска API

Где валидация:

- `require_env_vars INTERNAL_API_BASE_URL InternalApi__SharedSecret`

## Где Находится `strongSwan`

Отдельного predeploy-шага для конфигурации `strongSwan` сейчас нет.

- установка пакета `strongSwan` происходит в шаге `01-install-packages.sh`
- deploy-time конфигурация применяется отдельно через `deploy/host/apply-strongswan-config.sh`

Для deploy-time шага нужны:

- `STRONGSWAN_SERVER_ID`
- `STRONGSWAN_CERT_PATH`
- `STRONGSWAN_KEY_PATH`
- `STRONGSWAN_RIGHT_SOURCE_IP`
- `STRONGSWAN_DNS`
- `RADIUS_CLIENT_ADDRESS`
- `RADIUS_SHARED_SECRET`

Где валидация:

- `deploy/host/apply-strongswan-config.sh`
