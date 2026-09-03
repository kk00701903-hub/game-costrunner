@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0playtest.ps1" %*
exit /b %ERRORLEVEL%
