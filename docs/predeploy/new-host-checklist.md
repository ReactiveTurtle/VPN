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
- runtime env file: `/etc/vpnportal/vpnportal.production.container.env`
- published container port via host loopback: `127.0.0.1:5000` or `127.0.0.1:5001`
- deploy-managed operational tools: `/usr/local/bin`
- bootstrap-managed VPN runtime helpers: `/usr/local/lib/vpnportal`

## Подготовка Сервисов Приложения

До первого деплоя подготовьте хост приложения:

Для автоматизации большей части этих шагов можно использовать `deploy/predeploy/prepare-app-host.sh`.

Пример:

```bash
sudo ./deploy/predeploy/prepare-app-host.sh --target production --server-name vpn.example.com
```

Скрипт подготавливает директории, ставит `Docker Engine`, `Docker Compose plugin` и `nginx`, добавляет deploy user в группу `docker`, настраивает `nginx` config и container env-файл из example.

Если целевая машина одновременно является VPN-хостом, скрипт можно запустить с уже заполненным env-файлом bootstrap:

```bash
sudo ./deploy/predeploy/prepare-app-host.sh --target production --server-name vpn.example.com --vpn-host-env /etc/vpnportal/vpn-host.env
```

В этом режиме он дополнительно запускает `infrastructure/vpn-host/bootstrap/01-06`, включая установку и настройку `strongSwan`, `FreeRADIUS` и `PostgreSQL`.

При этом он не заменяет ручные шаги для SSH-доступа, настройки GitHub secrets для container env-файла, миграций БД и создания первого администратора.

1. Скопируйте `deploy/nginx/vpnportal.conf` в конфигурацию nginx и обновите `server_name`.
2. Скопируйте `deploy/env/vpnportal.production.container.env.example` или `deploy/env/vpnportal.staging.container.env.example` в `/etc/vpnportal/`.
3. Скопируйте `deploy/docker/docker-compose.yml` на сервер, например в `/opt/vpnportal/docker-compose.yml`.
4. Убедитесь, что `DEPLOY_PATH` существует на сервере и доступен для записи пользователю деплоя.
5. Убедитесь, что в `/usr/local/bin` можно писать через `sudo install`, если вы хотите отдельно обновлять host-managed operational tools.
6. Убедитесь, что `nginx` включен и может проксировать в контейнер на loopback-порт целевого окружения.

Docker rollout использует один `docker-compose.yml` и environment-specific container env-файлы. `staging` и `production` разделяются через project name, loopback-port и разные env-файлы.

Если `prepare-app-host.sh` добавил пользователя деплоя в группу `docker`, после этого нужно перелогиниться под этим пользователем или выполнить `newgrp docker`, иначе новые права группы не применятся в текущей сессии.

## Bootstrap VPN-Хоста

Если эта же машина одновременно хостит `strongSwan`, `FreeRADIUS` и `PostgreSQL`, выполните отдельный bootstrap, описанный в `infrastructure/vpn-host/README.md`.

Это можно сделать либо вручную по порядку из `infrastructure/vpn-host/README.md`, либо через `deploy/predeploy/prepare-app-host.sh --vpn-host-env <path>`.

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

Для runtime-конфигурации API боевые значения теперь должны попадать из GitHub deployment secrets в container env-файл во время workflow, а не храниться в tracked env-файлах репозитория.

## Критерии Готовности

Новая машина готова к первому деплою, когда:

- SSH-доступ работает для пользователя деплоя
- директории деплоя существуют
- Docker Engine и Compose plugin установлены
- конфигурация nginx установлена
- container env-файл подготовлен в `/etc/vpnportal/`
- compose file подготовлен в `DEPLOY_PATH`
- bootstrap VPN-хоста завершен, если этого требует целевая топология
- шаги по схеме БД и первому администратору понятны и запланированы до первого production-использования
