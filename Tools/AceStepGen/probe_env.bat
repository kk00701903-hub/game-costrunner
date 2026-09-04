@echo off
REM Environment probe for the ACE-Step install decision (GPU / driver / python / disk).
cd /d %~dp0
set OUT=env_probe.txt
echo === GPU === > %OUT%
powershell -NoProfile -Command "Get-CimInstance Win32_VideoController | Select-Object Name, DriverVersion, AdapterRAM | Format-List" >> %OUT% 2>&1
echo === PYTHON === >> %OUT%
python --version >> %OUT% 2>&1
py -0p >> %OUT% 2>&1
echo === GIT === >> %OUT%
git --version >> %OUT% 2>&1
echo === DISK === >> %OUT%
powershell -NoProfile -Command "Get-PSDrive C | Select-Object Used, Free | Format-List" >> %OUT% 2>&1
echo === RAM === >> %OUT%
powershell -NoProfile -Command "(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB" >> %OUT% 2>&1
echo === ACESTEP === >> %OUT%
if exist C:\dev\ACE-Step-1.5 (echo present >> %OUT%) else (echo absent >> %OUT%)
echo === OS === >> %OUT%
powershell -NoProfile -Command "(Get-CimInstance Win32_OperatingSystem).Caption + ' ' + (Get-CimInstance Win32_OperatingSystem).Version" >> %OUT% 2>&1
echo done >> %OUT%
