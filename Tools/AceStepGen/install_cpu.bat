@echo off
setlocal
REM ACE-Step 1.5 — CPU-only install for this PC (integrated Radeon, no ROCm support).
REM Everything is logged to install_log.txt next to this script so progress can be read.
set LOG=%~dp0install_log.txt
set ROOT=C:\dev\ACE-Step-1.5
echo [%date% %time%] install start > "%LOG%"

cd /d C:\dev
if not exist "%ROOT%\.git" (
  echo [step] git clone >> "%LOG%"
  git clone --depth 1 https://github.com/ACE-Step/ACE-Step-1.5.git >> "%LOG%" 2>&1
)
cd /d "%ROOT%" || (echo [fail] repo missing >> "%LOG%" & exit /b 1)

if not exist venv_cpu\Scripts\python.exe (
  echo [step] venv >> "%LOG%"
  python -m venv venv_cpu >> "%LOG%" 2>&1
)
call venv_cpu\Scripts\activate.bat

echo [step] pip upgrade >> "%LOG%"
python -m pip install --upgrade pip >> "%LOG%" 2>&1

echo [step] torch cpu >> "%LOG%"
pip install --no-cache-dir torch torchaudio torchvision --index-url https://download.pytorch.org/whl/cpu >> "%LOG%" 2>&1

echo [step] core deps (requirements-rocm.txt = everything except torch/torchao) >> "%LOG%"
pip install -r requirements-rocm.txt >> "%LOG%" 2>&1

echo [step] extra deps from pyproject >> "%LOG%"
pip install diskcache toml typer-slim pytorch-wavelets pywavelets lycoris-lora "setuptools<72" >> "%LOG%" 2>&1

echo [step] demucs (stem split) >> "%LOG%"
pip install demucs >> "%LOG%" 2>&1

echo [step] sanity >> "%LOG%"
python -c "import torch, transformers, diffusers, soundfile; print('torch', torch.__version__, 'cuda', torch.cuda.is_available())" >> "%LOG%" 2>&1

echo [step] model download (~10GB, HuggingFace) >> "%LOG%"
python -m acestep.model_downloader >> "%LOG%" 2>&1

echo [%date% %time%] install done >> "%LOG%"
echo done > "%~dp0install_done.txt"
endlocal
