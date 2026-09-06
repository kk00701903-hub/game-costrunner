@echo off
REM One-click: close Unity (memory) -> ACE-Step API -> 7 SFX stingers -> stop ACE -> reopen Unity.
REM Log: gen_sfx_log.txt next to this script. ASCII only.
cd /d %~dp0
echo [%date% %time%] gen_sfx start > gen_sfx_log.txt
powershell -NoProfile -Command "Get-Process Unity -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force; 'killed unity ' + $_.Id }" >> gen_sfx_log.txt 2>&1
timeout /t 5 /nobreak >nul
del /f /q C:\dev\game\Temp\UnityLockfile >nul 2>&1
start "ACE-Step API" /min "%~dp0start_api_cpu.bat"
set PYTHONUTF8=1
set PYTHONIOENCODING=utf-8
set PYTHONPATH=C:\dev\ACE-Step-1.5
echo waiting for api >> gen_sfx_log.txt
C:\dev\ACE-Step-1.5\venv_cpu\Scripts\python.exe -u wait_api.py >> gen_sfx_log.txt 2>&1
C:\dev\ACE-Step-1.5\venv_cpu\Scripts\python.exe -u generate.py --group sfx --out C:\dev\game\Assets\Resources\CoastRun\SFX --takes 1 --no-loopfix --redo >> gen_sfx_log.txt 2>&1
echo [%date% %time%] generate done >> gen_sfx_log.txt
powershell -NoProfile -Command "Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like '*api_server.py*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }" >> gen_sfx_log.txt 2>&1
timeout /t 3 /nobreak >nul
start "" "C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Unity.exe" -projectPath C:\dev\game
echo [%date% %time%] unity reopened >> gen_sfx_log.txt
echo done >> gen_sfx_log.txt
