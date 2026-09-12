@echo off
REM 59차: 도로변 소품 키트(Kerb_*)를 헤드리스로 빌드해 FBX 로 내보낸다.
cd /d %~dp0
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
if not exist %BLENDER% set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender-launcher.exe"
echo === kerb kit build %DATE% %TIME% > kerb_log.txt
%BLENDER% -b --python kerb_kit.py >> kerb_log.txt 2>&1
echo exit %ERRORLEVEL% >> kerb_log.txt
