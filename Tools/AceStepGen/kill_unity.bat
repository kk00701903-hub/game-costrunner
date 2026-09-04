@echo off
REM Lists Unity processes, kills orphaned editor instances (no window), then reopens the project.
cd /d %~dp0
tasklist /FI "IMAGENAME eq Unity.exe" /V > proc.txt 2>&1
tasklist /FI "IMAGENAME eq Unity Hub.exe" >> proc.txt 2>&1
powershell -NoProfile -Command "Get-Process Unity -ErrorAction SilentlyContinue | Select-Object Id, MainWindowTitle, StartTime, Responding | Format-List" >> proc.txt 2>&1
powershell -NoProfile -Command "Get-Process Unity -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force; 'killed ' + $_.Id }" >> proc.txt 2>&1
timeout /t 3 /nobreak >nul
del /f /q C:\dev\game\Temp\UnityLockfile >> proc.txt 2>&1
start "" "C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Unity.exe" -projectPath C:\dev\game
echo done >> proc.txt
