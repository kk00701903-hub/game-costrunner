@echo off
cd /d %~dp0
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
echo === kid chibi rig %DATE% %TIME% > kid_rig_log.txt
%BLENDER% -b --python kid_chibi_rig.py >> kid_rig_log.txt 2>&1
echo exit %ERRORLEVEL% >> kid_rig_log.txt
