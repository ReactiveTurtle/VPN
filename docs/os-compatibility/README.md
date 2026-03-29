# 📄 Документация: IKEv2 VPN на strongSwan (совместимость iPhone / Android / Windows)

## 🎯 Цель

Настроить VPN на strongSwan, который работает на:

* 📱 iPhone (iOS)
* 🤖 Android
* 🪟 Windows

---

# 🌐 Параметры сервера

* IP сервера: `194.87.83.193`
* Тип: **IKEv2 + EAP-MSCHAPv2**
* Домен: ❌ не используется (работа по IP)

---

# 🔐 Сертификат (КРИТИЧНО)

Сертификат должен содержать IP в SAN:

```text
Subject Alternative Name:
  IP Address:194.87.83.193
```

Проверка:

```bash
openssl x509 -in /etc/ipsec.d/certs/server-cert.pem -noout -text | grep -A2 "Subject Alternative Name"
```

---

# ⚙️ Конфигурация `/etc/ipsec.conf`

Рабочий вариант:

```conf
config setup
    charondebug="ike 1, knl 1, cfg 0"
    uniqueids=no

conn ikev2-vpn
    auto=add
    compress=no
    type=tunnel
    keyexchange=ikev2
    fragmentation=yes
    forceencaps=yes
    dpdaction=clear
    dpddelay=300s
    rekey=no

    left=%any
    leftid=194.87.83.193
    leftcert=server-cert.pem
    leftsendcert=always
    leftsubnet=0.0.0.0/0

    right=%any
    rightid=%any
    rightauth=eap-mschapv2
    rightsourceip=10.10.10.0/24
    rightdns=8.8.8.8,8.8.4.4,1.1.1.1
    rightsendcert=never
    eap_identity=%identity

    ike=aes256-sha256-modp2048,aes128-sha1-modp1024!
    esp=aes256-sha256,aes128-sha1!
```

---

# 🔑 Ключевой момент — шифры

## ❌ Проблема

Ошибка в логах:

```text
NO_PROP / received proposals unacceptable
```

Причина:

* iPhone не принимает старые/нестандартные алгоритмы
* Windows не поддерживает только новые

---

## ✅ Решение

Использовать комбинированный набор:

```conf
ike=aes256-sha256-modp2048,aes128-sha1-modp1024!
esp=aes256-sha256,aes128-sha1!
```

### Почему:

* 📱 iPhone → берёт `aes256-sha256-modp2048`
* 🪟 Windows → берёт `aes128-sha1-modp1024`
* 🤖 Android → поддерживает оба

---

# 📱 Настройки клиентов

## iPhone (iOS)

* Type: IKEv2
* Server: `194.87.83.193`
* Remote ID: `194.87.83.193`
* Local ID: *(пусто)*
* Auth: Username / Password

---

## Android

* Тип: IKEv2/IPSec MSCHAPv2
* Server: `194.87.83.193`
* Username / Password

---

## Windows

* Тип: IKEv2
* Встроенный VPN клиент
* Может использовать старые шифры → поэтому нужен fallback

---

# 🔍 Диагностика

Смотреть логи:

```bash
tail -f /var/log/syslog | grep charon
```

---

## Частые ошибки

### ❌ NO_PROP

→ несовместимые шифры

### ❌ AUTHENTICATION_FAILED

→ логин/пароль или сертификат

### ❌ нет логов

→ проблема с сетью / портами

---

# 🧠 Вывод

Для работы VPN на всех устройствах:

1. ✅ Сертификат с **IP в SAN**
2. ✅ `leftid = IP`
3. ✅ Совместимые шифры (modern + legacy)
4. ✅ Минимум “экзотики” в конфиге

---

# 🚀 Результат

После настройки:

* ✅ iPhone подключается
* ✅ Android подключается
* ✅ Windows подключается
* ✅ VPN стабильно работает