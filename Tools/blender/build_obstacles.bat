@echo off
REM 14차-11: 장애물·차량 3D 키트를 헤드리스로 빌드해 Unity 프로젝트에 FBX 로 내보낸다.
cd /d %~dp0
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
if not exist %BLENDER% set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender-launcher.exe"
echo === obstacle kit build %DATE% %TIME% > obstacle_log.txt
%BLENDER% -b --python obstacle_kit.py >> obstacle_log.txt 2>&1
echo exit %ERRORLEVEL% >> obstacle_log.txt
