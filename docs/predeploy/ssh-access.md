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
- `DEPLOY_SERVER_NAME`
- `DEPLOY_SSH_PRIVATE_KEY`

## Минимальные Требования К Доступу

До первого деплоя проверьте, что:

- целевая машина принимает SSH-подключения для пользователя деплоя
- пользователь деплоя имеет доступ к настроенному deployment path, например `/opt/vpnportal`
- SSH-ключ, настроенный в CI, соответствует целевому пользователю на новой машине
- `DEPLOY_PATH` уже существует на машине и доступен для записи пользователю деплоя

## GitHub Environment Secrets

Для текущего deployment workflow нужно подготовить SSH/deploy secrets и application runtime secrets в GitHub Environment Secrets. Во время deploy workflow рендерит их в host-side container env-файл на сервере.

Секреты должны быть заданы отдельно для каждого GitHub Environment:

- `stage` -> staging host, staging path и staging runtime values
- `prod` -> production host, production path и production runtime values

Так `DEPLOY_HOST` и остальные `DEPLOY_*` значения автоматически переключаются вместе с целевым окружением workflow.

SSH и deploy secrets:

- `DEPLOY_HOST`
- `DEPLOY_PORT`
- `DEPLOY_USER`
- `DEPLOY_PATH`
- `DEPLOY_SERVER_NAME`
- `DEPLOY_SSH_PRIVATE_KEY`

Важно:

- `DEPLOY_PATH` должен быть сохранен без скрытых символов в конце строки
- если в GitHub Environment Secret случайно попал `\r`, перевод строки или пробел в конце, `scp` может падать с `dest open ... No such file or directory`, даже если каталог на сервере реально существует и доступен пользователю `deploy`
- если есть подозрение на такой случай, удалите значение секрета и введите его заново вручную, например ровно `/opt/vpnportal`

Workflow также рендерит host-side файл `/etc/vpnportal/predeploy.<env>.env` из GitHub Environment Secrets. Этот файл используется `prepare-app-host.sh` как единый источник истины для app predeploy и должен содержать тот же `DEPLOY_PATH`, который обычный deploy использует для `scp` и релизов.

Рекомендуемые значения для GitHub Environment `stage`:

- `DEPLOY_HOST=<staging-host-or-ip>`
- `DEPLOY_PORT=<staging-ssh-port>`
- `DEPLOY_USER=deploy`
- `DEPLOY_PATH=/opt/vpnportal-stage`
- `DEPLOY_SERVER_NAME=stage-vpn.example.com`
- `DEPLOY_SSH_PRIVATE_KEY=<private key for the stage deploy user>`

`DEPLOY_COMMAND` в текущем `deploy.yml` не используется, поэтому добавлять его в GitHub Environment `stage` не нужно.

Application runtime secrets в GitHub Environment Secrets:

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

Во время deploy workflow преобразует эти GitHub secret names в runtime env keys приложения, например `Database__ConnectionString`, `Email__Host` и `InternalApi__SharedSecret`.

Во время deploy эти значения записываются на хост в один из файлов:

- `/etc/vpnportal/vpnportal.prod.container.env`
- `/etc/vpnportal/vpnportal.stage.container.env`

## Как Сгенерировать SSH-Ключ Для Деплоя

Если отдельной ключевой пары для деплоя еще нет, создайте ее заранее на доверенной машине:

```bash
ssh-keygen -t ed25519 -C "github-deploy-vpnportal" -f ./vpnportal-deploy-key
```

Эта команда создаст два файла:

- `./vpnportal-deploy-key` - приватный ключ
- `./vpnportal-deploy-key.pub` - публичный ключ

Дальше используйте их так:

1. Содержимое `./vpnportal-deploy-key` добавьте в GitHub Environment secret `DEPLOY_SSH_PRIVATE_KEY` для нужного окружения.
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
