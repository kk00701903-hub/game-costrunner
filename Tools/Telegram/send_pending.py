"""큐에 쌓였지만 아직 안 보낸 줄(sent.log 마지막 번호 이후)을 한 번 보내고 끝난다. 워처가 꺼져 있을 때 사용자 PC에서 실행.
    python Tools/Telegram/send_pending.py"""
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from report import send  # noqa

HERE = Path(__file__).resolve().parent
Q, LOG = HERE / "queue.txt", HERE / "sent.log"

def main():
    done = 0
    if LOG.is_file():
        try: done = int(LOG.read_text(encoding="utf-8").splitlines()[-1].split("\t")[0])
        except Exception: done = 0
    lines = Q.read_text(encoding="utf-8").splitlines() if Q.is_file() else []
    if done >= len(lines):
        print("nothing pending"); return
    while done < len(lines):
        msg = lines[done].replace("\\n", "\n").strip(); done += 1
        if not msg: continue
        try: ok = send(msg).get("ok")
        except Exception as e: ok = f"ERR {e}"
        with LOG.open("a", encoding="utf-8") as f: f.write(f"{done}\t{ok}\t{msg.splitlines()[0][:60]}\n")
        print(done, ok, msg.splitlines()[0][:60], flush=True)

if __name__ == "__main__":
    main()
