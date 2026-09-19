@echo off
cd /d %~dp0
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
echo === sharpen %DATE% %TIME% > sharpen_log.txt
%BLENDER% -b --python sharpen_assets.py -- %* >> sharpen_run.txt 2>&1
echo exit %ERRORLEVEL% >> sharpen_log.txt
