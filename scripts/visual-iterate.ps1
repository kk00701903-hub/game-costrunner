# Visual compare loop: Unity batch capture + score until pass threshold or max rounds.
param(
    [int]$MaxRounds = 12,
    [int]$PassScore = 10
)

$ErrorActionPreference = "Stop"
$Project = Split-Path -Parent $PSScriptRoot
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Unity.exe"
$OutDir = Join-Path $Project "Assets\_Guide\Capture\visual_compare"
$Log = Join-Path $Project "Temp\visual_iterate.log"

if (-not (Test-Path $Unity)) {
    Write-Error "Unity not found: $Unity"
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

for ($round = 1; $round -le $MaxRounds; $round++) {
    Write-Host "=== Visual iterate round $round / $MaxRounds ==="

    $args = @(
        "-batchmode", "-quit",
        "-projectPath", $Project,
        "-executeMethod", "CoastVisualIterate.RunBatch",
        "-logFile", $Log
    )

    & $Unity @args
    $code = $LASTEXITCODE
    if ($code -ne 0) {
        Write-Warning "Unity exit $code — see $Log"
        if ($round -lt $MaxRounds) { Start-Sleep -Seconds 3; continue }
        exit $code
    }

    $checklist = Join-Path $OutDir "checklist.txt"
    if (-not (Test-Path $checklist)) {
        Write-Warning "No checklist.txt — retrying"
        Start-Sleep -Seconds 2
        continue
    }

    $text = Get-Content $checklist -Raw -Encoding UTF8
    Write-Host $text

    if ($text -match "Score:\s*(\d+)\s*/\s*(\d+)") {
        $score = [int]$Matches[1]
        $total = [int]$Matches[2]
        Write-Host "Score $score / $total"
        if ($score -ge $PassScore) {
            Write-Host "PASS threshold met ($PassScore+)."
            exit 0
        }
    }

    Start-Sleep -Seconds 1
}

Write-Host "Max rounds reached without full pass — see Temp/visual_compare/"
exit 1
