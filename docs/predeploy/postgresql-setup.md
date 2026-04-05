# PostgreSQL Setup

Этот документ описывает создание PostgreSQL пользователей и базы данных, необходимых для работы VPN Portal.

## Зачем Нужны Пользователи

Для работы приложения и FreeRADIUS требуются:

- База данных `vpn_portal`
- Пользователь `vpn_portal_app` — для подключения приложения к БД
- Пользователь `vpn_portal_radius` — для подключения FreeRADIUS к БД

В текущем bootstrap-потоке single-host сервера шаг `deploy/predeploy/infrastructure/vpn-host/03-install-and-init-postgres.sh` автоматически создает обе роли и проверяет, что:

- роль `vpn_portal_app` существует
- роль `vpn_portal_radius` существует
- база данных существует
- обе роли имеют `CONNECT` к `vpn_portal`

## Предварительные Требования

Для текущей single-host topology основной путь подготовки PostgreSQL уже автоматизирован шагом `deploy/predeploy/infrastructure/vpn-host/03-install-and-init-postgres.sh`. Перед ним рекомендуется запускать `deploy/predeploy/infrastructure/vpn-host/00-validate-env.sh`, чтобы сразу увидеть, каких переменных не хватает.

Повторная read-only проверка этих ролей и `CONNECT`-прав также выполняется шагом `deploy/predeploy/infrastructure/vpn-host/07-verify-stack.sh`.

Ручной SQL-сценарий ниже нужен, если вы настраиваете PostgreSQL вне стандартного bootstrap потока или хотите воспроизвести тот же результат вручную.

1. PostgreSQL уже установлен на машине
2. У вас есть доступ с правами `postgres` пользователя или `sudo` для запуска `psql`

## Подключение К PostgreSQL

Подключитесь к PostgreSQL от имени системного пользователя `postgres`:

```bash
sudo -u postgres psql
```

Если вы настроили аутентификацию `peer` для `postgres` пользователя, используйте:

```bash
sudo -u postgres psql -d vpn_portal
```

## Создание Пользователей И Базы Данных

Для стандартного single-host bootstrap этот раздел обычно не нужен: роли и база данных создаются автоматически шагом `03-install-and-init-postgres.sh`. Ниже остается ручной эквивалент для нестандартной подготовки или отладки.

Выполните следующие SQL-команды в psql:

```sql
-- Создаем пользователя для приложения
CREATE USER vpn_portal_app WITH PASSWORD 'change-me-secure-password';

-- Создаем пользователя для FreeRADIUS
CREATE USER vpn_portal_radius WITH PASSWORD 'change-me-secure-password-radius';

-- Создаем базу данных
CREATE DATABASE vpn_portal OWNER vpn_portal_app;

-- Даем права приложению
GRANT ALL PRIVILEGES ON DATABASE vpn_portal TO vpn_portal_app;

-- Даем права FreeRADIUS
GRANT ALL PRIVILEGES ON DATABASE vpn_portal TO vpn_portal_radius;
```

После этого подключитесь к базе данных и дайте права на схему:

```sql
\c vpn_portal
GRANT ALL ON SCHEMA public TO vpn_portal_app;
GRANT ALL ON SCHEMA public TO vpn_portal_radius;
```

## Проверка Подключения

Проверьте, что пользователь может подключиться:

```bash
psql -h localhost -U vpn_portal_app -d vpn_portal
```

Введите пароль, который задали при создании пользователя.

## Настройка Подключения Из Контейнера

Если PostgreSQL работает на том же хосте, что и Docker контейнеры приложения, используйте `host.docker.internal` вместо `localhost`:

```env
Database__ConnectionString=Host=host.docker.internal;Port=5432;Database=vpn_portal;Username=vpn_portal_app;Password=your-password-here
```

Это связано с тем, что контейнеры видят хост через特殊的 DNS имя `host.docker.internal`.

## Связанные Документы

- `docs/predeploy/new-host-checklist.md` — подготовка хоста
- `infrastructure/vpn-host/README.md` — если PostgreSQL разворачивается через VPN host bootstrap
- `docs/runbooks/create-first-superadmin.md` — создание первого администратора после настройки БД
