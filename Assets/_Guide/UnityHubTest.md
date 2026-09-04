# Coast Run — Unity Hub / Editor testing

## Open from Hub

1. Unity Hub → **Projects**
2. **Coast Run** (`C:\dev\game`) is listed (favorite, Unity **6000.5.10f1**)
3. Click to open

If missing:

```powershell
.\scripts\open-unity-hub-project.ps1 -HubOnly
```

## First-time prepare (in Editor)

Menu: **Coast Run → Prepare Project For Hub Test**

- Builds 5-scene flow + Build Settings
- Rebuilds Stage/Cutscene tables

## Play / source test

| Action | Menu |
|--------|------|
| Full flow (Boot→Title→…) | **Coast Run → Play From Boot** (`Ctrl+Alt+B`) |
| Gameplay only | **Coast Run → Open Run Scene** then Play |

Game View: **720 × 1280** portrait.

## CLI

```powershell
# Register in Hub + launch Editor
.\scripts\open-unity-hub-project.ps1

# Launch and enter Play from Boot
.\scripts\open-unity-hub-project.ps1 -Play

# Legacy helper (also finds Unity 6)
.\playtest.ps1 -CoastRun
```
