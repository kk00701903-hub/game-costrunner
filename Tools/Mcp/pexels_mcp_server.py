#!/usr/bin/env python3
"""Coast Run - Pexels MCP 서버 (Claude 데스크톱 로컬 MCP, stdio).

.env 의 PEXELS_API_KEY 로 무료 사진/영상을 검색하고 내려받는다.
라이선스: Pexels License (상업적 사용 가능, 출처 표기 불필요). 사람 얼굴이 드러나는
소재를 게임에 쓸 때는 초상권을 따로 확인할 것.

의존성: requests

도구:
  pexels_ping           : 키 확인(잔여 쿼터 헤더 포함)
  pexels_search_photos  : 사진 검색
  pexels_search_videos  : 영상 검색
  pexels_curated        : 큐레이션 사진 피드
  pexels_download       : 검색 결과 id 또는 URL → 파일 저장
"""
import json, os, sys, time
from pathlib import Path

ROOT = Path(r"C:\dev\game")
DEFAULT_OUT = "Tools/Art/_pexels"


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


def _key():
    k = _env("PEXELS_API_KEY")
    if not k:
        raise RuntimeError("PEXELS_API_KEY 없음 (.env 확인)")
    return k


PHOTO_BASE = _env("PEXELS_API_BASE_URL") or "https://api.pexels.com/v1"
VIDEO_BASE = _env("PEXELS_VIDEOS_BASE_URL") or "https://api.pexels.com/videos"


def _get(url, params=None):
    import requests
    r = requests.get(url, headers={"Authorization": _key()}, params=params, timeout=60)
    if r.status_code == 401:
        raise RuntimeError("401 - PEXELS_API_KEY 가 잘못되었습니다.")
    r.raise_for_status()
    return r.json(), r.headers


def _resolve(path_str, default_dir=DEFAULT_OUT):
    p = Path(path_str) if path_str else None
    if p is None:
        return ROOT / default_dir
    return p if p.is_absolute() else ROOT / p


def tool_ping(args):
    data, h = _get(PHOTO_BASE + "/curated", {"per_page": 1})
    return json.dumps({"ok": True,
                       "limit": h.get("X-Ratelimit-Limit"),
                       "remaining": h.get("X-Ratelimit-Remaining")}, ensure_ascii=False)


def _photo_row(p):
    return {"id": p.get("id"), "w": p.get("width"), "h": p.get("height"),
            "photographer": p.get("photographer"), "alt": (p.get("alt") or "")[:100],
            "src": (p.get("src") or {}).get("original"),
            "preview": (p.get("src") or {}).get("medium")}


def tool_search_photos(args):
    params = {"query": args["query"], "per_page": int(args.get("per_page", 15)),
              "page": int(args.get("page", 1))}
    for k in ("orientation", "size", "color", "locale"):
        if args.get(k):
            params[k] = args[k]
    data, _ = _get(PHOTO_BASE + "/search", params)
    return json.dumps({"total": data.get("total_results"),
                       "photos": [_photo_row(p) for p in data.get("photos", [])]}, ensure_ascii=False)


def tool_curated(args):
    data, _ = _get(PHOTO_BASE + "/curated",
                   {"per_page": int(args.get("per_page", 15)), "page": int(args.get("page", 1))})
    return json.dumps({"photos": [_photo_row(p) for p in data.get("photos", [])]}, ensure_ascii=False)


def tool_search_videos(args):
    params = {"query": args["query"], "per_page": int(args.get("per_page", 10)),
              "page": int(args.get("page", 1))}
    for k in ("orientation", "size", "locale"):
        if args.get(k):
            params[k] = args[k]
    data, _ = _get(VIDEO_BASE + "/search", params)
    rows = []
    for v in data.get("videos", []):
        files = sorted((v.get("video_files") or []), key=lambda f: (f.get("width") or 0), reverse=True)
        rows.append({"id": v.get("id"), "duration": v.get("duration"),
                     "w": v.get("width"), "h": v.get("height"),
                     "user": (v.get("user") or {}).get("name"),
                     "src": files[0].get("link") if files else None,
                     "qualities": [{"q": f.get("quality"), "w": f.get("width"),
                                    "link": f.get("link")} for f in files[:4]]})
    return json.dumps({"total": data.get("total_results"), "videos": rows}, ensure_ascii=False)


def tool_download(args):
    import requests
    url = args.get("url")
    if not url:
        pid = args["id"]
        kind = args.get("kind", "photo")
        if kind == "video":
            data, _ = _get(VIDEO_BASE + "/videos/" + str(pid))
            files = sorted((data.get("video_files") or []), key=lambda f: (f.get("width") or 0), reverse=True)
            url = files[0]["link"]
        else:
            data, _ = _get(PHOTO_BASE + "/photos/" + str(pid))
            url = (data.get("src") or {}).get(args.get("size", "original"))
    out = _resolve(args.get("out"))
    if not out.suffix:
        ext = ".mp4" if args.get("kind") == "video" else ".jpg"
        out = out / ("pexels_{}_{}{}".format(args.get("id", "url"), time.strftime("%H%M%S"), ext))
    out.parent.mkdir(parents=True, exist_ok=True)
    with requests.get(url, stream=True, timeout=300) as r:
        r.raise_for_status()
        with open(out, "wb") as fh:
            for chunk in r.iter_content(1 << 16):
                fh.write(chunk)
    return json.dumps({"saved": str(out), "bytes": out.stat().st_size, "url": url}, ensure_ascii=False)


TOOLS = [
    {"name": "pexels_ping", "description": "Pexels 키 확인 및 남은 쿼터 조회.",
     "inputSchema": {"type": "object", "properties": {}}},
    {"name": "pexels_search_photos", "description": "Pexels 사진 검색(무료, 상업적 사용 가능).",
     "inputSchema": {"type": "object", "required": ["query"], "properties": {
         "query": {"type": "string"}, "per_page": {"type": "integer"}, "page": {"type": "integer"},
         "orientation": {"type": "string", "description": "landscape | portrait | square"},
         "size": {"type": "string", "description": "large | medium | small"},
         "color": {"type": "string"}, "locale": {"type": "string", "description": "ko-KR 등"}}}},
    {"name": "pexels_search_videos", "description": "Pexels 영상 검색. 해상도별 링크를 함께 준다.",
     "inputSchema": {"type": "object", "required": ["query"], "properties": {
         "query": {"type": "string"}, "per_page": {"type": "integer"}, "page": {"type": "integer"},
         "orientation": {"type": "string"}, "size": {"type": "string"}, "locale": {"type": "string"}}}},
    {"name": "pexels_curated", "description": "Pexels 큐레이션 사진 피드.",
     "inputSchema": {"type": "object", "properties": {
         "per_page": {"type": "integer"}, "page": {"type": "integer"}}}},
    {"name": "pexels_download", "description": "id(또는 url) 로 원본 저장. 기본 폴더 Tools/Art/_pexels.",
     "inputSchema": {"type": "object", "properties": {
         "id": {"type": "string"}, "url": {"type": "string"},
         "kind": {"type": "string", "description": "photo | video"},
         "size": {"type": "string", "description": "original | large2x | large | medium"},
         "out": {"type": "string"}}}},
]
HANDLERS = {"pexels_ping": tool_ping, "pexels_search_photos": tool_search_photos,
            "pexels_search_videos": tool_search_videos, "pexels_curated": tool_curated,
            "pexels_download": tool_download}


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
                        "serverInfo": {"name": "coastrun-pexels", "version": "1.0"}})
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
