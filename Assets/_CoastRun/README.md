# Coast Run (WIP)

Reference-video replication — MCP art pipeline + grind loop.

## Quick start

1. Unity menu: **Tools → Coast Run → Create Run Scene** (once)
2. **Tools → Coast Run → Play** (`Ctrl+Shift+C`)
3. Or: `.\playtest.ps1 -CoastRun`

Game View: **720×1280 portrait**.

## Controls

| Input | Action |
|-------|--------|
| Swipe L/R | Lane |
| Swipe up/down | Jump / Crouch |
| Hold | Tuck |
| Upgrade Shop (bottom-right) | Spend coins |
| U / I / O | Speed / Coin / Magnet (hotkeys) |

## MCP art

Details: [Art/PIPELINE_STATUS.md](Art/PIPELINE_STATUS.md)

PNG textures in `Resources/CoastRun/` load at runtime.  
FBX in `Art/` → Unity import → Prefab into `Resources/CoastRun/`  
(**Tools → Coast Run → List Missing Prefab Resources**)

## Core loop

NearMiss → coins → upgrades → MaxSpeed → 송전탑

## Docs

- `Assets/_CoastRun/COMPLETION.md` — 완성 상태·실행법
- `Assets/_Guide/GameDesign.md`
- `Assets/_Guide/StyleBible.md`
- `Assets/_Guide/AIAssetPromptPack.md`
- `Assets/_Guide/Story_OurPowerTower.md` — 프롤로그·랜드마크·시간대·목적지 UI

