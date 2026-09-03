# MCP Pipeline Status

Last update: 2026-09-03 — Story systems asset pack (cutscene / memory / ending).

## Done

| Phase | Deliverable |
|-------|-------------|
| 0–4 | Prior MCP pipeline |
| 5 | **~3h pace** + Season/Weather + Prop/Obstacle pools |
| 5 | Blender Seasonal pack (21 FBX) → Resources Prefabs |
| 6 | **HUD icons** (Illustrator): Icon_Coin, Icon_Speed, Icon_Magnet, Icon_Him, Icon_Tower |
| 6 | **Photoshop**: Sea_Turquoise_Tile vibrance refresh → Art + Resources |
| 6 | **Blender**: Obstacle_Cone + Obstacle_Barrier re-exported |
| 7 | **Story config**: `StageTable.rewardFragmentId`, `CutsceneTable` 13 entries, `story_data.json` |
| 7 | **Folders**: `Resources/CoastRun/Memory/`, `Audio/`, `Art/Memory/`, `Art/Ending/` |
| 7 | **Prompt pack**: `Assets/_Guide/AIAssetPromptPack.md` — P2 memory / cine / ending / BGM keys |

See [CONTENT_CATALOG.md](CONTENT_CATALOG.md) and [AIAssetPromptPack.md](../../_Guide/AIAssetPromptPack.md).

## Runtime vs Editor

- **PNG** under `Assets/Resources/CoastRun/` load immediately at Play.
- **Memory stills** → `Resources/CoastRun/Memory/Mem_R##_A.png` (Sprite, no mips).
- **BGM** → `Resources/CoastRun/Audio/<exactKey>.ogg`.
- **FBX → Prefab**: **Tools → Coast Run → Auto Import Art → Resources**
- **Story SO rebuild**: **Coast Run → Rebuild Story Config Assets**

## Next drops (priority)

1. Memory stills R01–R12 (Warm) + R13–R14 (Cold) — R15 UI-only  
2. `BGM_Cine_CH4_Close` + `BGM_End_Letter` + `BGM_Sting_Radio`  
3. Prologue P3 sticker board close-up  
4. Ending ambiguous ground still  

## Play

```
.\playtest.ps1 -CoastRun
```

Boot from **00_Boot**. NearMiss → coins → stages → memory overlay → ending (single path).
