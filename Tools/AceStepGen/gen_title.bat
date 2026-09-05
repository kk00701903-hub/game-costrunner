@echo off
REM Title theme + 15s opening cue. Log: gen_title_log.txt
cd /d %~dp0
set PYTHONUTF8=1
set PYTHONIOENCODING=utf-8
set PYTHONPATH=C:\dev\ACE-Step-1.5
C:\dev\ACE-Step-1.5\venv_cpu\Scripts\python.exe -u generate.py --only BGM_Opening --takes 1 > gen_title_log.txt 2>&1
C:\dev\ACE-Step-1.5\venv_cpu\Scripts\python.exe -u generate.py --only BGM_Title --takes 1 >> gen_title_log.txt 2>&1
echo done >> gen_title_log.txt
