@echo off
REM Environment probe + emergency stop for a running generate.py batch.
cd /d %~dp0
set OUT=env_probe.txt
echo === STOP generate.py === > %OUT%
powershell -NoProfile -Command "Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like '*generate.py*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force; 'killed ' + $_.ProcessId }" >> %OUT% 2>&1
echo === GPU === >> %OUT%
powershell -NoProfile -Command "Get-CimInstance Win32_VideoController | Select-Object Name, DriverVersion, AdapterRAM | Format-List" >> %OUT% 2>&1
echo === PYTHON === >> %OUT%
python --version >> %OUT% 2>&1
echo === DISK === >> %OUT%
powershell -NoProfile -Command "Get-PSDrive C | Select-Object Used, Free | Format-List" >> %OUT% 2>&1
echo done >> %OUT%
