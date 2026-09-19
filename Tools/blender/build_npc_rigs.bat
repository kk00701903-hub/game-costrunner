@echo off
cd /d %~dp0
set BLENDER="C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
echo === npc chibi rigs %DATE% %TIME% > npc_rig_log.txt
%BLENDER% -b --python npc_chibi_rig.py >> npc_rig_log.txt 2>&1
echo exit %ERRORLEVEL% >> npc_rig_log.txt
