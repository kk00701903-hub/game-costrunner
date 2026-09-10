@echo off
cd /d %~dp0
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
if not exist %BLENDER% set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender-launcher.exe"
echo === pet kit %DATE% %TIME% > pet_log.txt
%BLENDER% -b --python pet_kit.py >> pet_log.txt 2>&1
echo exit %ERRORLEVEL% >> pet_log.txt
