"""작업 진행 보고 — Telegram Bot API. .env의 TELEGRAM_BOT_TOKEN / TELEGRAM_ALLOWED_USER_ID 사용.
    python Tools/Telegram/report.py "메시지"
    python Tools/Telegram/report.py --test
"""
import json, os, sys, urllib.request, urllib.parse
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

def load_env():
    env = {}
    p = ROOT / ".env"
    if p.is_file():
        for line in p.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            k, v = line.split("=", 1)
            env[k.strip()] = v.strip().strip('"').strip("'")
    return env

def send(text: str) -> dict:
    env = load_env()
    tok = env.get("TELEGRAM_BOT_TOKEN") or os.environ.get("TELEGRAM_BOT_TOKEN")
    chat = env.get("TELEGRAM_ALLOWED_USER_ID") or os.environ.get("TELEGRAM_ALLOWED_USER_ID")
    if not tok or not chat:
        raise SystemExit("TELEGRAM_BOT_TOKEN / TELEGRAM_ALLOWED_USER_ID missing in .env")
    data = urllib.parse.urlencode({"chat_id": chat, "text": text, "disable_web_page_preview": "true"}).encode()
    req = urllib.request.Request(f"https://api.telegram.org/bot{tok}/sendMessage", data=data)
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.loads(r.read().decode())

if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--test":
        env = load_env(); tok = env.get("TELEGRAM_BOT_TOKEN")
        with urllib.request.urlopen(f"https://api.telegram.org/bot{tok}/getMe", timeout=30) as r:
            me = json.loads(r.read().decode())
        print("bot:", me.get("result", {}).get("username"))
        res = send("[송전탑] 텔레그램 API 통신 테스트 OK — 앞으로 단계별 시작/종료 보고는 이 채널로 보냅니다.")
        print("sent:", res.get("ok"))
    else:
        res = send(" ".join(sys.argv[1:]))
        print("sent:", res.get("ok"))
