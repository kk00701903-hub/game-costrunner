@echo off
REM ACE-Step REST API on CPU (port 8001). Log: api_log.txt next to this script.
REM ASCII only: cmd parses batch files in the OEM codepage.
set ROOT=C:\dev\ACE-Step-1.5

REM Stop any previous server so the port is free.
powershell -NoProfile -Command "Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like '*api_server.py*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }" >nul 2>&1

REM UTF-8 everywhere: the server logs em dashes; with cp949 stdout the task fails.
set PYTHONUTF8=1
set PYTHONIOENCODING=utf-8
chcp 65001 >nul

set ACESTEP_DEVICE=cpu
REM Server aborts a render after 600 s by default; CPU renders of 2-3 min tracks take 10-15 min.
set ACESTEP_GENERATION_TIMEOUT=3600
set ACESTEP_INIT_LLM=false
set ACESTEP_LM_BACKEND=pt
set TORCH_COMPILE_BACKEND=eager
set TOKENIZERS_PARALLELISM=false
set ACESTEP_OFFLOAD_TO_CPU=false
set OMP_NUM_THREADS=8
set PYTHONPATH=%ROOT%
cd /d %ROOT%
call venv_cpu\Scripts\activate.bat
echo [%date% %time%] api start > "%~dp0api_log.txt"
echo cwd=%CD% >> "%~dp0api_log.txt"
python -u acestep\api_server.py --host 127.0.0.1 --port 8001 >> "%~dp0api_log.txt" 2>&1
