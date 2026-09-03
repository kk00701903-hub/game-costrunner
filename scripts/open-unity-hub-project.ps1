# Register / open Coast Run in Unity Hub and launch the editor for source testing.
# Usage:
#   .\scripts\open-unity-hub-project.ps1
#   .\scripts\open-unity-hub-project.ps1 -Play
#   .\scripts\open-unity-hub-project.ps1 -PrepareOnly

param(
    [switch]$Play,
    [switch]$PrepareOnly,
    [switch]$HubOnly
)

$ErrorActionPreference = "Stop"
$Project = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$WantedVersion = "6000.5.10f1"
$UnityExe = "C:\Program Files\Unity\Hub\Editor\$WantedVersion\Editor\Unity.exe"
$HubExe = "C:\Program Files\Unity Hub\Unity Hub.exe"

Write-Host "Project : $Project"
Write-Host "Editor  : $WantedVersion"

function Register-HubProject {
    $py = @"
import json, sqlite3, time, uuid, os
from pathlib import Path
db = Path(os.environ['APPDATA']) / 'UnityHub' / 'hub.db'
path = r'''$Project'''
now = int(time.time() * 1000)
data = {
    'title': 'Coast Run',
    'sourceFingerprint': f'{now}.0,0,0,0,0,0,0,0,{now}.0,0,0,0,0,0|spn:false;eds:',
    'lastModified': now,
    'isCustomEditor': False,
    'path': path,
    'containingFolderPath': str(Path(path).parent),
    'version': '$WantedVersion',
    'architecture': 'x86_64',
    'changeset': '3bd4f66ad299',
    'isFavorite': True,
    'cloudEnabled': False,
    'projectName': 'Coast Run',
    'hasCustomDisplayName': False,
    'buildTarget': 'StandaloneWindows64',
    'renderPipeline': 'URP',
    'localProjectId': uuid.uuid4().hex,
}
payload = json.dumps(data, ensure_ascii=False)
con = sqlite3.connect(str(db))
cur = con.cursor()
cur.execute('DELETE FROM projects WHERE lower(path)=lower(?)', (path,))
cur.execute('INSERT INTO projects(path, data, updated_at) VALUES (?,?,?)', (path, payload, now))
con.commit()
con.close()
print('Hub registered:', path)
"@
    python -c $py
}

Register-HubProject

if ($HubOnly) {
    if (Test-Path $HubExe) {
        Start-Process $HubExe
        Write-Host "Unity Hub opened — click Coast Run (favorite)."
    }
    exit 0
}

if (-not (Test-Path $UnityExe)) {
    Write-Host "Unity $WantedVersion not found at:"
    Write-Host "  $UnityExe"
    Write-Host "Install that editor in Hub, or open the project from Hub UI."
    if (Test-Path $HubExe) { Start-Process $HubExe }
    exit 1
}

New-Item -ItemType Directory -Force -Path (Join-Path $Project "Temp") | Out-Null
$log = Join-Path $Project "Temp\coast-hub-open.log"

$args = @(
    "-projectPath", $Project,
    "-logFile", $log
)

if ($PrepareOnly) {
    $args += @(
        "-batchmode", "-nographics", "-quit",
        "-executeMethod", "CoastRun.Editor.CoastRunPlayMenu.PrepareForHubTest"
    )
    Write-Host "Batch prepare..."
    & $UnityExe @args
    exit $LASTEXITCODE
}

if ($Play) {
    # Open Boot scene path via -executeMethod after load is flaky; use -CoastPlay if bootstrap listens,
    # otherwise open editor and user hits Play From Boot.
    $args += "-executeMethod", "CoastRun.Editor.CoastRunPlayMenu.PlayFromBoot"
}

Write-Host "Launching Unity Editor..."
Start-Process -FilePath $UnityExe -ArgumentList $args
Write-Host "Log: $log"
Write-Host "In Editor: Coast Run → Play From Boot (Ctrl+Shift+B)"
