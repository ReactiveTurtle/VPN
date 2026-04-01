# New Host Checklist

Используйте этот checklist, чтобы подготовить новую машину до первого деплоя.

## Базовая Подготовка Машины

Сначала выполните начальные host-level шаги:

1. Установите необходимые host-компоненты, такие как `Docker Engine`, `Docker Compose plugin`, `nginx` и поддержка `systemd`.
2. Создайте корневую директорию деплоя, например `/opt/vpnportal`.
3. Подготовьте `/etc/vpnportal/` для env-файлов.
4. Убедитесь, что SSH-доступ уже работает, как описано в `docs/predeploy/ssh-access.md`.

## Deployment Layout

Ожидаемый layout на целевой машине включает:

- app root: `/opt/vpnportal`
- compose file: `/opt/vpnportal/docker-compose.yml`
- runtime env file: `/etc/vpnportal/vpnportal.prod.container.env`
- published container port via host loopback: `127.0.0.1:5000` or `127.0.0.1:5001`
- deploy-managed operational tools: `/usr/local/bin`
- bootstrap-managed VPN runtime helpers: `/usr/local/lib/vpnportal`

## Подготовка Сервисов Приложения

До первого деплоя подготовьте хост приложения:

Для автоматизации большей части этих шагов можно использовать скрипт из `deploy/predeploy/prepare-app-host.sh`, который автоматически разворачивается в `/opt/vpnportal/predeploy/` при каждом deploy.

Пример:

```bash
sudo /opt/vpnportal/predeploy/prepare-app-host.sh --target prod --server-name vpn.example.com
```

Скрипт подготавливает директории, ставит `Docker Engine`, `Docker Compose plugin` и `nginx`, добавляет deploy user в группу `docker`, настраивает `nginx` config и container env-файл из example.

Если целевая машина одновременно является VPN-хостом, скрипт можно запустить с уже заполненным env-файлом bootstrap:

```bash
sudo /opt/vpnportal/predeploy/prepare-app-host.sh --target prod --server-name vpn.example.com --vpn-host-env /etc/vpnportal/vpn-host.prod.env
```

Для `stage` используйте отдельный bootstrap env-файл, например `/etc/vpnportal/vpn-host.stage.env`, и отдельный runtime env-файл `/etc/vpnportal/vpnportal.stage.container.env`.

В bootstrap env-файле общие с приложением значения теперь тоже называются как runtime keys приложения, например:

- `ASPNETCORE_ENVIRONMENT=stage`
- `Email__PublicBaseUrl=https://stage-vpn.example.com`
- `InternalApi__SharedSecret=...`
- `VpnAccess__ServerAddress=stage-vpn.example.com`
- `VpnRuntime__DisconnectScriptPath=/usr/local/lib/vpnportal/disconnect-session.sh`
- `Email__Host=...`
- `Email__Port=587`
- `Email__Username=...`
- `Email__Password=...`
- `Email__FromEmail=...`
- `Email__FromName=VPN Portal Stage`

Отдельно в bootstrap env остаются host-only keys вроде `POSTGRES_*`, `STRONGSWAN_*`, `RADIUS_*`, `PORTAL_DEPLOY_ROOT` и `INTERNAL_API_BASE_URL`.

В этом режиме он дополнительно запускает `deploy/predeploy/infrastructure/vpn-host/01-06`, включая установку и настройку `strongSwan`, `FreeRADIUS` и `PostgreSQL`.

При этом он не заменяет ручные шаги для SSH-доступа, настройки GitHub Environment Secrets для runtime-конфигурации приложения, миграций БД и создания первого администратора.

1. Скопируйте `deploy/predeploy/nginx/vpnportal.conf` в конфигурацию nginx и обновите `server_name`.
2. Скопируйте `deploy/predeploy/env/vpnportal.prod.container.env.example` или `deploy/predeploy/env/vpnportal.stage.container.env.example` в `/etc/vpnportal/`.
3. Скопируйте `deploy/docker/docker-compose.yml` на сервер, например в `/opt/vpnportal/docker-compose.yml`.
4. Убедитесь, что `DEPLOY_PATH` существует на сервере и доступен для записи пользователю деплоя.
5. Убедитесь, что в `/usr/local/bin` можно писать через `sudo install`, если вы хотите отдельно обновлять host-managed operational tools.
6. Убедитесь, что `nginx` включен и может проксировать в контейнер на loopback-порт целевого окружения.

Docker rollout использует один `docker-compose.yml` и environment-specific container env-файлы. `stage` и `prod` разделяются через project name, loopback-port и разные env-файлы.

Если `prepare-app-host.sh` добавил пользователя деплоя в группу `docker`, после этого нужно перелогиниться под этим пользователем или выполнить `newgrp docker`, иначе новые права группы не применятся в текущей сессии.

## Bootstrap VPN-Хоста

Если эта же машина одновременно хостит `strongSwan`, `FreeRADIUS` и `PostgreSQL`, выполните отдельный bootstrap, описанный в `infrastructure/vpn-host/README.md`.

Это можно сделать либо вручную по порядку из `infrastructure/vpn-host/README.md` и `deploy/predeploy/infrastructure/vpn-host/01-08`, либо через `/opt/vpnportal/predeploy/prepare-app-host.sh --vpn-host-env <path>`.

Этот документ остается источником истины для:

- порядка запуска скриптов
- обязательных ручных входных данных
- VPN-специфичного host layout
- установки runtime helpers

## Подготовка Схемы И Первого Администратора

До первого реального запуска приложения выполните следующие разовые шаги:

1. Примените схему базы данных через `dotnet run --project src/VpnPortal.Migrations`.
2. Создайте первого `superadmin` вручную по `docs/runbooks/create-first-superadmin.md`.

Программа миграций также может сгенерировать совместимый хэш пароля:

- `dotnet run --project src/VpnPortal.Migrations -- hash-password "<plaintext>"`

Для runtime-конфигурации API боевые значения должны храниться в GitHub Environment Secrets и записываться workflow в host-side container env-файл, а не в tracked env-файлах репозитория.

## Критерии Готовности

Новая машина готова к первому деплою, когда:

- SSH-доступ работает для пользователя деплоя
- директории деплоя существуют
- Docker Engine и Compose plugin установлены
- конфигурация nginx установлена
- container env-файл подготовлен в `/etc/vpnportal/`
- compose file подготовлен в `DEPLOY_PATH`
- bootstrap VPN-хоста завершен, если этого требует целевая топология
- шаги по схеме БД и первому администратору понятны и запланированы до первого prod-использования
