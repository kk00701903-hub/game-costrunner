# Coast Run — Completion Status

## Systems (done)
- Core run loop, NearMiss, upgrades, 송전탑 destination
- Story: prologue, 20 landmarks, D-Day 3h, cheer lines
- Season/Weather: 4 seasons × 5 weather states + particles
- Content: 50 props, 10 obstacles, 27 Resources prefabs
- Audio: procedural ambient/wind/wheels
- Rendering: CoastToon shader, procedural sky, wave sea + foam
- Config: `Resources/CoastRun/Config/*.asset`

## Play
```
.\playtest.ps1 -CoastRun
```
Or **Tools → Coast Run → Full Setup (Config + Art Import)** then Play.

## Visual compare
```
Unity -executeMethod CoastOfflineCapture.Run
```
Output: `Temp/visual_compare/game_capture.png`

## Remaining polish (post-MVP)
- High-res MCP art pass (Photoshop sky, rigged character animations)
- Prologue VideoClip when source footage is ready
- Mobile build + long-session profiling
- Balance tuning from real 3h playtest
