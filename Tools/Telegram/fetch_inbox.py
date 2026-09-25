"""196차: 텔레그램 답장(입력칸) 받아 저장 — 사용자 PC에서 실행(.env 봇 토큰).
   보고 메시지는 ForceReply(입력칸)로 나간다(send_outbox.py). 사용자가 입력칸에 다음 작업을 쓰면
   이 스크립트가 getUpdates 로 받아 Tools/Telegram/inbox/ 에 저장한다.
     inbox/inbox.jsonl  : 받은 메시지 전부(한 줄에 하나: time, message_id, reply_to, text)
     inbox/latest.txt   : 아직 확인 안 한 새 메시지들(Claude 가 읽고 채팅에서 확인 받은 뒤 비운다)
     inbox/offset.txt   : 다음 getUpdates offset
   보안: .env 의 TELEGRAM_ALLOWED_USER_ID 가 보낸 메시지만 저장한다. 저장만 하고 아무것도 실행하지 않는다.
    python Tools/Telegram/fetch_inbox.py
"""
import json, sys, time, urllib.request, urllib.parse
from pathlib import Path
try:
    sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")
except Exception:
    pass
sys.path.insert(0, str(Path(__file__).resolve().parent))
from report import load_env, send  # noqa

HERE = Path(__file__).resolve().parent
BOX = HERE / "inbox"
JSONL, LATEST, OFFSET, RES = BOX / "inbox.jsonl", BOX / "latest.txt", BOX / "offset.txt", BOX / "fetch.result"


def main():
    BOX.mkdir(exist_ok=True)
    env = load_env()
    tok = env.get("TELEGRAM_BOT_TOKEN"); allowed = str(env.get("TELEGRAM_ALLOWED_USER_ID", "")).strip()
    if not tok or not allowed:
        RES.write_text("ERR no token/allowed id", encoding="utf-8"); print("ERR no token"); return
    off = 0
    if OFFSET.is_file():
        try: off = int(OFFSET.read_text(encoding="utf-8").strip() or "0")
        except ValueError: off = 0
    q = urllib.parse.urlencode({"offset": off, "timeout": 0, "allowed_updates": json.dumps(["message"])})
    try:
        with urllib.request.urlopen(f"https://api.telegram.org/bot{tok}/getUpdates?{q}", timeout=30) as r:
            res = json.loads(r.read().decode())
    except Exception as e:
        RES.write_text(f"ERR {e}", encoding="utf-8"); print("ERR", e); return
    if not res.get("ok"):
        RES.write_text(f"ERR {res}", encoding="utf-8"); print("ERR", res); return
    got = 0; new_off = off; acks = []
    for u in res.get("result", []):
        new_off = max(new_off, u["update_id"] + 1)
        m = u.get("message") or {}
        frm = str((m.get("from") or {}).get("id", "")); chat = str((m.get("chat") or {}).get("id", ""))
        text = m.get("text")
        if not text or (frm != allowed and chat != allowed):
            continue   # 허용된 사용자 말고는 버린다
        rec = {"time": time.strftime("%Y-%m-%d %H:%M:%S", time.localtime(m.get("date", time.time()))),
               "message_id": m.get("message_id"), "reply_to": (m.get("reply_to_message") or {}).get("message_id"), "text": text}
        with JSONL.open("a", encoding="utf-8") as f: f.write(json.dumps(rec, ensure_ascii=False) + "\n")
        with LATEST.open("a", encoding="utf-8") as f: f.write(f"[{rec['time']}] {text}\n")
        got += 1
        acks.append(text)
    OFFSET.write_text(str(new_off), encoding="utf-8")
    # 200차: 받은 요청마다 바로 「받았다」 답장(자동 수신 확인). 실행은 Claude 채팅 세션에서 확인 뒤.
    if "--ack" in sys.argv:
        for t in acks:
            try: send("📥 요청 받음: " + (t[:60] + ("…" if len(t) > 60 else "")) + "\n→ Claude 작업 세션에서 확인하고 진행합니다(세션이 열려 있지 않으면 다음 대화 때).")
            except Exception as e: print("ack fail", e)
    RES.write_text(f"ok {got}", encoding="utf-8")
    print("ok", got, flush=True)


if __name__ == "__main__":
    main()
