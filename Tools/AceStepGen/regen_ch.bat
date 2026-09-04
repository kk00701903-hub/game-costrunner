@echo off
REM Re-render the five chapter run tracks with the current tracks.json prompts + stems.
cd /d %~dp0
set PYTHONUTF8=1
set PYTHONIOENCODING=utf-8
set PYTHONPATH=C:\dev\ACE-Step-1.5
C:\dev\ACE-Step-1.5\venv_cpu\Scripts\python.exe -u generate.py --only BGM_CH1 BGM_CH2 BGM_CH3 BGM_CH4 BGM_CH5 --redo --stems --takes 1 > gen_log.txt 2>&1
echo done >> gen_log.txt
