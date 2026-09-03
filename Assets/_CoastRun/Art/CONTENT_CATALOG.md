# Coast Run — Content Catalog (~3h)

## Pace
| Knob | Value | Intent |
|------|-------|--------|
| Tower distance | **54,000 m** | ~90–110 min continuous at mid speed |
| MaxSpeed gate | **Lv 22** | Multi-run grind fills remaining time → **~3h first clear** |
| D-Day timer | **10,800 s (3h)** | Sunset pressure UI |
| Season bands | **13,500 m** each | Spring → Summer → Autumn → Winter along one journey |
| Weather roll | **~900 m** | Clear / Cloudy / Rain / Snow / Mist |

## Systems
- `SeasonWeatherDirector` — season + weather from path distance
- `WeatherFx` — rain / snow / mist particles
- `PropCatalog` + `SegmentDecorator` — reusable seasonal props per tile
- `ObstacleCatalog` — 10 obstacle families, weather-biased

## Prop pool (procedural + optional Prefab `Prop_<Id>_<Season>`)
Benches, lamps, umbrellas, planters, trees (cherry/palm/maple/pine), snowman, pumpkin, leaf/snow piles, buoys, carts, NPCs, awnings, bus stop, fountain, statues, festival lanterns, … (**50 PropId**)

## Obstacles
TrafficCone, Barrier, CrateStack, WetFloorSign, SnowDrift, LeafDrift, PuddleSlow, TouristCluster, DeliveryBox, BikeFallen

## Story landmarks
20 beats from 4% → 95% (season-tagged memories)

## Art hook
Place optional FBX/Prefabs in `Assets/Resources/CoastRun/` named:
- `Prop_Bench_Summer`, `Prop_CherryTree_Spring`, …
- `Obstacle_Barrier`, `Obstacle_SnowDrift`, …
Fallback: procedural primitives (always available).
