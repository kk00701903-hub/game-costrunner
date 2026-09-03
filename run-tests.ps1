# 『347』 EditMode unit tests (Windows)
# Usage: .\run-tests.ps1

$ErrorActionPreference = "Stop"
$Project = Split-Path -Parent $MyInvocation.MyCommand.Path
$unityWanted = (Select-String -Path (Join-Path $Project "ProjectSettings\ProjectVersion.txt") -Pattern '^m_EditorVersion:\s*(.+)$').Matches[0].Groups[1].Value.Trim()
$unity = "C:\Program Files\Unity\Hub\Editor\$unityWanted\Editor\Unity.exe"
if (-not (Test-Path $unity)) {
    Write-Error "Unity.exe not found for $unityWanted"
}

$results = Join-Path $Project "Temp\TestResults-EditMode.xml"
$log = Join-Path $Project "Temp\unity-tests.log"
New-Item -ItemType Directory -Force -Path (Join-Path $Project "Temp") | Out-Null

Write-Host "347 EditMode tests — $unityWanted"
Get-Process Unity -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

$p = Start-Process -FilePath $unity -ArgumentList @(
    "-batchmode", "-nographics", "-projectPath", $Project,
    "-runTests", "-testPlatform", "EditMode",
    "-testResults", $results,
    "-logFile", $log
) -PassThru -Wait

$alt = Join-Path $env:USERPROFILE "AppData\LocalLow\DefaultCompany\game\TestResults.xml"
if (-not (Test-Path $results) -and (Test-Path $alt)) {
    Copy-Item $alt $results -Force
}

if (Test-Path $results) {
    [xml]$xml = Get-Content $results
    $run = $xml.'test-run'
    Write-Host "RESULT=$($run.result) total=$($run.total) passed=$($run.passed) failed=$($run.failed) duration=$($run.duration)s"
    $xml.SelectNodes("//test-case[not(@result='Passed')]") | ForEach-Object {
        Write-Host ("FAIL " + $_.fullname)
    }
    exit [int]$p.ExitCode
}

Write-Host "No results XML. See $log"
exit 1
