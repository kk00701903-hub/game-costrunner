#!/usr/bin/env python3
"""Coast Run — Unity MCP 서버 (Claude 데스크톱 로컬 MCP, 의존성 없음).

Claude 데스크톱 설정(claude_desktop_config.json)에 이렇게 등록:
  "mcpServers": { "unity": { "command": "C:\\\\Users\\\\ares2\\\\AppData\\\\Local\\\\Programs\\\\Python\\\\Python312\\\\python.exe",
                             "args": ["C:\\\\dev\\\\game\\\\Tools\\\\Mcp\\\\unity_mcp_server.py"] } }

도구:
  unity_launch      : 유니티 에디터를 프로젝트로 연다(이미 열려 있으면 그대로). 허브를 거치지 않는다.
  unity_status      : 에디터 연결/컴파일/플레이 상태 + 컴파일 에러 + 최근 로그
  unity_compile     : 에셋 새로고침 → 컴파일 끝날 때까지 기다림 → 에러 목록
  unity_cmd         : 에디터 원격 명령(play/stop/menu/scene/warp/clear/retry/hit/lane/jump/crouch/tap/key/shot/log ...)
  unity_shot        : 게임 뷰 스크린샷을 Tools/_shots/<name>.png 로 저장(파일 생성까지 대기)
  unity_log         : 최근 경고/에러 로그
에디터 쪽은 Assets/_CoastRun/Editor/CoastRemote.cs 가 127.0.0.1:47001 로 받는다.
"""
import json, os, socket, subprocess, sys, time

PROJECT = r"C:\dev\game"
UNITY_EXE = r"C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Unity.exe"
PORT = 47001


def send(cmd, timeout=25.0):
    s = socket.create_connection(("127.0.0.1", PORT), timeout=timeout)
    try:
        s.sendall((cmd + "\n").encode("utf-8"))
        buf = b""
        s.settimeout(timeout)
        while not buf.endswith(b"\n"):
            chunk = s.recv(65536)
            if not chunk:
                break
            buf += chunk
        txt = buf.decode("utf-8", "replace").strip()
        try:
            return json.loads(txt)
        except Exception:
            return {"raw": txt}
    finally:
        s.close()


def connected():
    try:
        return send("ping", 3.0)
    except Exception:
        return None


def unity_running():
    try:
        out = subprocess.run(["tasklist", "/FI", "IMAGENAME eq Unity.exe", "/FO", "CSV", "/NH"], capture_output=True, text=True, timeout=10).stdout
        return "Unity.exe" in out
    except Exception:
        return False


def tool_launch(args):
    if connected():
        return "이미 열려 있고 연결됨: " + json.dumps(connected(), ensure_ascii=False)
    if unity_running():
        return "Unity.exe 프로세스는 떠 있는데 브릿지(47001)에 연결이 안 됨 — 로딩/컴파일 중이거나 CoastRemote가 아직 안 붙음. unity_status로 다시 확인."
    exe = args.get("unity_exe") or UNITY_EXE
    proj = args.get("project") or PROJECT
    if not os.path.exists(exe):
        return "Unity.exe 없음: " + exe
    subprocess.Popen([exe, "-projectPath", proj], creationflags=getattr(subprocess, "DETACHED_PROCESS", 0) | getattr(subprocess, "CREATE_NEW_PROCESS_GROUP", 0), close_fds=True)
    wait = float(args.get("wait_seconds", 150))
    t0 = time.time()
    while time.time() - t0 < wait:
        time.sleep(3)
        r = connected()
        if r:
            return f"열림 ({int(time.time()-t0)}s): " + json.dumps(r, ensure_ascii=False)
    return f"실행은 했지만 {int(wait)}초 안에 브릿지 연결이 안 됨(첫 임포트 중일 수 있음). unity_status로 다시 확인."


def tool_status(args):
    r = connected()
    if not r:
        return json.dumps({"connected": False, "process": unity_running()}, ensure_ascii=False)
    try:
        st = send("status", 10)
    except Exception as e:
        st = {"error": str(e)}
    st["connected"] = True
    return json.dumps(st, ensure_ascii=False, indent=1)


def tool_compile(args):
    if not connected():
        return "에디터 미연결 — unity_launch 먼저."
    send("refresh", 30)
    wait = float(args.get("wait_seconds", 120))
    t0 = time.time()
    time.sleep(1.5)
    last = None
    while time.time() - t0 < wait:
        try:
            st = send("status", 10)
            last = st
            if not st.get("compiling") and not st.get("updating"):
                errs = st.get("compileErrors", "")
                return ("컴파일 완료 — 에러 없음" if not errs else "컴파일 에러:\n" + errs) + f"  ({int(time.time()-t0)}s)"
        except Exception:
            pass  # 도메인 리로드 중엔 연결이 잠깐 끊긴다
        time.sleep(2)
    return "컴파일 대기 시간 초과: " + json.dumps(last, ensure_ascii=False)


def tool_cmd(args):
    cmd = args.get("command", "").strip()
    if not cmd:
        return "command 필요"
    try:
        return json.dumps(send(cmd, float(args.get("timeout", 25))), ensure_ascii=False)
    except Exception as e:
        return "브릿지 오류: " + str(e)


def tool_shot(args):
    name = args.get("name") or ("shot_" + time.strftime("%H%M%S"))
    try:
        r = send("shot " + name, 10)
    except Exception as e:
        return "브릿지 오류: " + str(e)
    path = r.get("path") or os.path.join(PROJECT, "Tools", "_shots", name + ".png")
    t0 = time.time()
    while time.time() - t0 < 8:
        if os.path.exists(path) and os.path.getsize(path) > 1000:
            time.sleep(0.3)
            return json.dumps({"ok": True, "path": path, "bytes": os.path.getsize(path)}, ensure_ascii=False)
        time.sleep(0.25)
    return json.dumps({"ok": False, "path": path, "note": "파일이 안 생김 — 게임 뷰가 보이는 상태(플레이 중)여야 한다"}, ensure_ascii=False)


def tool_log(args):
    try:
        return send("log " + str(int(args.get("lines", 40))), 10).get("log", "")
    except Exception as e:
        return "브릿지 오류: " + str(e)


TOOLS = [
    {"name": "unity_launch", "description": "Unity 에디터를 프로젝트(C:\\dev\\game)로 직접 연다(허브 불필요). 이미 열려 있으면 연결 상태만 돌려준다.",
     "inputSchema": {"type": "object", "properties": {"wait_seconds": {"type": "number", "description": "브릿지 연결 대기(기본 150)"}}}},
    {"name": "unity_status", "description": "에디터 브릿지 연결/컴파일/플레이 상태, 컴파일 에러, 최근 경고·에러 로그.",
     "inputSchema": {"type": "object", "properties": {}}},
    {"name": "unity_compile", "description": "AssetDatabase.Refresh 후 컴파일이 끝날 때까지 기다리고 에러 목록을 돌려준다.",
     "inputSchema": {"type": "object", "properties": {"wait_seconds": {"type": "number"}}}},
    {"name": "unity_cmd", "description": "에디터 원격 명령 한 줄. play | stop | pause | menu <메뉴 경로> | scene <이름> | warp | clear | retry | hit | lane -1|1 | jump | crouch | tap <nx> <ny> | key <KeyCode> | timescale <f> | log [n] | clearlog | gameview",
     "inputSchema": {"type": "object", "properties": {"command": {"type": "string"}, "timeout": {"type": "number"}}, "required": ["command"]}},
    {"name": "unity_shot", "description": "게임 뷰 스크린샷을 Tools/_shots/<name>.png 로 저장하고 경로를 돌려준다(플레이 중).",
     "inputSchema": {"type": "object", "properties": {"name": {"type": "string"}}}},
    {"name": "unity_log", "description": "최근 경고/에러/예외 로그 n줄.",
     "inputSchema": {"type": "object", "properties": {"lines": {"type": "integer"}}}},
]
HANDLERS = {"unity_launch": tool_launch, "unity_status": tool_status, "unity_compile": tool_compile,
            "unity_cmd": tool_cmd, "unity_shot": tool_shot, "unity_log": tool_log}


def reply(msg_id, result=None, error=None):
    m = {"jsonrpc": "2.0", "id": msg_id}
    if error is not None:
        m["error"] = error
    else:
        m["result"] = result
    sys.stdout.write(json.dumps(m, ensure_ascii=False) + "\n")
    sys.stdout.flush()


def main():
    sys.stdin.reconfigure(encoding="utf-8")
    sys.stdout.reconfigure(encoding="utf-8")
    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue
        try:
            msg = json.loads(line)
        except Exception:
            continue
        mid = msg.get("id")
        method = msg.get("method", "")
        params = msg.get("params") or {}
        if method == "initialize":
            reply(mid, {"protocolVersion": params.get("protocolVersion", "2024-11-05"),
                        "capabilities": {"tools": {}},
                        "serverInfo": {"name": "coastrun-unity", "version": "1.0"}})
        elif method == "notifications/initialized" or mid is None:
            continue
        elif method == "ping":
            reply(mid, {})
        elif method == "tools/list":
            reply(mid, {"tools": TOOLS})
        elif method == "tools/call":
            name = params.get("name"); args = params.get("arguments") or {}
            h = HANDLERS.get(name)
            if not h:
                reply(mid, error={"code": -32601, "message": "unknown tool " + str(name)})
                continue
            try:
                text = h(args)
                reply(mid, {"content": [{"type": "text", "text": str(text)}], "isError": False})
            except Exception as e:
                reply(mid, {"content": [{"type": "text", "text": "오류: " + repr(e)}], "isError": True})
        else:
            reply(mid, error={"code": -32601, "message": "method not found: " + method})


if __name__ == "__main__":
    main()
