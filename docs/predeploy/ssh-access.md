# SSH Access

SSH-подключение является обязательным predeploy-шагом для новой машины, потому что текущий процесс доставки и активации релизов работает через SSH.

Подготовку отдельного пользователя для деплоя см. в `docs/predeploy/deploy-user.md`.

## Зачем Это Нужно

Репозиторий упаковывается через GitHub Actions и деплоится по SSH. До первого деплоя целевая машина должна принимать SSH-подключения для того сценария, который будет использоваться для выкладки.

Это относится к:

- CI-деплою из GitHub Actions
- ручной административной проверке новой машины перед первым деплоем

## Какие Данные Нужны

Для целевой машины нужно подготовить:

- hostname или IP-адрес
- SSH-порт
- пользователя для деплоя
- приватный ключ этого пользователя

Текущий deployment workflow ожидает следующие GitHub environment secrets:

- `DEPLOY_HOST`
- `DEPLOY_PORT`
- `DEPLOY_USER`
- `DEPLOY_PATH`
- `DEPLOY_SSH_PRIVATE_KEY`

## Минимальные Требования К Доступу

До первого деплоя проверьте, что:

- целевая машина принимает SSH-подключения для пользователя деплоя
- пользователь деплоя имеет доступ к настроенному deployment path, например `/opt/vpnportal`
- SSH-ключ, настроенный в CI, соответствует целевому пользователю на новой машине
- `DEPLOY_PATH` уже существует на машине и доступен для записи пользователю деплоя

## GitHub Deployment Secrets

Для текущего deployment workflow нужно подготовить не только SSH secrets, но и application secrets для рендера `appsettings.{Environment}.json` во время packaging/deploy.

SSH и deploy secrets:

- `DEPLOY_HOST`
- `DEPLOY_PORT`
- `DEPLOY_USER`
- `DEPLOY_PATH`
- `DEPLOY_SSH_PRIVATE_KEY`

Application secrets для `appsettings.{Environment}.json`:

- `APP_DATABASE_CONNECTION_STRING`
- `APP_EMAIL_HOST`
- `APP_EMAIL_USERNAME`
- `APP_EMAIL_PASSWORD`
- `APP_EMAIL_FROM_EMAIL`
- `APP_EMAIL_FROM_NAME`
- `APP_EMAIL_PUBLIC_BASE_URL`
- `APP_INTERNAL_API_SHARED_SECRET`
- `APP_VPN_ACCESS_SERVER_ADDRESS`
- `APP_VPN_RUNTIME_DISCONNECT_SCRIPT_PATH`

## Как Сгенерировать SSH-Ключ Для Деплоя

Если отдельной ключевой пары для деплоя еще нет, создайте ее заранее на доверенной машине:

```bash
ssh-keygen -t ed25519 -C "github-deploy-vpnportal" -f ./vpnportal-deploy-key
```

Эта команда создаст два файла:

- `./vpnportal-deploy-key` - приватный ключ
- `./vpnportal-deploy-key.pub` - публичный ключ

Дальше используйте их так:

1. Содержимое `./vpnportal-deploy-key` добавьте в GitHub secret `DEPLOY_SSH_PRIVATE_KEY`.
2. Содержимое `./vpnportal-deploy-key.pub` добавьте на сервер в `authorized_keys` пользователя деплоя.

Рекомендации:

- используйте отдельную ключевую пару только для deploy-задач
- не добавляйте passphrase, если ключ должен использоваться GitHub Actions без дополнительного интерактива
- не коммитьте ключевые файлы в репозиторий и не храните их рядом с проектом дольше, чем это нужно

## Как Технически Добавить Ключ На Машину

На целевой машине публичный ключ нужно добавить в `~/.ssh/authorized_keys` именно того пользователя, под которым будет выполняться деплой.

1. Подключитесь к машине под пользователем с правом менять домашнюю директорию deploy-пользователя.
2. Создайте каталог `~/.ssh`, если его еще нет:

```bash
mkdir -p /home/<deploy-user>/.ssh
chmod 700 /home/<deploy-user>/.ssh
chown <deploy-user>:<deploy-user> /home/<deploy-user>/.ssh
```

3. Добавьте публичный ключ в `/home/<deploy-user>/.ssh/authorized_keys`:

```bash
printf '%s\n' '<public-key>' >> /home/<deploy-user>/.ssh/authorized_keys
chmod 600 /home/<deploy-user>/.ssh/authorized_keys
chown <deploy-user>:<deploy-user> /home/<deploy-user>/.ssh/authorized_keys
```

4. Убедитесь, что добавлен именно публичный ключ в формате `ssh-ed25519 ...` или `ssh-rsa ...`, а не приватный ключ.
5. Если SSH-сервер использует нестандартный домашний каталог или пользователя, скорректируйте путь к `authorized_keys` под фактический `HOME` этого пользователя.

Для проверки можно сразу выполнить вход с той машины, где хранится соответствующий приватный ключ:

```bash
ssh -i <private-key-path> -p <port> <deploy-user>@<host>
```

Если подключение не проходит, отдельно проверьте права на `~/.ssh` и `authorized_keys`, существование пользователя и настройки `PubkeyAuthentication`/`AuthorizedKeysFile` в `sshd_config`.

## Первая Проверка Подключения

До включения deployment workflow нужно подтвердить доступность новой машины.

Минимально нужно проверить, что:

- hostname корректно резолвится, а машина отвечает на ожидаемом SSH-порту
- пользователь деплоя может войти с нужным ключом
- ожидаемые целевые директории и путь к удаленной deploy-команде уже существуют

## Примечания

- Настройку SSH нужно рассматривать как часть predeploy, а не как часть обычного rollout релиза.
- Подготовка машины и layout приложения описаны в `docs/predeploy/new-host-checklist.md`.
- Упаковка и поведение workflow остаются описаны в `deploy/README.md`.
