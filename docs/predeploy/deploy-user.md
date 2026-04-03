# Deploy User

Этот документ описывает создание отдельного пользователя для деплоя на новой машине.

## Зачем Нужен Отдельный Пользователь

Выделенный deploy-пользователь упрощает контроль доступа и позволяет не использовать для деплоя обычную административную учетную запись.

Для текущего workflow этот пользователь должен:

- принимать SSH-подключение по ключу
- иметь доступ к каталогу деплоя
- входить в группу `docker`, если deploy будет запускать `docker compose` без `sudo`
- при необходимости выполнять ограниченный набор `sudo`-действий, которые использует rollout для записи env-файла в `/etc/vpnportal`

## Пример Создания Пользователя

Создайте системного пользователя, домашний каталог и shell:

```bash
sudo useradd -m -s /bin/bash deploy
```

Если пользователь уже существует, этот шаг можно пропустить.

## Подготовка SSH-Каталога

Создайте каталог для SSH-ключей и назначьте правильные права:

```bash
sudo install -d -m 700 -o deploy -g deploy /home/deploy/.ssh
```

После этого откройте файл `authorized_keys` через `nano`:

```bash
sudo nano /home/deploy/.ssh/authorized_keys
```

Дальше:

1. Откройте файл `vpnportal-deploy-key.pub` на той машине, где вы генерировали ключ.
2. Скопируйте всю строку целиком, начиная с `ssh-ed25519` или `ssh-rsa`.
3. Вставьте эту строку в открытый `nano` без переносов и без дополнительных символов.
4. Сохраните файл через `Ctrl+O`, нажмите `Enter`.
5. Выйдите из `nano` через `Ctrl+X`.

После сохранения выставьте владельца и права:

```bash
sudo chown deploy:deploy /home/deploy/.ssh/authorized_keys
sudo chmod 600 /home/deploy/.ssh/authorized_keys
```

В файле `authorized_keys` каждая строка должна содержать ровно один публичный ключ.

## Доступ К Каталогам Деплоя

Создайте директорию для деплоя и выдайте полные права пользователю deploy:

```bash
sudo mkdir -p /opt/vpnportal
sudo chown -R deploy:deploy /opt/vpnportal
sudo chmod 755 /opt/vpnportal
```

После этого:
- Пользователь deploy владеет `/opt/vpnportal`
- Может создавать подкаталоги (releases/, bin/, predeploy/) без sudo
- GitHub Actions workflow работает без необходимости sudo для создания папок

Если вы переходите на Docker-based rollout, дополнительно добавьте пользователя в группу `docker`:

```bash
sudo usermod -aG docker deploy
```

После этого перелогиньтесь под пользователем `deploy` или выполните `newgrp docker`.

## Sudo-Права Для Деплоя

Текущий Docker-based rollout использует `sudo` для:

- `install` env-файла в `/etc/vpnportal`

Если вы хотите использовать отдельного deploy-пользователя без полного root-доступа, настройте ограниченное правило в `/etc/sudoers.d/`.

Пример:

```bash
sudo tee /etc/sudoers.d/vpnportal-deploy >/dev/null <<'EOF'
deploy ALL=(root) NOPASSWD: /usr/bin/install
EOF
sudo chmod 440 /etc/sudoers.d/vpnportal-deploy
```

После этого обязательно проверьте файл через:

```bash
sudo visudo -cf /etc/sudoers.d/vpnportal-deploy
```

## Что Указать В GitHub Environment Secrets

После подготовки пользователя используйте его в deployment secrets нужного GitHub Environment:

- `DEPLOY_USER=deploy`
- `DEPLOY_PATH=/opt/vpnportal`

Если используется отдельный `stage` layout, укажите соответствующий путь в secrets окружения `stage`.

Рекомендуемые значения для GitHub Environment `stage`:

- `DEPLOY_HOST=<staging-host-or-ip>`
- `DEPLOY_PORT=<staging-ssh-port>`
- `DEPLOY_USER=deploy`
- `DEPLOY_PATH=/opt/vpnportal-stage`
- `DEPLOY_SSH_PRIVATE_KEY=<private key for the stage deploy user>`

`DEPLOY_COMMAND` для текущего deployment workflow не нужен и не используется.

`DEPLOY_HOST`, `DEPLOY_PORT`, `DEPLOY_USER`, `DEPLOY_PATH` и `DEPLOY_SSH_PRIVATE_KEY` должны храниться отдельно для `stage` и `prod`, чтобы branch deploy шел на staging host, а tag deploy - на production host.

## Проверка

Проверьте минимальный сценарий до первого workflow-run:

```bash
ssh -i ./vpnportal-deploy-key -p <port> deploy@<host>
test -w /opt/vpnportal
docker version
docker compose version
sudo -n install -m 0640 /dev/null /etc/vpnportal/deploy-user-check.tmp && sudo rm -f /etc/vpnportal/deploy-user-check.tmp
```

Если `sudo -n` завершается ошибкой, значит для пользователя еще не настроены нужные `sudoers`-права.

Дополнительно проверьте, что GitHub Environment secret `DEPLOY_PATH` не содержит скрытых символов в конце строки. Для текущего workflow это критично: лишний `\r`, перевод строки или пробел в `DEPLOY_PATH` приводит к падению первого `scp` с ошибкой `No such file or directory`, даже если сам каталог на сервере существует и доступен пользователю `deploy`.

## Связанные Документы

- `docs/predeploy/ssh-access.md`
- `docs/predeploy/new-host-checklist.md`
- `deploy/README.md`
