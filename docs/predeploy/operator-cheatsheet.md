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
sudo install -m 0640 infrastructure/vpn-host/env/vpn-host.prod.env.example /etc/vpnportal/vpn-host.prod.env
sudo nano /etc/vpnportal/vpn-host.prod.env
```

2. Подготовьте app host:

```bash
sudo /opt/vpnportal/predeploy/prepare-app-host.sh --target prod --server-name vpn.example.com --vpn-host-env /etc/vpnportal/vpn-host.prod.env
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
sudo ./deploy/predeploy/infrastructure/vpn-host/07-smoke-test-portal.sh /etc/vpnportal/vpn-host.prod.env
```

## Stage

1. Подготовьте bootstrap env-файл:

```bash
sudo install -d -m 0750 /etc/vpnportal
sudo install -m 0640 infrastructure/vpn-host/env/vpn-host.stage.env.example /etc/vpnportal/vpn-host.stage.env
sudo nano /etc/vpnportal/vpn-host.stage.env
```

2. Подготовьте app host:

```bash
sudo /opt/vpnportal/predeploy/prepare-app-host.sh --target stage --server-name stage-vpn.example.com --vpn-host-env /etc/vpnportal/vpn-host.stage.env
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
sudo ./deploy/predeploy/infrastructure/vpn-host/07-smoke-test-portal.sh /etc/vpnportal/vpn-host.stage.env
```

## Что Проверить Перед Стартом

- заполнены обязательные переменные в `/etc/vpnportal/vpn-host.<target>.env`
- SSH уже настроен для deploy user
- `DEPLOY_PATH` существует и доступен для записи
- после добавления deploy user в группу `docker` выполнен новый логин или `newgrp docker`
- GitHub Environment Secrets настроены отдельно для `stage` и `prod`

## Что Делать После Predeploy

1. Запустить обычный deploy workflow.
2. Убедиться, что миграции БД применились.
3. Создать первого `superadmin` по `docs/runbooks/create-first-superadmin.md`.
4. Прогнать `07-smoke-test-portal.sh` и затем `infrastructure/vpn-host/runbooks/verify-vpn-runtime-flow.md`.
