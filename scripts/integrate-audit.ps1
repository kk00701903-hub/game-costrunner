# Coast Run file-level integration audit (no Unity required)
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$res = Join-Path $root "Assets\Resources\CoastRun"
$scene = Join-Path $res "Scene"
$report = Join-Path $root "Temp\visual_compare\integration_report.txt"
New-Item -ItemType Directory -Force -Path (Split-Path $report) | Out-Null

$script:pass = 0
$script:fail = 0
$lines = @("=== Coast Run File Integration Audit ===", (Get-Date -Format u), "")

function Test-Asset($path, $label) {
    $ok = Test-Path $path
    if ($ok) { $script:pass++ } else { $script:fail++ }
    $script:lines += if ($ok) { "[PASS] $label" } else { "[FAIL] $label  ($path)" }
}

$lines += "--- Textures ---"
@(
    "SummerSky_Portrait.png", "Sea_Turquoise_Tile.png", "Road_Promenade.png",
    "Icon_Coin.png", "Icon_Speed.png", "Icon_Magnet.png", "Icon_Tower.png", "Icon_Him.png",
    "Watch_Frame.png", "UI_Panel_Memory.png", "UI_TitleBackground.png"
) | ForEach-Object { Test-Asset (Join-Path $res $_) "Texture $_" }

$lines += "--- Scene stills ---"
1..5 | ForEach-Object { Test-Asset (Join-Path $scene "Scene_Frame_$_.jpg") "Scene_Frame_$_" }

$lines += "--- Prefabs ---"
@(
    "GirlSkater.prefab", "Pole_WireSet.prefab", "Tile_Promenade_30m.prefab",
    "Tile_TownL_ShopA.prefab", "Tile_SeaWallR_30m.prefab", "Obstacle_Cone.prefab"
) | ForEach-Object { Test-Asset (Join-Path $res $_) "Prefab $_" }

$tower = Join-Path $res "TransmissionTower.prefab"
if (Test-Path $tower) {
    $script:pass++; $lines += "[PASS] Prefab TransmissionTower.prefab"
} else {
    $lines += "[WARN] Prefab TransmissionTower.prefab (run Tools > Coast Run > Full Setup in Unity)"
}

$lines += "--- Scenes ---"
Test-Asset (Join-Path $root "Assets\_CoastRun\Scenes\MainMenu.unity") "MainMenu.unity"
Test-Asset (Join-Path $root "Assets\_CoastRun\Scenes\Run.unity") "Run.unity"

$lines += ""
$lines += "Summary: $($script:pass) pass, $($script:fail) fail"
$lines | Set-Content $report -Encoding UTF8
$lines
