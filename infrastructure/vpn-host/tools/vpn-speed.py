#!/usr/bin/env python3
import re
import shutil
import subprocess
import sys
import time

INTERVAL = 1
PING_TIMEOUT = "0.2"

est_re = re.compile(
    r"^\s*(ikev2-vpn\[\d+\]): ESTABLISHED (.*?), .*?\.\.\.([0-9.]+)\[([^\]]+)\]"
)
eap_re = re.compile(
    r"^\s*(ikev2-vpn\[\d+\]): Remote EAP identity: (.+)$"
)
ike_re = re.compile(
    r"^\s*(ikev2-vpn\[\d+\]): IKE proposal: (.+)$"
)
child_inst_re = re.compile(
    r"^\s*(ikev2-vpn\{\d+\}):\s+INSTALLED"
)
bytes_re = re.compile(
    r"^\s*(ikev2-vpn\{\d+\}):\s+(.+?),\s+(\d+)\s+bytes_i.*?(\d+)\s+bytes_o"
)
vip_re = re.compile(
    r"^\s*(ikev2-vpn\{\d+\}):\s+0\.0\.0\.0/0 === (.+)$"
)

prev = {}


def human_bytes(v):
    units = ["B", "KB", "MB", "GB", "TB"]
    v = float(v)
    for u in units:
        if v < 1024 or u == units[-1]:
            return f"{v:.1f} {u}"
        v /= 1024


def human_rate(v):
    units = ["B/s", "KB/s", "MB/s", "GB/s"]
    v = float(v)
    for u in units:
        if v < 1024 or u == units[-1]:
            return f"{v:.1f} {u}"
        v /= 1024


def trim(text, width):
    text = str(text)
    return text if len(text) <= width else text[: width - 1] + "…"


def color_rate(v):
    if v > 1024 * 1024:
        return "\033[92m"  # green
    if v > 100 * 1024:
        return "\033[93m"  # yellow
    return "\033[90m"  # gray


def reset():
    return "\033[0m"


def ping_host(ip):
    try:
        out = subprocess.check_output(
            ["ping", "-c", "1", "-W", PING_TIMEOUT, ip],
            stderr=subprocess.DEVNULL,
            text=True,
        )
        m = re.search(r"time=([\d.]+)\s*ms", out)
        return f"{m.group(1)} ms" if m else "-"
    except Exception:
        return "-"


def get_status():
    out = subprocess.check_output(["ipsec", "statusall"], text=True)

    conns = []
    eap = {}
    ike = {}
    children = []
    child_info = {}

    for line in out.splitlines():
        m = est_re.match(line)
        if m:
            conns.append(
                {
                    "conn": m.group(1),
                    "uptime": m.group(2),
                    "client_ip": m.group(3),
                    "client_id": m.group(4),
                }
            )
            continue

        m = eap_re.match(line)
        if m:
            eap[m.group(1)] = m.group(2)
            continue

        m = ike_re.match(line)
        if m:
            ike[m.group(1)] = m.group(2)
            continue

        m = child_inst_re.match(line)
        if m:
            children.append(m.group(1))
            child_info.setdefault(m.group(1), {})
            continue

        m = bytes_re.match(line)
        if m:
            child = m.group(1)
            child_info.setdefault(child, {})
            child_info[child]["esp"] = m.group(2)
            child_info[child]["bytes_in"] = int(m.group(3))
            child_info[child]["bytes_out"] = int(m.group(4))
            continue

        m = vip_re.match(line)
        if m:
            child = m.group(1)
            child_info.setdefault(child, {})
            child_info[child]["vip"] = m.group(2)
            continue

    rows = []
    for i, c in enumerate(conns):
        child = children[i] if i < len(children) else None
        info = child_info.get(child, {})
        rows.append(
            {
                "conn": c["conn"],
                "client_ip": c["client_ip"],
                "client_id": c["client_id"],
                "eap": eap.get(c["conn"], "-"),
                "uptime": c["uptime"],
                "ike": ike.get(c["conn"], "-"),
                "esp": info.get("esp", "-"),
                "vip": info.get("vip", "-"),
                "bytes_in": info.get("bytes_in", 0),
                "bytes_out": info.get("bytes_out", 0),
            }
        )
    return rows


def render():
    width = shutil.get_terminal_size((150, 40)).columns
    lines = []
    now_str = time.strftime("%F %T")
    lines.append(f"VPN monitor  {now_str}")
    lines.append("")

    try:
        rows = get_status()
    except Exception as e:
        lines.append(f"Ошибка: {e}")
        return "\n".join(lines)

    if not rows:
        lines.append("Нет активных VPN-подключений")
        return "\n".join(lines)

    header = f"{'CONN':14} {'CLIENT_IP':15} {'USER':12} {'VPN_IP':18} {'PING':10} {'RX':12} {'TX':12} {'TOTAL':12} {'UPTIME':18}"
    lines.append(header)
    lines.append("-" * min(len(header), width))

    now = time.time()
    for r in rows:
        key = r["conn"]
        old = prev.get(key)

        if old:
            dt = max(now - old["ts"], 1e-6)
            rx = max((r["bytes_in"] - old["bytes_in"]) / dt, 0)
            tx = max((r["bytes_out"] - old["bytes_out"]) / dt, 0)
        else:
            rx = tx = 0

        prev[key] = {
            "bytes_in": r["bytes_in"],
            "bytes_out": r["bytes_out"],
            "ts": now,
        }

        total = r["bytes_in"] + r["bytes_out"]
        ping = ping_host(r["client_ip"])

        rx_s = human_rate(rx)
        tx_s = human_rate(tx)

        line = (
            f"{trim(r['conn'], 14):14} "
            f"{trim(r['client_ip'], 15):15} "
            f"{trim(r['eap'], 12):12} "
            f"{trim(r['vip'], 18):18} "
            f"{trim(ping, 10):10} "
            f"{color_rate(rx)}{trim(rx_s, 12):12}{reset()} "
            f"{color_rate(tx)}{trim(tx_s, 12):12}{reset()} "
            f"{trim(human_bytes(total), 12):12} "
            f"{trim(r['uptime'], 18):18}"
        )
        lines.append(line)

    lines.append("")
    lines.append(
        "PING = до внешнего IP клиента. Серый < 100 KB/s, жёлтый > 100 KB/s, зелёный > 1 MB/s"
    )
    return "\n".join(lines)


def main():
    try:
        subprocess.run(
            ["tput", "civis"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL
        )
        first = True
        while True:
            frame = render()
            if first:
                sys.stdout.write(frame)
                first = False
            else:
                sys.stdout.write("\033[H")
                sys.stdout.write(frame)
                sys.stdout.write("\033[J")
            sys.stdout.flush()
            time.sleep(INTERVAL)
    except KeyboardInterrupt:
        pass
    finally:
        subprocess.run(
            ["tput", "cnorm"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL
        )
        sys.stdout.write("\n")
        sys.stdout.flush()


if __name__ == "__main__":
    main()
