# Blender MCP 연결 가이드 (Coast Run)

Cursor ↔ Blender 연결용. 서버 설정은 `.cursor/mcp.json`에 이미 들어 있음.

## 확인된 환경

- Blender **5.2.1 LTS** — `C:\Program Files\Blender Foundation\Blender 5.2\`
- `uv` / `uvx` — `C:\Users\ares2\.local\bin\`
- 애드온 설치됨 —  
  `%APPDATA%\Blender Foundation\Blender\5.2\scripts\addons\blender_mcp.py`

## 1회 활성화 (Blender에서)

1. **Blender 실행** (5.2)
2. **Edit → Preferences → Add-ons**
3. 검색: `MCP` 또는 `Blender MCP`
4. **Interface: MCP for Blender** 체크(활성화)
5. Preferences 닫기  
   (목록에 없으면 재시작 후 다시 확인)

## 매 세션 (연결)

1. Blender 3D 뷰포트에서 **N** 키 → 사이드 패널
2. **BlenderMCP** / **MCP** 탭
3. **Connect to MCP Server** / **Start MCP Server** 클릭
4. **Cursor 재시작** → **Settings → MCP**에서 `blender` Connected 확인

## 사용 예

- “빈 씬에 스케이트보드 만들기 (오렌지 휠)”
- “소녀 실루엣 로우폴리 캐릭터 만들고 FBX로 Assets/_CoastRun/Art/Character 내보내기”

## 주의

- Blender를 **켠 상태**에서만 MCP가 동작함
- Photoshop MCP와 동시에 켜도 됨
- `uv`는 `C:\Users\ares2\.local\bin`에 설치됨 (PATH에 없으면 Cursor 재시작)
