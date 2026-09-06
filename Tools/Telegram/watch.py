"""큐 파일 감시 → 텔레그램 전송 (사용자 PC에서 실행). 샌드박스에서 Tools/Telegram/queue.txt에 한 줄씩 append하면 보낸다.
   줄 안의 '\n'(리터럴 백슬래시-n)은 줄바꿈으로 바뀐다."""
import sys, time, json
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from report import send  # noqa

HERE = Path(__file__).resolve().parent
Q = HERE / "queue.txt"
LOG = HERE / "sent.log"

def main():
    print("telegram watcher on", Q, flush=True)
    done = 0
    if LOG.is_file():
        try: done = int(LOG.read_text(encoding="utf-8").splitlines()[-1].split("\t")[0])
        except Exception: done = 0
    try: send("[송전탑] 텔레그램 API 워처 시작 — 이 창을 닫으면 보고가 멈춥니다.")
    except Exception as e: print("send failed:", e)
    while True:
        try:
            lines = Q.read_text(encoding="utf-8").splitlines() if Q.is_file() else []
            while done < len(lines):
                msg = lines[done].replace("\\n", "\n").strip()
                done += 1
                if not msg: continue
                try:
                    r = send(msg); ok = r.get("ok")
                except Exception as e:
                    ok = f"ERR {e}"
                with LOG.open("a", encoding="utf-8") as f: f.write(f"{done}\t{ok}\t{msg[:60]}\n")
                print(done, ok, msg[:60], flush=True)
        except Exception as e:
            print("loop err", e, flush=True)
        time.sleep(3)

if __name__ == "__main__":
    main()
