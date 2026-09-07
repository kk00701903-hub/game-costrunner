@echo off
cd /d %~dp0
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
if not exist %BLENDER% set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender-launcher.exe"
echo === bird pets %DATE% %TIME% > bird_log.txt
%BLENDER% -b --python bird_pet.py >> bird_log.txt 2>&1
echo exit %ERRORLEVEL% >> bird_log.txt
