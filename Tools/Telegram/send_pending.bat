@echo off
cd /d "%~dp0..\.."
python Tools\Telegram\send_pending.py
pause
