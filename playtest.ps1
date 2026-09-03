# 『우리의 송전탑』 (Coast Run) quick test launcher (Windows)
# Usage:
#   .\playtest.ps1              -> open Unity on 02_Run.unity
#   .\playtest.ps1 -Play        -> open scene + enter Play
#   .\playtest.ps1 -Title       -> open 01_Title.unity
#   .\playtest.ps1 -CompileOnly -> Roslyn compile check only

param(
    [switch]$Play,
    [switch]$Title,
    [switch]$CompileOnly
)

$ErrorActionPreference = "Stop"
$Project = Split-Path -Parent $MyInvocation.MyCommand.Path

function Get-ProjectUnityVersion {
    $versionFile = Join-Path $Project "ProjectSettings\ProjectVersion.txt"
    if (-not (Test-Path $versionFile)) { return $null }
    foreach ($line in Get-Content $versionFile) {
        if ($line -match '^m_EditorVersion:\s*(.+)$') { return $Matches[1].Trim() }
    }
    return $null
}

function Find-UnityExe {
    $wanted = Get-ProjectUnityVersion
    if ($wanted) {
        $exact = "C:\Program Files\Unity\Hub\Editor\$wanted\Editor\Unity.exe"
        if (Test-Path $exact) { return $exact }
    }
    $hub = "C:\Program Files\Unity\Hub\Editor"
    if (Test-Path $hub) {
        # Prefer Unity 6 (6000.x), then latest installed.
        $dirs = Get-ChildItem $hub -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending
        $u6 = $dirs | Where-Object { $_.Name -match '^6000\.' } | Select-Object -First 1
        if ($u6) {
            $exe = Join-Path $u6.FullName "Editor\Unity.exe"
            if (Test-Path $exe) { return $exe }
        }
        foreach ($d in $dirs) {
            $exe = Join-Path $d.FullName "Editor\Unity.exe"
            if (Test-Path $exe) { return $exe }
        }
    }
    return $null
}

Write-Host "Coast Run playtest - project: $Project"

$compile = Join-Path $Project "Temp\compilecheck.ps1"
if (Test-Path $compile) {
    Write-Host "Compiling..."
    & powershell -NoProfile -ExecutionPolicy Bypass -File $compile
    if ($LASTEXITCODE -ne 0 -and $CompileOnly) { exit $LASTEXITCODE }
}

if ($CompileOnly) { exit 0 }

$unity = Find-UnityExe
if (-not $unity) {
    Write-Host "Unity.exe not found. Open Unity Hub manually and load: $Project"
    Write-Host "Then: Tools > Coast Run > PLAY 해안 주행  (Ctrl+Shift+C)"
    exit 1
}

Write-Host "Unity: $unity"

$log = Join-Path $Project "Temp\coastrun-play.log"
New-Item -ItemType Directory -Force -Path (Join-Path $Project "Temp") | Out-Null
$args = @("-projectPath", $Project, "-logFile", $log)
if ($Play) {
    $args += "-CoastPlay"
}
elseif ($Title) {
    $args += "-executeMethod"
    $args += "CoastRunMenu.OpenMainMenuScene"
}
else {
    $args += "-executeMethod"
    $args += "CoastRunMenu.OpenRunScene"
}

Start-Process -FilePath $unity -ArgumentList $args | Out-Null
Write-Host "Coast Run: Unity launched. Game tab에서 Play 확인 (-Play 로 자동 시작)."
exit 0
