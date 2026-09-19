@echo off
cd /d %~dp0
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
echo === cutify demo %DATE% %TIME% > cutify_log.txt
%BLENDER% -b --python cutify_demo_render.py -- "%~1" "cutify_out\%~n1_before.png" 0 >> cutify_log.txt 2>&1
%BLENDER% -b --python cutify_demo_render.py -- "%~1" "cutify_out\%~n1_after.png" 1 >> cutify_log.txt 2>&1
%BLENDER% -b -P cutify_assets.py -- --in "%~1" --out "cutify_out\%~n1_soft.fbx" >> cutify_log.txt 2>&1
echo exit %ERRORLEVEL% >> cutify_log.txt
