#!/usr/bin/env python3
"""Coast Run — Kling AI MCP 서버 (Claude 데스크톱 로컬 MCP, stdio).

Tools/KlingGen/kling_client.py 를 그대로 감싼다. 키는 프로젝트 루트 .env 에서 읽는다
(KLING_ACCESS_KEY + KLING_SECRET_KEY 또는 KLING_API_KEY). 의존성: requests, PyJWT
(pip install -r Tools/KlingGen/requirements.txt).

claude_desktop_config.json 등록 예:
  "kling": { "command": "...python.exe", "args": ["C:\\dev\\game\\Tools\\Mcp\\kling_mcp_server.py"] }

도구:
  kling_ping         : 계정/키 연결 확인
  kling_image        : 텍스트(+참조 이미지) → 이미지 생성 작업 제출. wait=true 면 완료까지 기다려 저장
  kling_image2video  : 정지 이미지 → 5/10초 영상 작업 제출(비동기). task_id 반환
  kling_status       : 작업 상태 1회 조회(image | video)
  kling_wait         : 작업 완료까지 대기 후 파일로 저장(최대 timeout 초)
  kling_download     : URL → 파일 저장
"""
import json, sys, time
from pathlib import Path

ROOT = Path(r"C:\dev\game")
sys.path.insert(0, str(ROOT / "Tools" / "KlingGen"))

_client = None


def client():
    global _client
    if _client is None:
        from kling_client import KlingClient  # noqa
        _client = KlingClient()
    return _client


def _extract(data):
    from kling_client import _extract_status, _extract_urls  # noqa
    st = _extract_status(data)
    d = data.get("data") if isinstance(data.get("data"), dict) else data
    urls = list(_extract_urls(data) or [])
    vids = ((d.get("task_result") or {}).get("videos")) or []
    urls += [v["url"] for v in vids if isinstance(v, dict) and v.get("url")]
    return st, urls


def _resolve(path_str, default_dir):
    p = Path(path_str) if path_str else None
    if p is None:
        return ROOT / default_dir
    return p if p.is_absolute() else ROOT / p


def tool_ping(args):
    c = client()
    return json.dumps({"auth_mode": c.auth_mode, "base_url": c.base_url, "ping": c.ping()}, ensure_ascii=False)[:2000]


def tool_image(args):
    c = client()
    ref = args.get("reference_image")
    if ref and not ref.startswith("http"):
        ref = str(_resolve(ref, "."))
    tid = c.generate_image(
        prompt=args["prompt"], negative_prompt=args.get("negative_prompt"),
        model=args.get("model", "kling-v1"), aspect_ratio=args.get("aspect_ratio", "9:16"),
        n=int(args.get("n", 1)), reference_image=ref, reference_type=args.get("reference_type"),
        image_fidelity=args.get("image_fidelity"))
    if not args.get("wait", True):
        return json.dumps({"task_id": tid, "kind": "image"}, ensure_ascii=False)
    return tool_wait({"task_id": tid, "kind": "image", "out": args.get("out"), "timeout": args.get("timeout", 300)})


def tool_image2video(args):
    c = client()
    img = args["image"]
    if not img.startswith("http"):
        img = str(_resolve(img, "."))
    body = {"model_name": args.get("model", "kling-v1-6"), "image": c._encode_image(img),
            "prompt": args["prompt"],
            "negative_prompt": args.get("negative_prompt", "text, letters, watermark, logo, distorted face, extra limbs, flicker, photorealistic"),
            "cfg_scale": float(args.get("cfg_scale", 0.5)), "mode": args.get("mode", "std"),
            "duration": str(args.get("duration", "5"))}
    from kling_client import _extract_task_id  # noqa
    data = c._request("POST", "/v1/videos/image2video", json_body=body)
    tid = _extract_task_id(data)
    if not tid:
        raise RuntimeError(f"no task id: {data}")
    return json.dumps({"task_id": tid, "kind": "video", "hint": "kling_wait 또는 kling_status 로 확인"}, ensure_ascii=False)


def _endpoint(kind):
    return "/v1/videos/image2video/" if kind == "video" else "/v1/images/generations/"


def tool_status(args):
    data = client()._request("GET", _endpoint(args.get("kind", "image")) + args["task_id"])
    st, urls = _extract(data)
    return json.dumps({"task_id": args["task_id"], "status": st or "pending", "urls": urls}, ensure_ascii=False)


def tool_wait(args):
    c = client()
    kind = args.get("kind", "image")
    tid = args["task_id"]
    deadline = time.time() + int(args.get("timeout", 300))
    st, urls = "", []
    while time.time() < deadline:
        st, urls = _extract(c._request("GET", _endpoint(kind) + tid))
        if st in ("succeed", "success", "completed"):
            break
        if st in ("failed", "error"):
            raise RuntimeError(f"task failed: {tid} ({st})")
        time.sleep(8 if kind == "video" else 4)
    else:
        return json.dumps({"task_id": tid, "status": st or "pending", "timed_out": True}, ensure_ascii=False)
    saved = []
    if urls:
        out = _resolve(args.get("out"), "Tools/KlingGen/out/raw")
        ext = ".mp4" if kind == "video" else ".png"
        if out.suffix:
            targets = [out] if len(urls) == 1 else [out.with_name(f"{out.stem}_{i+1}{out.suffix}") for i in range(len(urls))]
        else:
            stamp = time.strftime("%Y%m%d_%H%M%S")
            targets = [out / f"kling_{stamp}_{i+1}{ext}" for i in range(len(urls))]
        for u, t in zip(urls, targets):
            saved.append(str(c.download(u, t)))
    return json.dumps({"task_id": tid, "status": st, "urls": urls, "saved": saved}, ensure_ascii=False)


def tool_download(args):
    dest = _resolve(args["out"], ".")
    return str(client().download(args["url"], dest))


TOOLS = [
    {"name": "kling_ping", "description": "Kling AI 키·계정 연결 확인(.env 의 KLING_* 사용).",
     "inputSchema": {"type": "object", "properties": {}}},
    {"name": "kling_image", "description": "Kling 이미지 생성. 기본 9:16, wait=true 면 완료까지 기다려 파일로 저장(out: 파일 또는 폴더, 기본 Tools/KlingGen/out/raw).",
     "inputSchema": {"type": "object", "required": ["prompt"], "properties": {
         "prompt": {"type": "string"}, "negative_prompt": {"type": "string"},
         "model": {"type": "string", "description": "기본 kling-v1"},
         "aspect_ratio": {"type": "string", "description": "9:16 | 16:9 | 1:1 …"},
         "n": {"type": "integer"}, "reference_image": {"type": "string", "description": "로컬 경로(프로젝트 상대 가능) 또는 URL"},
         "reference_type": {"type": "string", "description": "subject | face"}, "image_fidelity": {"type": "number"},
         "wait": {"type": "boolean"}, "timeout": {"type": "integer"}, "out": {"type": "string"}}}},
    {"name": "kling_image2video", "description": "정지 이미지 → 영상(image2video) 작업 제출. 비동기: task_id 를 돌려주니 kling_wait(kind=video) 로 받는다.",
     "inputSchema": {"type": "object", "required": ["image", "prompt"], "properties": {
         "image": {"type": "string", "description": "로컬 경로 또는 URL"}, "prompt": {"type": "string"},
         "negative_prompt": {"type": "string"}, "model": {"type": "string", "description": "기본 kling-v1-6"},
         "mode": {"type": "string", "description": "std | pro"}, "duration": {"type": "string", "description": "5 | 10"},
         "cfg_scale": {"type": "number"}}}},
    {"name": "kling_status", "description": "작업 상태 1회 조회.",
     "inputSchema": {"type": "object", "required": ["task_id"], "properties": {
         "task_id": {"type": "string"}, "kind": {"type": "string", "description": "image | video"}}}},
    {"name": "kling_wait", "description": "작업 완료까지 대기(기본 300초) 후 결과 파일 저장. 영상은 kind=video, timeout 을 넉넉히(600~1200).",
     "inputSchema": {"type": "object", "required": ["task_id"], "properties": {
         "task_id": {"type": "string"}, "kind": {"type": "string"}, "timeout": {"type": "integer"}, "out": {"type": "string"}}}},
    {"name": "kling_download", "description": "결과 URL 을 파일로 저장.",
     "inputSchema": {"type": "object", "required": ["url", "out"], "properties": {"url": {"type": "string"}, "out": {"type": "string"}}}},
]
HANDLERS = {"kling_ping": tool_ping, "kling_image": tool_image, "kling_image2video": tool_image2video,
            "kling_status": tool_status, "kling_wait": tool_wait, "kling_download": tool_download}


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
                        "serverInfo": {"name": "coastrun-kling", "version": "1.0"}})
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
