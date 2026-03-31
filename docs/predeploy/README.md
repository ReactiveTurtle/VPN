# Predeploy

Этот раздел описывает предварительные шаги, которые нужно выполнить до первого деплоя на новую машину.

В рамках этого репозитория predeploy включает:

- настройку SSH-доступа для деплоя
- подготовку целевой машины, ее layout и конфигурации
- разовые bootstrap-шаги, без которых штатный процесс деплоя не сможет отработать успешно

## Порядок чтения

Читайте документы в таком порядке:

1. `docs/predeploy/ssh-access.md`
2. `docs/predeploy/new-host-checklist.md`
3. `docs/deployment.md`
4. `deploy/README.md`

## Связанные материалы

- `deploy/README.md` - упаковка, поведение GitHub Actions workflow и удаленная deploy-команда
- `infrastructure/vpn-host/README.md` - подготовка single-host VPN-сервера, bootstrap и операционный layout
- `docs/deployment.md` - текущая схема деплоя и runtime contract
- `docs/runbooks/create-first-superadmin.md` - ручное создание первого `superadmin`
