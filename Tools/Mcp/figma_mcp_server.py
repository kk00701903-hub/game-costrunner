#!/usr/bin/env python3
"""Coast Run — Figma 브리지 MCP 서버 (Claude 데스크톱 로컬 MCP, stdio).

Figma 데스크톱에서 돌아가는 개발 플러그인 「Frequency Bridge」(Tools/Figma/plugin) 와
로컬 HTTP(127.0.0.1:47010) 롱폴링으로 이어져, 플러그인 API(JS) 를 원격 실행한다.
의존성 없음(표준 라이브러리만).

claude_desktop_config.json 등록 예:
  "figma": { "command": "...python.exe", "args": ["C:\\dev\\game\\Tools\\Mcp\\figma_mcp_server.py"] }

Figma 쪽 1회 설정: Plugins → Development → Import plugin from manifest…
  → C:\dev\game\Tools\Figma\plugin\manifest.json  → 이후 파일을 열고 플러그인 실행(창을 띄워 둔다).

도구:
  figma_ping           : 플러그인 연결 상태(마지막 폴링, 열린 문서·페이지)
  figma_run            : 플러그인 안에서 JS 실행 (async 본문, figma/args/H 사용 가능) → 반환값 JSON
  figma_pages          : 페이지 목록
  figma_selection      : 현재 선택 노드 요약
  figma_place_image    : 로컬 이미지 파일을 현재 페이지(또는 parent) 에 배치
  figma_import_assets  : Tools/Figma/frequency_assets.json 의 카테고리를 페이지+그리드로 이관
  figma_export_node    : 노드를 PNG 로 내보내 로컬 파일로 저장
"""
import json, sys, threading, time, uuid, os, queue, urllib.parse
from http.server import ThreadingHTTPServer, BaseHTTPRequestHandler
from pathlib import Path

ROOT = Path(r"C:\dev\game")
PORT = 47010
ALLOWED_ROOTS = [ROOT, Path(r"C:\Users\ares2\Claude"), Path(os.environ.get("TEMP", r"C:\Temp"))]
EXPORT_DIR = ROOT / "Tools" / "Figma" / "exports"

_pending = queue.Queue()          # 플러그인이 가져갈 명령
_results = {}                     # id -> result
_result_ev = {}                   # id -> Event
_state = {"last_poll": 0.0, "doc": "", "page": "", "client": ""}


def _allowed(p: Path):
    try:
        rp = p.resolve()
    except Exception:
        return False
    return any(str(rp).lower().startswith(str(r.resolve()).lower()) for r in ALLOWED_ROOTS)


class H(BaseHTTPRequestHandler):
    def log_message(self, *a):  # stdout 은 MCP 채널이므로 조용히
        pass

    def _cors(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Headers", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")

    def _json(self, obj, code=200):
        body = json.dumps(obj, ensure_ascii=False).encode("utf-8")
        self.send_response(code); self._cors()
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body))); self.end_headers()
        self.wfile.write(body)

    def do_OPTIONS(self):
        self.send_response(204); self._cors(); self.end_headers()

    def do_GET(self):
        u = urllib.parse.urlparse(self.path); q = urllib.parse.parse_qs(u.query)
        if u.path == "/poll":
            _state["last_poll"] = time.time()
            _state["doc"] = q.get("doc", [""])[0]; _state["page"] = q.get("page", [""])[0]; _state["client"] = q.get("client", [""])[0]
            try:
                cmd = _pending.get(timeout=20)
                self._json({"cmd": cmd})
            except queue.Empty:
                self._json({})
        elif u.path == "/health":
            self._json({"ok": True, "since_poll": round(time.time() - _state["last_poll"], 1) if _state["last_poll"] else None, **_state})
        elif u.path == "/file":
            p = Path(q.get("path", [""])[0])
            if not p.is_absolute():
                p = ROOT / p
            if not p.is_file() or not _allowed(p):
                self._json({"error": "not allowed or missing: " + str(p)}, 404); return
            data = p.read_bytes()
            ext = p.suffix.lower()
            ctype = {".png": "image/png", ".jpg": "image/jpeg", ".jpeg": "image/jpeg", ".webp": "image/webp", ".gif": "image/gif", ".svg": "image/svg+xml"}.get(ext, "application/octet-stream")
            self.send_response(200); self._cors()
            self.send_header("Content-Type", ctype); self.send_header("Content-Length", str(len(data))); self.end_headers()
            self.wfile.write(data)
        else:
            self._json({"error": "not found"}, 404)

    def do_POST(self):
        u = urllib.parse.urlparse(self.path); q = urllib.parse.parse_qs(u.query)
        n = int(self.headers.get("Content-Length") or 0)
        body = self.rfile.read(n) if n else b""
        if u.path == "/result":
            try:
                r = json.loads(body.decode("utf-8"))
            except Exception as e:
                self._json({"error": repr(e)}, 400); return
            rid = r.get("id")
            _results[rid] = r
            ev = _result_ev.get(rid)
            if ev: ev.set()
            self._json({"ok": True})
        elif u.path == "/save":
            p = Path(q.get("path", [""])[0])
            if not p.is_absolute():
                p = EXPORT_DIR / p
            if not _allowed(p):
                self._json({"error": "not allowed: " + str(p)}, 403); return
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_bytes(body)
            self._json({"ok": True, "path": str(p), "bytes": len(body)})
        else:
            self._json({"error": "not found"}, 404)


def start_http():
    srv = ThreadingHTTPServer(("127.0.0.1", PORT), H)
    threading.Thread(target=srv.serve_forever, daemon=True).start()
    return srv


def plugin_alive(max_age=30.0):
    return _state["last_poll"] > 0 and (time.time() - _state["last_poll"]) < max_age


def run_js(code, args=None, timeout=90):
    """플러그인 안에서 JS 실행. code 는 async 함수 본문(return 가능)."""
    if not plugin_alive():
        raise RuntimeError("Figma 플러그인이 연결돼 있지 않습니다. Figma 에서 파일을 열고 Plugins → Development → Frequency Bridge 를 실행해 두세요. (마지막 폴링: %s)" % (
            ("%.0f초 전" % (time.time() - _state["last_poll"])) if _state["last_poll"] else "없음"))
    rid = uuid.uuid4().hex[:10]
    ev = threading.Event(); _result_ev[rid] = ev
    _pending.put({"id": rid, "code": code, "args": args or {}})
    if not ev.wait(timeout):
        _result_ev.pop(rid, None)
        raise TimeoutError("플러그인 응답 없음 (%ds). 플러그인 창이 열려 있는지 확인." % timeout)
    r = _results.pop(rid, None); _result_ev.pop(rid, None)
    if not r.get("ok"):
        raise RuntimeError("Figma JS 오류: " + str(r.get("result")))
    return r.get("result")


# ── 도구 ────────────────────────────────────────────────────────────────
def tool_ping(args):
    info = {"plugin_connected": plugin_alive(), "since_poll_sec": round(time.time() - _state["last_poll"], 1) if _state["last_poll"] else None,
            "doc": _state["doc"], "page": _state["page"], "http": "http://127.0.0.1:%d" % PORT,
            "plugin_manifest": str(ROOT / "Tools" / "Figma" / "plugin" / "manifest.json")}
    return json.dumps(info, ensure_ascii=False)


def tool_run(args):
    r = run_js(args["code"], args.get("args"), int(args.get("timeout", 90)))
    s = json.dumps(r, ensure_ascii=False, default=str)
    return s if len(s) < 60000 else s[:60000] + "…(잘림)"


def tool_pages(args):
    return json.dumps(run_js("return figma.root.children.map(p => ({id: p.id, name: p.name, current: p.id === figma.currentPage.id}));"), ensure_ascii=False)


def tool_selection(args):
    return json.dumps(run_js("return figma.currentPage.selection.map(n => ({id: n.id, name: n.name, type: n.type, x: n.x, y: n.y, w: n.width, h: n.height}));"), ensure_ascii=False)


def tool_place_image(args):
    p = Path(args["path"])
    if not p.is_absolute(): p = ROOT / p
    if not p.is_file(): raise FileNotFoundError(str(p))
    js = """
const url = 'http://127.0.0.1:%d/file?path=' + encodeURIComponent(args.path);
const parent = args.parent_id ? await figma.getNodeByIdAsync(args.parent_id) : figma.currentPage;
const node = await H.placeImage(url, args.name || args.path.split(/[\\\\/]/).pop(), parent, args.x || 0, args.y || 0, args.max_w || 0);
return {id: node.id, name: node.name, w: node.width, h: node.height};
""" % PORT
    return json.dumps(run_js(js, {"path": str(p), "name": args.get("name"), "parent_id": args.get("parent_id"), "x": args.get("x", 0), "y": args.get("y", 0), "max_w": args.get("max_w", 0)}), ensure_ascii=False)


def tool_import_assets(args):
    man_path = Path(args.get("manifest") or (ROOT / "Tools" / "Figma" / "frequency_assets.json"))
    man = json.loads(man_path.read_text(encoding="utf-8"))
    want = args.get("categories")
    cats = [c for c in man["categories"] if not want or c["key"] in want or c["title"] in want]
    cols = int(args.get("cols", 8)); cell = int(args.get("cell", 240)); gap = 24
    batch = int(args.get("batch", 24))
    prefix = args.get("page_prefix", "Frequency / ")
    report = []
    for c in cats:
        page_name = prefix + c["title"]
        # 페이지(있으면 재사용) + 제목
        pid = run_js("""
let page = figma.root.children.find(p => p.name === args.name);
if (!page) { page = figma.createPage(); page.name = args.name; }
await figma.setCurrentPageAsync(page);
await page.loadAsync();
for (const ch of [...page.children]) ch.remove();
await H.title(page, args.title, args.sub);
return page.id;
""", {"name": page_name, "title": c["title"], "sub": "%d개 · %s" % (len(c["items"]), c.get("note", ""))})
        items = c["items"]
        placed = 0
        for b0 in range(0, len(items), batch):
            chunk = items[b0:b0 + batch]
            r = run_js("""
const page = await figma.getNodeByIdAsync(args.page_id);
await figma.setCurrentPageAsync(page);
let n = 0;
for (const it of args.items) {
  const i = it.index; const col = i %% args.cols, row = Math.floor(i / args.cols);
  const x = 40 + col * (args.cell + args.gap), y = 140 + row * (args.cell + args.gap + 34);
  try { await H.cellImage('http://127.0.0.1:%d/file?path=' + encodeURIComponent(it.path), it.name, page, x, y, args.cell); n++; }
  catch (e) { console.log('skip', it.name, String(e)); }
}
return n;
""" % PORT, {"page_id": pid, "items": [{"index": b0 + k, "path": it["import"], "name": it["name"]} for k, it in enumerate(chunk)],
                         "cols": cols, "cell": cell, "gap": gap}, timeout=240)
            placed += int(r or 0)
        report.append({"page": page_name, "items": len(items), "placed": placed})
    return json.dumps(report, ensure_ascii=False)


def tool_export_node(args):
    out = Path(args.get("out") or ("export_%s.png" % args["node_id"].replace(":", "_")))
    if not out.is_absolute(): out = EXPORT_DIR / out
    js = """
const n = await figma.getNodeByIdAsync(args.node_id);
const bytes = await n.exportAsync({format: 'PNG', constraint: {type: 'SCALE', value: args.scale || 1}});
const r = await fetch('http://127.0.0.1:%d/save?path=' + encodeURIComponent(args.out), {method: 'POST', body: bytes});
return await r.json();
""" % PORT
    return json.dumps(run_js(js, {"node_id": args["node_id"], "out": str(out), "scale": args.get("scale", 1)}), ensure_ascii=False)


TOOLS = [
    {"name": "figma_ping", "description": "Figma 플러그인(Frequency Bridge) 연결 상태와 열린 문서·페이지.", "inputSchema": {"type": "object", "properties": {}}},
    {"name": "figma_run", "description": "Figma 플러그인 안에서 JS 실행(async 함수 본문; figma, args, H(helpers: placeImage/cellImage/title/text/frame) 사용, return 값이 JSON 으로 돌아옴).",
     "inputSchema": {"type": "object", "required": ["code"], "properties": {"code": {"type": "string"}, "args": {"type": "object"}, "timeout": {"type": "integer"}}}},
    {"name": "figma_pages", "description": "페이지 목록.", "inputSchema": {"type": "object", "properties": {}}},
    {"name": "figma_selection", "description": "현재 선택된 노드 요약.", "inputSchema": {"type": "object", "properties": {}}},
    {"name": "figma_place_image", "description": "로컬 이미지 파일(프로젝트 상대 경로 가능)을 현재 페이지에 이미지 노드로 배치.",
     "inputSchema": {"type": "object", "required": ["path"], "properties": {"path": {"type": "string"}, "name": {"type": "string"}, "x": {"type": "number"}, "y": {"type": "number"}, "max_w": {"type": "number"}, "parent_id": {"type": "string"}}}},
    {"name": "figma_import_assets", "description": "Tools/Figma/frequency_assets.json(카테고리별 에셋 목록)을 Figma 페이지 'Frequency / <카테고리>' 로 그리드 이관. categories 로 일부만 가능.",
     "inputSchema": {"type": "object", "properties": {"manifest": {"type": "string"}, "categories": {"type": "array", "items": {"type": "string"}}, "cols": {"type": "integer"}, "cell": {"type": "integer"}, "batch": {"type": "integer"}, "page_prefix": {"type": "string"}}}},
    {"name": "figma_export_node", "description": "노드를 PNG 로 내보내 Tools/Figma/exports/ 에 저장.",
     "inputSchema": {"type": "object", "required": ["node_id"], "properties": {"node_id": {"type": "string"}, "out": {"type": "string"}, "scale": {"type": "number"}}}},
]
HANDLERS = {"figma_ping": tool_ping, "figma_run": tool_run, "figma_pages": tool_pages, "figma_selection": tool_selection,
            "figma_place_image": tool_place_image, "figma_import_assets": tool_import_assets, "figma_export_node": tool_export_node}


def reply(msg_id, result=None, error=None):
    m = {"jsonrpc": "2.0", "id": msg_id}
    if error is not None: m["error"] = error
    else: m["result"] = result
    sys.stdout.write(json.dumps(m, ensure_ascii=False) + "\n"); sys.stdout.flush()


def main():
    sys.stdin.reconfigure(encoding="utf-8"); sys.stdout.reconfigure(encoding="utf-8")
    try:
        start_http()
    except OSError as e:
        sys.stderr.write("HTTP %d 포트 사용 불가: %r (다른 인스턴스가 떠 있으면 그쪽이 브리지)\n" % (PORT, e))
    for line in sys.stdin:
        line = line.strip()
        if not line: continue
        try: msg = json.loads(line)
        except Exception: continue
        mid = msg.get("id"); method = msg.get("method", ""); params = msg.get("params") or {}
        if method == "initialize":
            reply(mid, {"protocolVersion": params.get("protocolVersion", "2024-11-05"), "capabilities": {"tools": {}}, "serverInfo": {"name": "coastrun-figma", "version": "1.0"}})
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
                reply(mid, error={"code": -32601, "message": "unknown tool " + str(name)}); continue
            try:
                reply(mid, {"content": [{"type": "text", "text": str(h(args))}], "isError": False})
            except Exception as e:
                reply(mid, {"content": [{"type": "text", "text": "오류: " + repr(e)}], "isError": True})
        else:
            reply(mid, error={"code": -32601, "message": "method not found: " + method})


if __name__ == "__main__":
    if "--serve-only" in sys.argv:      # 플러그인 단독 테스트용
        start_http(); print("http on", PORT); 
        while True: time.sleep(3600)
    main()
