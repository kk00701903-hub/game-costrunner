# Coast Run: integration test + Windows test build package
param(
    [switch]$SkipBuild,
    [switch]$SkipUnityAudit
)

$ErrorActionPreference = "Stop"
$Project = Split-Path -Parent $PSScriptRoot
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Unity.exe"
$Dist = Join-Path $Project "Dist\CoastRun_Test"
$Zip = Join-Path $Project "Dist\CoastRun_Test_Windows_x64.zip"
$ReportDir = Join-Path $Project "Assets\_Guide\Capture\visual_compare"
$UnityLog = Join-Path $Project "Temp\test_pipeline.log"

if (-not (Test-Path $Unity)) { Write-Error "Unity not found: $Unity" }
New-Item -ItemType Directory -Force -Path $ReportDir | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $Project "Temp") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $Project "Dist") | Out-Null

Write-Host "=== 1) File-level audit ==="
& powershell -NoProfile -File (Join-Path $PSScriptRoot "integrate-audit.ps1")
$fileReport = Join-Path $Project "Temp\visual_compare\integration_report.txt"
if (Test-Path $fileReport) {
    Copy-Item $fileReport (Join-Path $ReportDir "integration_report_files.txt") -Force
}

function Invoke-UnityMethod([string]$method, [string]$logFile, [switch]$WithGraphics) {
    $unityArgs = @("-batchmode", "-quit", "-projectPath", $Project, "-executeMethod", $method, "-logFile", $logFile)
    if (-not $WithGraphics) { $unityArgs = @("-nographics") + $unityArgs }
    Write-Host "Unity → $method"
    $p = Start-Process -FilePath $Unity -ArgumentList $unityArgs -PassThru -WindowStyle Hidden
    $timeoutMs = 45 * 60 * 1000
    if (-not $p.WaitForExit($timeoutMs)) {
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        throw "Unity timed out: $method"
    }
    return $p.ExitCode
}

if (-not $SkipUnityAudit) {
    Write-Host "=== 2) Unity integration audit ==="
    $code = Invoke-UnityMethod "CoastIntegrationTest.RunAuditBatch" (Join-Path $Project "Temp\integration_audit.log")
    if ($code -ne 0) { Write-Warning "Unity audit exit $code" }
    $unityReport = Join-Path $Project "Temp\visual_compare\integration_report.txt"
    $unityReport2 = Join-Path $ReportDir "integration_report_unity.txt"
    if (Test-Path $unityReport) {
        Copy-Item $unityReport $unityReport2 -Force
        Get-Content $unityReport
    } elseif (Test-Path $unityReport2) {
        Get-Content $unityReport2
    }
}

if (-not $SkipBuild) {
    Write-Host "=== 3) Windows x64 test build ==="
    $code = Invoke-UnityMethod "CoastTestBuild.BuildBatch" $UnityLog -WithGraphics
    if ($code -ne 0) {
        Write-Warning "Build exit $code — see $UnityLog"
        Select-String -Path $UnityLog -Pattern "error|RESULT=|BuildFailed|Exception|Scripts have compiler" | Select-Object -Last 40
        exit $code
    }

    if (-not (Test-Path (Join-Path $Dist "CoastRun.exe"))) {
        Write-Error "CoastRun.exe missing in $Dist — see $UnityLog"
    }

    Write-Host "=== 4) Zip package ==="
    if (Test-Path $Zip) { Remove-Item $Zip -Force }
    Compress-Archive -Path (Join-Path $Dist "*") -DestinationPath $Zip -Force
    Write-Host "Install/test package: $Zip"
    Get-ChildItem $Dist | Format-Table Name, Length
    Get-Item $Zip | Format-List FullName, Length, LastWriteTime
}

Write-Host "DONE"
