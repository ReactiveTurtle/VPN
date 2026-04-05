# Operator Cheatsheet

Короткая шпаргалка для первого запуска predeploy на новой машине.

Подробности по переменным и шагам остаются в:

- `docs/predeploy/variables.md`
- `docs/predeploy/new-host-checklist.md`
- `infrastructure/vpn-host/README.md`

## Prod

1. Подготовьте bootstrap env-файл:

```bash
sudo install -d -m 0750 /etc/vpnportal
sudo install -m 0640 deploy/predeploy/env/predeploy.prod.env.example /etc/vpnportal/predeploy.prod.env
sudo nano /etc/vpnportal/predeploy.prod.env
sudo install -m 0640 infrastructure/vpn-host/env/vpn-host.prod.env.example /etc/vpnportal/vpn-host.prod.env
sudo nano /etc/vpnportal/vpn-host.prod.env
```

`/etc/vpnportal/predeploy.prod.env` можно подготовить вручную до первого app predeploy или получить из обычного deploy workflow, который обновляет этот файл на хосте.

2. Подготовьте app host:

```bash
sudo /opt/vpnportal/predeploy/prepare-app-host.sh --predeploy-env /etc/vpnportal/predeploy.prod.env
```

3. Если bootstrap запускаете вручную, порядок такой:

```bash
sudo ./deploy/predeploy/infrastructure/vpn-host/00-validate-env.sh /etc/vpnportal/vpn-host.prod.env
sudo ./deploy/predeploy/infrastructure/vpn-host/01-install-packages.sh /etc/vpnportal/vpn-host.prod.env
sudo ./deploy/predeploy/infrastructure/vpn-host/02-create-users-and-directories.sh /etc/vpnportal/vpn-host.prod.env
sudo ./deploy/predeploy/infrastructure/vpn-host/03-install-and-init-postgres.sh /etc/vpnportal/vpn-host.prod.env
sudo ./deploy/predeploy/infrastructure/vpn-host/04-configure-freeradius.sh /etc/vpnportal/vpn-host.prod.env
sudo ./deploy/predeploy/infrastructure/vpn-host/05-configure-portal-host.sh /etc/vpnportal/vpn-host.prod.env
sudo ./deploy/predeploy/infrastructure/vpn-host/06-verify-stack.sh /etc/vpnportal/vpn-host.prod.env
```

4. После первого deploy и запуска API:

```bash
sudo ./deploy/host/verify-portal-runtime.sh /etc/vpnportal/vpn-host.prod.env
```

## Stage

1. Подготовьте bootstrap env-файл:

```bash
sudo install -d -m 0750 /etc/vpnportal
sudo install -m 0640 deploy/predeploy/env/predeploy.stage.env.example /etc/vpnportal/predeploy.stage.env
sudo nano /etc/vpnportal/predeploy.stage.env
sudo install -m 0640 infrastructure/vpn-host/env/vpn-host.stage.env.example /etc/vpnportal/vpn-host.stage.env
sudo nano /etc/vpnportal/vpn-host.stage.env
```

`/etc/vpnportal/predeploy.stage.env` можно подготовить вручную до первого app predeploy или получить из обычного deploy workflow, который обновляет этот файл на хосте.

2. Подготовьте app host:

```bash
sudo /opt/vpnportal/predeploy/prepare-app-host.sh --predeploy-env /etc/vpnportal/predeploy.stage.env
```

3. Если bootstrap запускаете вручную, порядок такой:

```bash
sudo ./deploy/predeploy/infrastructure/vpn-host/00-validate-env.sh /etc/vpnportal/vpn-host.stage.env
sudo ./deploy/predeploy/infrastructure/vpn-host/01-install-packages.sh /etc/vpnportal/vpn-host.stage.env
sudo ./deploy/predeploy/infrastructure/vpn-host/02-create-users-and-directories.sh /etc/vpnportal/vpn-host.stage.env
sudo ./deploy/predeploy/infrastructure/vpn-host/03-install-and-init-postgres.sh /etc/vpnportal/vpn-host.stage.env
sudo ./deploy/predeploy/infrastructure/vpn-host/04-configure-freeradius.sh /etc/vpnportal/vpn-host.stage.env
sudo ./deploy/predeploy/infrastructure/vpn-host/05-configure-portal-host.sh /etc/vpnportal/vpn-host.stage.env
sudo ./deploy/predeploy/infrastructure/vpn-host/06-verify-stack.sh /etc/vpnportal/vpn-host.stage.env
```

4. После первого deploy и запуска API:

```bash
sudo ./deploy/host/verify-portal-runtime.sh /etc/vpnportal/vpn-host.stage.env
```

## Что Проверить Перед Стартом

- заполнены обязательные переменные в `/etc/vpnportal/predeploy.<target>.env`
- заполнены обязательные переменные в `/etc/vpnportal/vpn-host.<target>.env`
- `prepare-app-host.sh` будет использовать оба файла одного и того же окружения автоматически
- SSH уже настроен для deploy user
- `DEPLOY_PATH` из `/etc/vpnportal/predeploy.<target>.env` существует и доступен для записи
- после добавления deploy user в группу `docker` выполнен новый логин или `newgrp docker`
- GitHub Environment Secrets настроены отдельно для `stage` и `prod`

## Что Делать После Predeploy

1. Запустить обычный deploy workflow.
2. Убедиться, что миграции БД применились.
3. Создать первого `superadmin` по `docs/runbooks/create-first-superadmin.md`.
4. Прогнать `deploy/host/verify-portal-runtime.sh` и затем `infrastructure/vpn-host/runbooks/verify-vpn-runtime-flow.md`.
