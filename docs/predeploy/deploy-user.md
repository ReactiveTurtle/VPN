# Deploy User

Этот документ описывает создание отдельного пользователя для деплоя на новой машине.

## Зачем Нужен Отдельный Пользователь

Выделенный deploy-пользователь упрощает контроль доступа и позволяет не использовать для деплоя обычную административную учетную запись.

Для текущего workflow этот пользователь должен:

- принимать SSH-подключение по ключу
- иметь доступ к каталогу деплоя
- при необходимости выполнять ограниченный набор `sudo`-действий, которые использует deploy-скрипт

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

Минимально подготовьте layout, с которым будет работать deploy:

```bash
sudo install -d -m 755 /opt/vpnportal
sudo install -d -m 755 /opt/vpnportal/bin
sudo install -d -m 755 /opt/vpnportal/releases
sudo chown -R deploy:deploy /opt/vpnportal
```

Если используется `staging`, подготовьте отдельный root, например `/opt/vpnportal-staging`.

## Sudo-Права Для Деплоя

Текущий `deploy/remote/deploy-package.sh` использует `sudo` для:

- `install` в `/usr/local/bin`
- `systemctl daemon-reload`
- `systemctl restart <service>`
- `systemctl reload nginx`

Если вы хотите использовать отдельного deploy-пользователя без полного root-доступа, настройте ограниченное правило в `/etc/sudoers.d/`.

Пример:

```bash
sudo tee /etc/sudoers.d/vpnportal-deploy >/dev/null <<'EOF'
deploy ALL=(root) NOPASSWD: /usr/bin/install, /usr/bin/systemctl daemon-reload, /usr/bin/systemctl restart vpnportal-api, /usr/bin/systemctl restart vpnportal-api-staging, /usr/bin/systemctl reload nginx
EOF
sudo chmod 440 /etc/sudoers.d/vpnportal-deploy
```

После этого обязательно проверьте файл через:

```bash
sudo visudo -cf /etc/sudoers.d/vpnportal-deploy
```

## Что Указать В GitHub Secrets

После подготовки пользователя используйте его в deployment secrets:

- `DEPLOY_USER=deploy`
- `DEPLOY_PATH=/opt/vpnportal`

Если используется отдельный staging layout, укажите соответствующий путь под staging-конвенцию.

## Проверка

Проверьте минимальный сценарий до первого workflow-run:

```bash
ssh -i ./vpnportal-deploy-key -p <port> deploy@<host>
test -w /opt/vpnportal
sudo -n systemctl daemon-reload
```

Если `sudo -n` завершается ошибкой, значит для пользователя еще не настроены нужные `sudoers`-права.

## Связанные Документы

- `docs/predeploy/ssh-access.md`
- `docs/predeploy/new-host-checklist.md`
- `deploy/README.md`
