# Frequency — Figma 이관 (142차)

Claude 데스크톱 ⇄ Figma 데스크톱을 로컬 MCP(`Tools/Mcp/figma_mcp_server.py`) + 개발 플러그인(`plugin/`)으로 잇는다.
Figma 공식 MCP(Dev Mode)는 읽기 전용이라 에셋을 **넣는** 데 쓸 수 없어서 직접 만든 브리지다.

## 1회 설정 (사람 손이 필요한 두 가지)

1. **Claude 데스크톱 재시작** — `claude_desktop_config.json` 에 `figma` 서버가 이미 등록돼 있다(트레이까지 완전 종료 후 실행).
   재시작 후 Claude 에 `figma_ping` 도구가 보이면 됨.
2. **Figma 데스크톱에서 플러그인 1회 등록** — 아무 파일이나 연 뒤
   메뉴 `Plugins → Development → Import plugin from manifest…` → `C:\dev\game\Tools\Figma\plugin\manifest.json`
   (한 번만. 이후엔 `Plugins → Development → Frequency Bridge` 로 실행)

## 이관 절차

1. Figma 에서 프로젝트 **Frequency** 를 만들고(팀 홈 → `+ Project`), 그 안에 파일 **Frequency — Game Assets** 를 새로 연다.
   (프로젝트/파일 생성은 플러그인 API 로 불가 — 이 두 클릭만 사람이)
2. 그 파일에서 `Plugins → Development → Frequency Bridge` 실행 → 창에 「연결됨」이 뜨면
3. Claude 에게 「피그마로 에셋 이관」이라고 하면 `figma_import_assets` 가 `frequency_assets.json` 대로
   페이지 `Frequency / UI · 아이콘` … 8개를 만들고 그리드로 넣는다(약 900장, 10~20분).

## 파일

| 경로 | 역할 |
|---|---|
| `Tools/Mcp/figma_mcp_server.py` | MCP(stdio) + 127.0.0.1:47010 HTTP(롱폴링·파일 서빙·내보내기 저장) |
| `Tools/Figma/plugin/manifest.json, code.js, ui.html` | Figma 개발 플러그인 — 서버가 준 JS 를 `figma` API 로 실행 |
| `Tools/Figma/build_asset_manifest.py` | Assets/ 이미지를 8개 카테고리로 분류 → `frequency_assets.json` + 축소본 `_import/` |
| `Tools/Figma/frequency_assets.json` | 카테고리·원본 경로·크기·축소본 경로 |
| `Tools/Figma/_import/<카테고리>/` | Figma 에 넣는 축소본(긴 변 512~1024px; 원본은 Assets/ 그대로) |
| `Tools/Figma/exports/` | `figma_export_node` 결과 |

## 카테고리 (2026-09-19)

UI · 아이콘 178 / 캐릭터 79 / 카드 · 스케줄 68 / 배경 · 환경 71 / 장애물 · 소품 · FX 76 / 텍스처 54 / 컷씬 · 챕터 351(현행본, 레거시·폴백 제외) / 브랜드 · 앨범 · 컨셉 27 — 총 904

## 도구

`figma_ping` · `figma_run(code)` (플러그인 안에서 JS 실행; `figma`, `args`, `H` 헬퍼) · `figma_pages` · `figma_selection` ·
`figma_place_image(path)` · `figma_import_assets(categories?)` · `figma_export_node(node_id)`

## 참고: Figma 공식 Dev Mode MCP(읽기)

Figma 데스크톱 `Preferences → Enable Dev Mode MCP Server` 를 켜면 `http://127.0.0.1:3845/mcp` 가 뜬다.
Claude 데스크톱에 붙이려면 `"figma_devmode": {"command": "C:\\Program Files\\nodejs\\npx.cmd", "args": ["-y", "mcp-remote", "http://127.0.0.1:3845/mcp"]}`
(디자인 읽기·스크린샷·변수 조회용. 현재는 꺼져 있어 등록하지 않음.)
