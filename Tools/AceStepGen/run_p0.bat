@echo off
REM P0 tracks (11) + chapter stems. ACE-Step API (8001) must be running. Log: gen_log.txt
cd /d %~dp0
set PYTHONUTF8=1
set PYTHONIOENCODING=utf-8
set PYTHONPATH=C:\dev\ACE-Step-1.5
if exist C:\dev\ACE-Step-1.5\venv_cpu\Scripts\python.exe (
  set PY=C:\dev\ACE-Step-1.5\venv_cpu\Scripts\python.exe
) else (
  set PY=python
)
%PY% -u generate.py --priority P0 --stems --takes 1 %* > gen_log.txt 2>&1
echo done >> gen_log.txt
