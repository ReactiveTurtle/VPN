# New Host Checklist

Используйте этот checklist, чтобы подготовить новую машину до первого деплоя.

## Базовая Подготовка Машины

Сначала выполните начальные host-level шаги:

1. Установите необходимые runtime-компоненты, такие как `.NET 10 runtime`, `nginx` и поддержка `systemd`.
2. Создайте корневую директорию деплоя, например `/opt/vpnportal`.
3. Подготовьте `/etc/vpnportal/` для env-файлов.
4. Убедитесь, что SSH-доступ уже работает, как описано в `docs/predeploy/ssh-access.md`.

## Deployment Layout

Ожидаемый layout на целевой машине включает:

- app root: `/opt/vpnportal`
- API files: `/opt/vpnportal/current/api`
- static SPA: `/opt/vpnportal/current/api/wwwroot`
- deploy-managed operational tools: `/usr/local/bin`
- bootstrap-managed VPN runtime helpers: `/usr/local/lib/vpnportal`

## Подготовка Сервисов Приложения

До первого деплоя подготовьте хост приложения:

Для автоматизации большей части этих шагов можно использовать `deploy/predeploy/prepare-app-host.sh`.

Пример:

```bash
sudo ./deploy/predeploy/prepare-app-host.sh --target production --server-name vpn.example.com
```

Скрипт подготавливает директории, ставит базовые пакеты, устанавливает `systemd` unit, `nginx` config и runtime env-файл из example.

Если целевая машина одновременно является VPN-хостом, скрипт можно запустить с уже заполненным env-файлом bootstrap:

```bash
sudo ./deploy/predeploy/prepare-app-host.sh --target production --server-name vpn.example.com --vpn-host-env /etc/vpnportal/vpn-host.env
```

В этом режиме он дополнительно запускает `infrastructure/vpn-host/bootstrap/01-06`, включая установку и настройку `strongSwan`, `FreeRADIUS` и `PostgreSQL`.

При этом он не заменяет ручные шаги для SSH-доступа, настройки GitHub secrets для рендера `appsettings.{Environment}.json`, миграций БД и создания первого администратора.

1. Скопируйте `deploy/systemd/vpnportal-api.production.service` и/или `deploy/systemd/vpnportal-api.staging.service` в `/etc/systemd/system/` и при необходимости поправьте пути или пользователя.
2. Скопируйте `deploy/nginx/vpnportal.conf` в конфигурацию nginx и обновите `server_name`.
3. Скопируйте `deploy/env/vpnportal.production.env.example` или `deploy/env/vpnportal.staging.env.example` в `/etc/vpnportal/`.
4. Убедитесь, что `DEPLOY_PATH` существует на сервере и доступен для записи пользователю деплоя.
5. Убедитесь, что в `/usr/local/bin` можно писать через `sudo install` из packaged deploy-скрипта, если вы хотите автоматически обновлять packaged operational tools.
6. Включите нужный systemd-сервис.

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

Для runtime-конфигурации API боевые значения теперь должны попадать из GitHub deployment secrets в `appsettings.{Environment}.json` во время workflow, а не храниться в tracked env-файлах репозитория.

## Критерии Готовности

Новая машина готова к первому деплою, когда:

- SSH-доступ работает для пользователя деплоя
- директории деплоя существуют
- конфигурации systemd и nginx установлены
- env-файл подготовлен в `/etc/vpnportal/`
- удаленный deploy-скрипт установлен и исполняем
- bootstrap VPN-хоста завершен, если этого требует целевая топология
- шаги по схеме БД и первому администратору понятны и запланированы до первого production-использования
