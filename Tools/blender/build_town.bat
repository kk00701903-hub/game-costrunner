@echo off
cd /d %~dp0
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
echo === town kit build %DATE% %TIME% > town_log.txt
%BLENDER% -b --python town_kit.py >> town_log.txt 2>&1
echo exit %ERRORLEVEL% >> town_log.txt
