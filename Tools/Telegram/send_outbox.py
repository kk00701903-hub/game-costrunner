"""outbox.txt 한 파일을 텔레그램으로 보내고 비운다 (사용자 PC에서, .env 의 봇 토큰 사용).
   Cowork 세션은 api.telegram.org 가 막혀 있어 outbox.txt 를 쓰고 Unity 메뉴
   (Coast Run/Telegram/Send outbox — unity_cmd "menu ..." 로도 호출) 로 이 스크립트를 돌린다.
   결과는 sent.log 에 'O\t<ok>\t<첫줄>' 로 남고, 마지막 결과 한 줄이 outbox.result 에 남는다.
    python Tools/Telegram/send_outbox.py
"""
import sys
from pathlib import Path
try:
    sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")
except Exception:
    pass
sys.path.insert(0, str(Path(__file__).resolve().parent))
from report import send  # noqa

HERE = Path(__file__).resolve().parent
OUT, LOG, RES = HERE / "outbox.txt", HERE / "sent.log", HERE / "outbox.result"


def main():
    if not OUT.is_file():
        RES.write_text("no outbox", encoding="utf-8"); print("no outbox"); return
    msg = OUT.read_text(encoding="utf-8-sig").strip()
    if not msg:
        RES.write_text("empty", encoding="utf-8"); print("empty"); return
    try:
        ok = send(msg).get("ok")
    except Exception as e:
        ok = f"ERR {e}"
    first = msg.splitlines()[0][:60]
    with LOG.open("a", encoding="utf-8") as f:
        f.write(f"O\t{ok}\t{first}\n")
    RES.write_text(f"{ok}\t{first}", encoding="utf-8")
    if ok is True:
        OUT.write_text("", encoding="utf-8")
    print(ok, first, flush=True)


if __name__ == "__main__":
    main()
