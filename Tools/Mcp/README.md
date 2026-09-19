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
| `figma` | `Tools/Mcp/figma_mcp_server.py` | Figma 개발 플러그인 `Tools/Figma/plugin` 과 127.0.0.1:47010 롱폴링 — 상세 `Tools/Figma/README.md` — **신규 2026-09-19** |
| `android` | `Tools/Mcp/android_mcp_server.py` | `.env` 의 `ANDROID_*` — adb/emulator 래퍼 (Cursor 의 터미널 권한을 대신함) — **신규 2026-09-14** |

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

## android

Cursor 에서는 에이전트가 PowerShell 로 `adb`/`emulator.exe` 를 직접 돌렸다(터미널 auto-run 권한).
Claude 데스크톱은 그 권한이 없으므로 `android_mcp_server.py` 가 같은 일을 MCP 도구로 제공한다.
경로·기본값은 `.env` 의 `ANDROID_SDK_ROOT / ANDROID_ADB / ANDROID_EMULATOR / ANDROID_AVD / ANDROID_AVD_ALT / ANDROID_PKG / ANDROID_ACTIVITY`.

도구: `android_ping` / `android_devices` / `android_avds` / `android_start_avd` / `android_kill_avd` /
`android_install` / `android_launch` / `android_stop_app` / `android_screencap` / `android_logcat` /
`android_input` / `android_shell` / `android_props`

전형적 흐름(실기기 USB 연결 후):
`android_ping` → `android_install`(Builds/CoastRun.apk) → `android_launch`(boot=run) → `android_screencap`(name=device_run) → `android_logcat`

> 이 PC 에선 에뮬레이터로 Unity 6 APK 화면을 볼 수 없다(ARM APK ↔ x86_64 AVD SIGILL, ARM64 AVD 는 QEMU2 부팅 불가).
> 상세: `EMU_VISUAL.md`. 실기기 또는 에디터 Play(`unity_shot`) 로 확인한다.

PC 에서 직접 점검: `python Tools\Mcp\android_mcp_server.py --selftest`

Claude 데스크톱 등록: 앱 설정 → 개발자 → 구성 편집 에 `claude_desktop_mcp_android.json` 의 `android` 항목을 `mcpServers` 안에 붙여 넣고 앱 완전 재시작.

## 문제가 생기면

- 서버가 안 뜬다 → Claude 데스크톱 로그: `%APPDATA%\Claude\logs\mcp-server-<이름>.log`
- `ModuleNotFoundError: requests` → 설정에 적힌 그 python.exe 로 `pip install requests`
- blender 도구가 실패 → Blender 가 켜져 있고 애드온 서버가 start 상태인지 확인
