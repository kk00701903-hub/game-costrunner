@echo off
REM lycoris-lora / pytorch-wavelets pulled torch down to 2.12.1 while torchvision stayed
REM at 0.29.0 (built for 2.14) -> "operator torchvision::nms does not exist" at import.
REM Re-pin the matching CPU trio without touching anything else.
set LOG=%~dp0fix_log.txt
cd /d C:\dev\ACE-Step-1.5
call venv_cpu\Scripts\activate.bat
echo [%date% %time%] fix start > "%LOG%"
pip install --no-cache-dir --force-reinstall --no-deps torch==2.14.0 torchvision==0.29.0 torchaudio==2.11.0 --index-url https://download.pytorch.org/whl/cpu >> "%LOG%" 2>&1
python -c "import torch, torchvision, torchaudio; print('torch', torch.__version__, 'tv', torchvision.__version__, 'ta', torchaudio.__version__)" >> "%LOG%" 2>&1
set PYTHONPATH=C:\dev\ACE-Step-1.5
python -c "import acestep.api_server; print('api_server import OK')" >> "%LOG%" 2>&1
echo [%date% %time%] fix done >> "%LOG%"
