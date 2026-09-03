# 『347』 quick test launcher (Windows)
# Usage:
#   .\playtest.ps1              -> open Unity on Run.unity
#   .\playtest.ps1 -Play        -> open scene + enter Play (Runner)
#   .\playtest.ps1 -Arena       -> open scene + enter Play (King Arena)
#   .\playtest.ps1 -CompileOnly -> Roslyn compile check only
#   .\playtest.ps1 -CoastRun     -> open Coast Run scene + enter Play

param(
    [switch]$Play,
    [switch]$Arena,
    [switch]$CoastRun,
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

Write-Host "347 playtest — project: $Project"

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
    Write-Host "Then: Tools > 347 > Play Runner  (Ctrl+Shift+R)"
    exit 1
}

Write-Host "Unity: $unity"

$log = Join-Path $Project "Temp\347-play.log"
New-Item -ItemType Directory -Force -Path (Join-Path $Project "Temp") | Out-Null
$args = @("-projectPath", $Project, "-logFile", $log)
if ($CoastRun) {
    $args += "-CoastPlay"
}
elseif ($Play) {
    $args += "-347Play"
}
elseif ($Arena) {
    $args += "-347Arena"
}
else {
    $args += "-executeMethod"
    $args += "PlayTestTools.OpenRunScene"
}

Start-Process -FilePath $unity -ArgumentList $args | Out-Null
Write-Host "347: Unity launched. Game tab에서 Play 확인 (Runner auto-starts with -347Play)."
exit 0
