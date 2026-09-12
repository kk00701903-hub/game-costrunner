#!/usr/bin/env python3
"""Coast Run - Mixamo MCP 서버 (Claude 데스크톱 로컬 MCP, stdio).

Adobe Mixamo 의 비공개 웹 API 를 감싼다. 애니메이션을 검색하고 FBX 로 내보내
Unity 프로젝트(Assets/Art/Animations/Mixamo)에 바로 떨군다.

인증: 공개 API 키가 없으므로 mixamo.com 로그인 세션의 access token 을 쓴다.
  1. https://www.mixamo.com 로그인
  2. F12 → Application → Local Storage → https://www.mixamo.com → access_token 값 복사
     (또는 Network 탭의 api/v1 요청 Authorization 헤더에서 Bearer 뒤 문자열)
  3. 프로젝트 루트 .env 에  MIXAMO_BEARER=eyJhbGciOi...  로 저장
  토큰은 보통 몇 시간~하루면 만료된다. mixamo_ping 이 401 을 주면 다시 복사할 것.

의존성: requests

claude_desktop_config.json 등록 예:
  "mixamo": { "command": "...python.exe", "args": ["C:\\dev\\game\\Tools\\Mcp\\mixamo_mcp_server.py"] }

도구:
  mixamo_ping       : 토큰 유효성 + 내 캐릭터 목록 확인
  mixamo_characters : 캐릭터(리그) 목록 - export 에 쓸 character_id 를 얻는다
  mixamo_search     : 애니메이션/모션팩 검색
  mixamo_details    : 단일 애니메이션 상세(파라미터 포함)
  mixamo_download   : 애니메이션 → FBX 내보내기 후 파일 저장(폴링 포함)
"""
import json, os, sys, time
from pathlib import Path

ROOT = Path(r"C:\dev\game")
API = "https://www.mixamo.com/api/v1"
DEFAULT_OUT = "Assets/Art/Animations/Mixamo"


# ── .env ─────────────────────────────────────────────────────────
def _env(key, default=None):
    v = os.environ.get(key)
    if v:
        return v.strip()
    f = ROOT / ".env"
    if f.exists():
        for line in f.read_text(encoding="utf-8", errors="ignore").splitlines():
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            k, _, val = line.partition("=")
            if k.strip() == key:
                return val.strip().strip('"').strip("'")
    return default


def _token():
    t = _env("MIXAMO_BEARER")
    if not t:
        raise RuntimeError(
            "MIXAMO_BEARER 없음. mixamo.com 로그인 후 Local Storage 의 access_token 을 "
            ".env 에 MIXAMO_BEARER=... 로 넣어 주세요.")
    return t


def _headers(json_body=False):
    h = {"X-Api-Key": "mixamo2",
         "Authorization": "Bearer " + _token(),
         "Accept": "application/json",
         "Origin": "https://www.mixamo.com",
         "Referer": "https://www.mixamo.com/",
         "User-Agent": "Mozilla/5.0 CoastRun-MCP"}
    if json_body:
        h["Content-Type"] = "application/json"
    return h


def _req(method, path, params=None, body=None, timeout=60):
    import requests
    url = path if path.startswith("http") else API + path
    r = requests.request(method, url, headers=_headers(body is not None),
                         params=params, json=body, timeout=timeout)
    if r.status_code == 401:
        raise RuntimeError("401 - MIXAMO_BEARER 토큰이 만료되었거나 잘못되었습니다. 다시 복사해 주세요.")
    r.raise_for_status()
    if not r.content:
        return {}
    try:
        return r.json()
    except Exception:
        return {"raw": r.text[:2000]}


def _resolve(path_str, default_dir=DEFAULT_OUT):
    p = Path(path_str) if path_str else None
    if p is None:
        return ROOT / default_dir
    return p if p.is_absolute() else ROOT / p


# ── tools ────────────────────────────────────────────────────────
def tool_ping(args):
    data = _req("GET", "/characters")
    chars = data.get("results") or data.get("characters") or []
    return json.dumps({"ok": True, "characters": len(chars),
                       "first": (chars[0].get("id") if chars else None)}, ensure_ascii=False)


def tool_characters(args):
    data = _req("GET", "/characters")
    chars = data.get("results") or data.get("characters") or []
    out = [{"id": c.get("id"), "name": c.get("name") or c.get("description"),
            "type": c.get("type")} for c in chars]
    return json.dumps({"count": len(out), "characters": out}, ensure_ascii=False)


def tool_search(args):
    params = {"page": int(args.get("page", 1)),
              "limit": int(args.get("limit", 24)),
              "order": args.get("order", ""),
              "type": args.get("type", "Motion,MotionPack"),
              "query": args.get("query", "")}
    cid = args.get("character_id")
    if cid:
        params["character_id"] = cid
    data = _req("GET", "/products", params=params)
    items = data.get("results") or []
    out = [{"id": it.get("id"), "name": it.get("name"), "type": it.get("type"),
            "description": (it.get("description") or "")[:120],
            "motions": len(it.get("motions") or []) or None,
            "thumbnail": it.get("thumbnail_animated") or it.get("thumbnail")}
           for it in items]
    return json.dumps({"total": data.get("pagination", {}).get("num_results", len(out)),
                       "page": params["page"], "results": out}, ensure_ascii=False)


def _product(pid, cid):
    return _req("GET", "/products/{}".format(pid), params={"similar": 0, "character_id": cid})


def tool_details(args):
    p = _product(args["id"], args.get("character_id", ""))
    d = p.get("details") or {}
    return json.dumps({"id": p.get("id"), "name": p.get("name"), "type": p.get("type"),
                       "params": (d.get("gms_hash") or {}).get("params"),
                       "duration": d.get("duration")}, ensure_ascii=False)[:4000]


def _gms(product, overrides=None):
    """제품 상세의 gms_hash 를 export 가 받는 형태로 바꾼다."""
    d = product.get("details") or {}
    g = dict(d.get("gms_hash") or {})
    raw = g.get("params") or []
    vals = []
    for i, p in enumerate(raw):
        # p = [name, default, min, max] 형태
        v = p[1] if isinstance(p, (list, tuple)) and len(p) > 1 else p
        if overrides and isinstance(overrides, list) and i < len(overrides) and overrides[i] is not None:
            v = overrides[i]
        vals.append(str(v))
    g["params"] = ",".join(vals)
    return g


def tool_download(args):
    pid = args["id"]
    cid = args.get("character_id") or ""
    if not cid:
        data = _req("GET", "/characters")
        chars = data.get("results") or data.get("characters") or []
        if not chars:
            raise RuntimeError("character_id 가 필요합니다. mixamo_characters 로 확인하세요.")
        cid = chars[0]["id"]

    product = _product(pid, cid)
    ptype = product.get("type") or "Motion"
    if ptype == "MotionPack":
        motions = (product.get("details") or {}).get("motions") or []
        gms = [_gms(m) for m in motions] or [_gms(product)]
    else:
        gms = [_gms(product, args.get("params"))]

    body = {"gms_hash": gms,
            "preferences": {"format": args.get("format", "fbx7_2019"),
                            "skin": "true" if args.get("skin", False) else "false",
                            "fps": str(args.get("fps", 30)),
                            "reducekf": str(args.get("reducekf", 0))},
            "character_id": cid,
            "type": ptype,
            "product_name": product.get("name")}
    _req("POST", "/animations/export", body=body)

    deadline = time.time() + int(args.get("timeout", 180))
    url, status = None, ""
    while time.time() < deadline:
        time.sleep(3)
        mon = _req("GET", "/characters/{}/monitor".format(cid))
        status = (mon.get("status") or "").lower()
        if status == "completed":
            url = mon.get("job_result")
            break
        if status in ("failed", "error"):
            raise RuntimeError("Mixamo export 실패: {}".format(mon.get("message") or mon))
    if not url:
        return json.dumps({"status": status or "pending", "timed_out": True}, ensure_ascii=False)

    import requests
    out = _resolve(args.get("out"))
    if not out.suffix:
        safe = "".join(ch if ch.isalnum() or ch in "-_ " else "_" for ch in (product.get("name") or pid)).strip()
        out = out / (safe.replace(" ", "_") + ".fbx")
    out.parent.mkdir(parents=True, exist_ok=True)
    with requests.get(url, stream=True, timeout=300) as r:
        r.raise_for_status()
        with open(out, "wb") as fh:
            for chunk in r.iter_content(1 << 16):
                fh.write(chunk)
    return json.dumps({"status": "completed", "name": product.get("name"),
                       "saved": str(out), "bytes": out.stat().st_size,
                       "hint": "Unity 에디터에서 Assets 새로고침 후 Rig=Humanoid 확인"}, ensure_ascii=False)


TOOLS = [
    {"name": "mixamo_ping", "description": "MIXAMO_BEARER 토큰 유효성 확인(캐릭터 수 반환). 401 이면 토큰 재복사.",
     "inputSchema": {"type": "object", "properties": {}}},
    {"name": "mixamo_characters", "description": "내 Mixamo 캐릭터(리그) 목록. export 에 쓰는 character_id 를 얻는다.",
     "inputSchema": {"type": "object", "properties": {}}},
    {"name": "mixamo_search", "description": "Mixamo 애니메이션/모션팩 검색. 예: query='run', type='Motion'.",
     "inputSchema": {"type": "object", "properties": {
         "query": {"type": "string"}, "page": {"type": "integer"}, "limit": {"type": "integer"},
         "type": {"type": "string", "description": "Motion | MotionPack | Motion,MotionPack"},
         "order": {"type": "string"}, "character_id": {"type": "string"}}}},
    {"name": "mixamo_details", "description": "애니메이션 상세(조절 가능한 파라미터·길이).",
     "inputSchema": {"type": "object", "required": ["id"], "properties": {
         "id": {"type": "string"}, "character_id": {"type": "string"}}}},
    {"name": "mixamo_download", "description": "애니메이션을 FBX 로 내보내 저장(기본 Assets/Art/Animations/Mixamo, skin 제외, 30fps).",
     "inputSchema": {"type": "object", "required": ["id"], "properties": {
         "id": {"type": "string"}, "character_id": {"type": "string"},
         "out": {"type": "string", "description": "파일 또는 폴더(프로젝트 상대 경로 가능)"},
         "format": {"type": "string", "description": "fbx7_2019 | fbx7_2014 | dae"},
         "skin": {"type": "boolean", "description": "true 면 메시 포함(기본 false: 애니메이션만)"},
         "fps": {"type": "integer"}, "reducekf": {"type": "integer"},
         "params": {"type": "array", "items": {"type": "number"}, "description": "mixamo_details 의 파라미터 값 덮어쓰기"},
         "timeout": {"type": "integer"}}}},
]
HANDLERS = {"mixamo_ping": tool_ping, "mixamo_characters": tool_characters,
            "mixamo_search": tool_search, "mixamo_details": tool_details,
            "mixamo_download": tool_download}


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
                        "serverInfo": {"name": "coastrun-mixamo", "version": "1.0"}})
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
