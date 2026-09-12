@echo off
REM 64차: 제주 집 키트(JHouse_*)를 헤드리스로 빌드해 FBX 로 내보낸다.
cd /d %~dp0
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
if not exist %BLENDER% set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender-launcher.exe"
echo === jeju house kit build %DATE% %TIME% > jeju_house_log.txt
%BLENDER% -b --python jeju_house_kit.py >> jeju_house_log.txt 2>&1
echo exit %ERRORLEVEL% >> jeju_house_log.txt
