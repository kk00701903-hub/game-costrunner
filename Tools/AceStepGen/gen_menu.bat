@echo off
REM First track only: BGM_Menu (75 s). Log: gen_log.txt
cd /d %~dp0
set PYTHONUTF8=1
set PYTHONIOENCODING=utf-8
set PYTHONPATH=C:\dev\ACE-Step-1.5
C:\dev\ACE-Step-1.5\venv_cpu\Scripts\python.exe -u generate.py --only BGM_Menu --takes 1 > gen_log.txt 2>&1
echo done >> gen_log.txt
