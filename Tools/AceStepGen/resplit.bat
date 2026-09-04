@echo off
REM Rebuild chapter stems from saved takes with the current tracks.json bus mapping (no render).
cd /d %~dp0
set PYTHONUTF8=1
set PYTHONIOENCODING=utf-8
set PYTHONPATH=C:\dev\ACE-Step-1.5
C:\dev\ACE-Step-1.5\venv_cpu\Scripts\python.exe -u generate.py --group ch --stems --resplit %* > gen_log.txt 2>&1
echo done >> gen_log.txt
