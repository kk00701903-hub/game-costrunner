@echo off
REM P1 tracks: cutscene scores + Memory_Mid/Cold. ACE-Step API (8001) must be running.
cd /d %~dp0
set PYTHONUTF8=1
set PYTHONIOENCODING=utf-8
set PYTHONPATH=C:\dev\ACE-Step-1.5
C:\dev\ACE-Step-1.5\venv_cpu\Scripts\python.exe -u generate.py --only BGM_Cine_Prologue BGM_Cine_CH2_Open BGM_Cine_CH3_Open BGM_Cine_CH4_Open BGM_Cine_CH5_Open BGM_Cine_CH1_Close BGM_Cine_CH2_Close BGM_Cine_CH3_Close BGM_Cine_CH4_Close BGM_Memory_Mid BGM_Memory_Cold --takes 1 > gen_log.txt 2>&1
echo done >> gen_log.txt
