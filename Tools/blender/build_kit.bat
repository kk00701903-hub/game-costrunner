@echo off
REM Builds the Jeju building/prop kit headlessly and exports FBX into the Unity project.
cd /d %~dp0
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
if not exist %BLENDER% set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender-launcher.exe"
echo === Jeju kit build %DATE% %TIME% > build_log.txt
%BLENDER% -b --python jeju_kit.py >> build_log.txt 2>&1
echo exit %ERRORLEVEL% >> build_log.txt
