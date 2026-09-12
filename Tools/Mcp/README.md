# Coast Run — 로컬 MCP 서버 모음

Claude 데스크톱이 이 프로젝트를 다룰 때 쓰는 stdio MCP 서버들.
설정 원본은 이 폴더의 `claude_desktop_config.json` 이고, 실제로 읽히는 파일은
`%APPDATA%\Claude\claude_desktop_config.json` 이다.

## 적용 절차

```powershell
copy /Y "C:\dev\game\Tools\Mcp\claude_desktop_config.json" "%APPDATA%\Claude\claude_desktop_config.json"
pip install requests PyJWT
```
그다음 Claude 데스크톱을 완전히 종료 후 재시작. (트레이 아이콘까지 종료할 것)

Cursor 를 쓸 때는 `.cursor/mcp.json` 이 같은 목록을 갖고 있으니 별도 복사 불필요.

## 등록된 서버

| 이름 | 실행 | 비고 |
|---|---|---|
| `unity` | `Tools/Mcp/unity_mcp_server.py` | 에디터 실행·컴파일·로그·스크린샷 |
| `blender` | `uvx blender-mcp` | Blender 애드온이 9876 포트에서 대기해야 함 |
| `photoshop` | `@alisaitteke/photoshop-mcp` | Photoshop 2026 실행 필요 |
| `illustrator` | `illustrator-mcp-server` | Illustrator 2026 실행 필요 |
| `kling` | `Tools/Mcp/kling_mcp_server.py` | `.env` 의 `KLING_*` |
| `mixamo` | `Tools/Mcp/mixamo_mcp_server.py` | `.env` 의 `MIXAMO_BEARER` — **신규** |
| `pexels` | `Tools/Mcp/pexels_mcp_server.py` | `.env` 의 `PEXELS_API_KEY` — **신규** |

## mixamo

Mixamo 는 공개 API 키가 없어서 로그인 세션 토큰을 쓴다.

1. https://www.mixamo.com 로그인
2. F12 → Application → Local Storage → `https://www.mixamo.com` → `access_token` 값 복사
3. `.env` 의 `MIXAMO_BEARER=` 뒤에 붙여넣기
4. `mixamo_ping` 으로 확인. 401 이면 토큰 만료 → 다시 복사 (보통 몇 시간~하루)

도구: `mixamo_ping` / `mixamo_characters` / `mixamo_search` / `mixamo_details` / `mixamo_download`

`mixamo_download` 는 기본값으로 skin 없이(애니메이션만) 30fps FBX 를
`Assets/Art/Animations/Mixamo/` 에 저장한다. Unity 에서 Rig → Humanoid 확인할 것.

> 비공개 웹 API 라 엔드포인트가 바뀔 수 있다. 처음 쓸 때 `mixamo_ping` →
> `mixamo_characters` → `mixamo_search` 순으로 한 번씩 확인하는 게 안전하다.

## pexels

`.env` 의 `PEXELS_API_KEY` 를 그대로 쓴다. 별도 설정 없음.
도구: `pexels_ping` / `pexels_search_photos` / `pexels_search_videos` /
`pexels_curated` / `pexels_download` (기본 저장 위치 `Tools/Art/_pexels/`).

## 문제가 생기면

- 서버가 안 뜬다 → Claude 데스크톱 로그: `%APPDATA%\Claude\logs\mcp-server-<이름>.log`
- `ModuleNotFoundError: requests` → 설정에 적힌 그 python.exe 로 `pip install requests`
- blender 도구가 실패 → Blender 가 켜져 있고 애드온 서버가 start 상태인지 확인
