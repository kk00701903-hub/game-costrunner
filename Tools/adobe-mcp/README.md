# Adobe MCP 연결 (Coast Run)

## 설치 확인됨

| 앱 | 경로 |
|----|------|
| Photoshop 2026 | `C:\Program Files\Adobe\Adobe Photoshop 2026\Photoshop.exe` |
| Illustrator 2026 | `C:\Program Files\Adobe\Adobe Illustrator 2026\Support Files\Contents\Windows\Illustrator.exe` |

MCP 설정: `.cursor/mcp.json` (`photoshop`, `illustrator`, `blender`)

## 사용 전

1. **Photoshop** 또는 **Illustrator** 실행 (작업할 앱)
2. **Cursor 재시작**
3. **Settings → MCP**에서 `photoshop` / `illustrator` Connected 확인

## 예시

**Photoshop**
- "1024×1024 청록 바다 타일 PNG 만들어 Assets/_CoastRun/Art/Environment/에 저장"
- "style_frame_1 참고 여름 하늘 배경"

**Illustrator**
- "720×1280 UI 아이콘 세트 (코인, 점프, 레인)"
- "Coast Run 로고 벡터"

## 문제 해결

- **Ping failed** → 해당 Adobe 앱을 먼저 실행
- **도구 안 보임** → Cursor 재시작
- Illustrator 버전 지정 → 채팅에서 "Use Illustrator 2026"
