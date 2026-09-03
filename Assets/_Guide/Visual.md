# 『347』 Visual Guide

Mobile portrait target: **Galaxy S26 — 1080×2340, 60 fps**.

## Render stack

| Asset | Path |
|-------|------|
| URP Asset | `Assets/_Project/Settings/UniversalRP.asset` |
| Forward Renderer | Package default (referenced by URP asset) |
| Run Volume | `Assets/Resources/347/RunVolumeProfile.asset` |
| Material Library | `Assets/Resources/347/MaterialLibrary.asset` |

First-time setup (or after clone):

```
Tools > 347 > Setup Visual Pipeline (URP + Materials + Volume)
Tools > 347 > Import Free CC0 Assets
```

`Import Free CC0 Assets` pulls Kenney CC0 packs (city roads + animated character) into `Resources/` with 347 file names. See [`FreeAssetMap.json`](../_Guide/FreeAssetMap.json).

Play uses `VisualBootstrap` to spawn a global volume if the baked profile is missing.

## Quality tiers

| Tier | Use | Post | Shadow distance |
|------|-----|------|-----------------|
| Low | older phones | off | 28 m |
| S26 | default | on | 50 m |
| High | editor capture | on | 70 m |

Set via `VisualQuality.Apply(VisualTier.S26)` or PlayerPrefs key `r347_visual_tier`.

## Material naming

| Material | Role |
|----------|------|
| `M_Asphalt` | Road tiles, track GLB fallback |
| `M_Concrete` | Walls, kerbs, debris |
| `M_Metal` | Guardrails, vehicles |
| `M_EmissiveSign` | Shopfront / depot signage |
| `M_Water` | Flooded zone plane |
| `M_CharacterSkin` | Doha placeholder skin |

Runtime code prefers baked SO materials over `Shader.Find`.

## Character

- **3D mode (default):** `GameConfig.use3DCharacter = true` → `PlayerCharacterView`
- Import Store humanoid to `Resources/Character/Doha/DohaModel.prefab` (Humanoid rig + Animator)
- **2D fallback:** set `use3DCharacter = false` → `PlayerSpriteView` + `Girl_*.png`

Animator parameters (see `CharacterAnimParams.cs`): Speed, Grounded, Slide, Jump, Lean, Hurt, Counter, Grind, WallRun, Dead.

## Poly budget (mobile)

| Category | Target tris |
|----------|-------------|
| Character body | 5k–15k |
| Track tile | ≤8k |
| Prop / hazard | ≤4k |
| King / drone silhouette | ≤6k |

Validate after import: **Tools > 347 > Validate Art**

## LOD

`MeshLodGroup` on roadside props: full detail &lt;45 m, no shadows &lt;90 m, culled ≥90 m.

## Zone read (geometry)

Zone tiles are picked by name token in `ZoneDirector.AllowsTile`:

1. Arcade — `Track_Arcade`, shopfront walls
2. Overpass — `Track_Overpass`, guardrail
3. Flooded — `Track_Flooded`, water plane
4. Tower — straight + bright ambient
5. Depot — `Track_Depot`, scanner emissive

TestCatalog builds placeholder geometry when GLB/Store packs are absent.

## Import preset (GLB/FBX)

- Scale: 1 unit = 1 metre
- Pivot: road tiles at **rear centre (min Z)**
- Textures: 1K props, 2K character max
- Materials: URP Lit; run Setup Visual Pipeline to rebake if pink

## Store pack checklist

See plan + `AssetRequest.md`. After import:

1. Tools > 347 > Validate Art
2. Galaxy S26 Game View → Play Runner
3. Confirm no pink shaders, 60 fps in Profiler (≤16 ms)
