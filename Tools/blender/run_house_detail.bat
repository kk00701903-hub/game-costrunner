@echo off
cd /d %~dp0
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
echo === house detail %DATE% %TIME% > house_detail_log.txt
%BLENDER% -b --python house_detail_render.py >> house_detail_log.txt 2>&1
echo exit %ERRORLEVEL% >> house_detail_log.txt
