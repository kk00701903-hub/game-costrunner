@echo off
cd /d %~dp0
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
echo === haneul chibi rig %DATE% %TIME% > haneul_rig_log.txt
%BLENDER% -b --python haneul_chibi_rig.py >> haneul_rig_log.txt 2>&1
echo exit %ERRORLEVEL% >> haneul_rig_log.txt
