# Coast Run — AI Asset Prompt Pack

> **Mode:** 100% reference video replication (`Assets/_Guide/Reference/style_reference.mp4`)  
> Plus story systems: cutscenes · memory fragments · ending (`엔딩_개정안_v2.md`).  
> Replace procedural placeholders in `Assets/_CoastRun/` as assets arrive.

---

## Global style suffix (append to every prompt)

```
Studio Ghibli meets Makoto Shinkai, cel-shaded 3D game art, mobile portrait 720x1280,
sharp cool blue shadows, high saturation summer sky and sea, warm cream coastal town,
clean ink outline on character only, no photoreal, no PBR grime, game-ready low poly
```

### Memory tone suffixes

```
MemWarm: memory flashback, saturation +15% vs present, soft bloom, slight film grain, warm nostalgic tint, summer light
MemCold: memory flashback, desaturated, cool distant tint, hazy and far, slight film grain, muted (CH5 R13–R15 ONLY)
```

### Hard rules (story)

- **Doyun appears ONLY in memory / letter VO** — never present-day silhouette, back, or hands.
- **Ending is one path** — no true ending branch, no explanatory captions.
- **R15** — hand + phone only; no place, no clothing readable, no Lua on screen.
- **Ending ground closeup** — neither freshly dug nor packed earth.

---

## P0 — Character (girl + board + backpack)

**Filename target:** `Assets/_CoastRun/Art/Character/GirlSkater.fbx`

```
Teenage girl skater back view, short brown hair blowing in wind, mustard yellow short-sleeve shirt,
blue denim shorts, large olive/khaki travel backpack, teal sneakers, classic skateboard with bright orange wheels,
cruising stance slightly leaning forward, cel-shaded game character, T-pose alternative: skating idle loop,
[GLOBAL STYLE SUFFIX]
```

**Animations needed:** Idle cruise, Jump, Crouch/Tuck, SoftHit stumble, Hair/Backpack wind secondary.

---

## P0 — Summer sky + clouds

**Filename target:** `Assets/_CoastRun/Art/Sky/SummerSky_Cubemap.exr`

```
Painted summer cumulus clouds, massive fluffy white clouds with soft blue shadows,
deep azure sky gradient, Japanese anime background art, seamless skybox cubemap,
horizon pale blue haze over distant coastal hills, [GLOBAL STYLE SUFFIX]
```

---

## P0 — Turquoise sea + foam

**Filename target:** `Assets/_CoastRun/Art/Environment/Sea_Normal.png` + wave shader

```
Turquoise coastal ocean surface top-down tileable normal map, white foam crest strips,
cel-anime sea, bright teal #1F9EC7, gentle waves, game texture 1024 seamless,
[GLOBAL STYLE SUFFIX]
```

---

## P0 — Promenade road tile (30 m)

**Filename target:** `Assets/_CoastRun/Art/Tiles/Tile_Promenade_30m.fbx`

```
Wide coastal promenade tile 30 meters, warm beige stone pavement, subtle cracks,
optional yellow center line dashes, low curb on both sides, straight segment,
modular game environment, top-down friendly UVs, [GLOBAL STYLE SUFFIX]
```

---

## P0 — Left town modules

**Filename target:** `Assets/_CoastRun/Art/Tiles/Tile_TownL_Shop*.fbx`

```
Japanese coastal town shop fronts, warm cream stucco, blue balcony rails, red awning,
2-3 story low buildings, utility pole slot on sidewalk, cel-shaded game modular pieces,
[GLOBAL STYLE SUFFIX]
```

---

## P0 — Utility pole + wires

**Filename target:** `Assets/_CoastRun/Art/Props/Pole_WireSet.fbx`

```
Concrete utility pole with cross arm, thin black power lines, Japanese seaside street,
simple low poly game prop, [GLOBAL STYLE SUFFIX]
```

---

## P0 — Sea wall + sand (right)

**Filename target:** `Assets/_CoastRun/Art/Tiles/Tile_SeaWallR_30m.fbx`

```
Low grey sea wall, sandy beach strip, rocks at base, modular 30m coastal segment,
view from promenade looking right toward ocean, [GLOBAL STYLE SUFFIX]
```

---

## P1 — Walking NPCs (2–3 variants)

```
Casual summer pedestrians, simple low poly, back/side views, muted clothing,
background NPC for mobile runner, no outline, [GLOBAL STYLE SUFFIX]
```

---

## P1 — Doyun master (memory-only)

**Filename:** `Assets/_CoastRun/Art/Character/DOYUN_MASTER.png`  
★ Never use in present-day Run / Ending staging.

```
A teenage boy about eighteen seen from the side, short black hair,
plain grey t-shirt, work trousers, slightly slouched posture,
holding a small tool and looking down at it with concentration,
summer light, simple background, character reference sheet,
[GLOBAL STYLE SUFFIX]
```

---

## P2 — Memory stills R01–R15

**Runtime path:** `Assets/Resources/CoastRun/Memory/`  
**Keys match** `story_data.json` → `stillKeys` (`Mem_R01_A`, `Mem_R01_B`, …)  
**Import:** Sprite, Clamp, **Mip Maps OFF**, 1080×1920 portrait.

| ID | Tone | Still keys | Notes |
|----|------|------------|-------|
| R01 | Warm | Mem_R01_A/B | Broken truck in boy's hands |
| R02 | Warm | Mem_R02_A/B | Digging by tower foundation + candy tin |
| R03 | Warm | Mem_R03_A | Boy + glowing radio at 2am |
| R04 | Warm | Mem_R04_A/B | Breakwater, doing nothing |
| R05 | Warm | Mem_R05_A | Boy swallows words |
| R06 | Warm | Mem_R06_A/B | Polar-bear ice cream sticker under deck |
| R07 | Warm | Mem_R07_A/B | Abandoned factory + old guard talking |
| R08 | Warm | Mem_R08_A | Marker name on bike rear wheel |
| R09 | Warm | Mem_R09_A | Over-shoulder text send (no readable glyphs) |
| R10 | Warm | Mem_R10_A/B | Writing at desk + radio |
| R11 | Warm | Mem_R11_A | Bike on highway dusk — **no accident** |
| R12 | Warm | Mem_R12_A/B | Girl turns back from tower road |
| R13 | **Cold** | Mem_R13_A | Brief phone call, distant |
| R14 | **Cold** | Mem_R14_A | Boy far away toward tower |
| R15 | **Cold** | *(none — UI only)* | Hand + phone; no place; no Lua drawn |

### Sample prompts

**R01**
```
Two children about twelve crouched on a sunlit street,
boy turning a broken skateboard truck in his hands, girl watching,
[MemWarm], [GLOBAL STYLE SUFFIX]
```

**R11** (must stay ordinary)
```
Boy on a bicycle crossing a wide empty highway at dusk, from behind at a distance,
girl watching from roadside, long shadows, calm ordinary moment,
no accident, no danger, [MemWarm], [GLOBAL STYLE SUFFIX]
```

**R15** (reject if place/clothing readable)
```
Extreme close-up of a girl's hand holding a phone to her ear,
only hand and phone lit, background completely out-of-focus darkness,
no room, no furniture, no window, no outdoor scenery, no other people,
no readable clothing, [MemCold], [GLOBAL STYLE SUFFIX]
```

**Gallery thumbs:** `Memory_Thumb_R01.png` … `R15` (R15 thumb = phone icon only).

---

## P2 — Cutscene stills / timelines

**Config:** `CutsceneTable` (`Prologue`, `CH2_Open`…`CH5_Open`, `CH1_Close`…`CH4_Close`)  
**BGM keys (exact):** `BGM_Cine_Prologue`, `BGM_Cine_CH2_Open` … `BGM_Cine_CH4_Close`  
**CH4_Close:** intentional silence 0:50–0:58 — do not fill ambient.

| ID | Length | Must-show beats |
|----|--------|-----------------|
| Prologue P1 | ~40s | Phone SMS + send-time field visible |
| Prologue P2 | ~50s | Station board 「정비 중 · 운행 중단」 |
| Prologue P3 | ~40s | Board underside stickers: polar bear ice cream / torn band logo / pen Y+D |
| Prologue P4 | ~20s | **Not a rendered clip** — camera handoff to Run |
| CH3_Close | 90s | WhiteFlash entry · festival poster date déjà vu |
| CH4_Close | 90s | WhiteFlash · send time reveal · silence window |

Timeline assets → assign on `CutsceneDef.timeline` when ready.

---

## P2 — Ending (`04_Ending`)

**BGM keys:** `BGM_End_Arrival`, `BGM_End_Letter`, `BGM_End_Descent` (loop), `BGM_Sting_Radio`

| Shot | Asset hint | Constraint |
|------|------------|------------|
| Tower + silver grass + red beacon | reuse TransmissionTower + grass set | Nobody waiting |
| Ground 2s closeup | `End_Ground_Ambiguous.png` | Not dug, not packed |
| Rusty snack tin + truck + folded paper | props | Piano one-note SFX on reveal |
| Letter hands + paper | UI/still | No face |
| Phone 3s | UI | Number visible, **name clipped off**; no call press; cam+harmony frozen |
| Descent village lights | point lights OK | No UI, no fail |
| Stinger | **black only** | DJ VO+subs; never identify sender |

---

## P2 — Audio file drop

Place under `Assets/Resources/CoastRun/Audio/` (filename = key, `.ogg` preferred):

```
BGM_Cine_Prologue, BGM_Cine_CH2_Open, BGM_Cine_CH3_Open, BGM_Cine_CH4_Open, BGM_Cine_CH5_Open,
BGM_Cine_CH1_Close, BGM_Cine_CH2_Close, BGM_Cine_CH3_Close, BGM_Cine_CH4_Close,
BGM_Memory_Warm, BGM_Memory_Mid, BGM_Memory_Cold,
BGM_End_Arrival, BGM_End_Letter, BGM_End_Descent, BGM_Sting_Radio
```

---

## Import checklist (Cursor task)

When each asset lands:

1. Place under `Assets/_CoastRun/Art/` (authoring) and/or `Assets/Resources/CoastRun/` (runtime)
2. Memory stills → `Resources/CoastRun/Memory/` as Sprite, no mips
3. Assign URP Lit / Toon using `CoastPalette`
4. Swap procedural meshes in `PromenadeSegmentBuilder` / `CoastPlayerVisual`
5. Wire Timeline assets into **Coast Run → Rebuild Cutscene Table Defaults** then assign `timeline` fields
6. Screenshot Play Mode vs `style_frame_*.jpg` — StyleBible 5 checks must pass

**Unity menu batch:** `Coast Run → Rebuild Story Config Assets`  
(StageTable + CutsceneTable + Resources mirrors)

---

## StyleBible verification (must pass)

1. Sky occupies ≥⅓ of screen  
2. Town left, sea right  
3. Girl in bottom ⅓  
4. Sharp cool shadows  
5. Utility poles + wires visible
