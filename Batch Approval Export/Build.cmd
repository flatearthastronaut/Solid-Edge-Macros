@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "..\Hammill Common\Build-Macro.ps1" -Project . -Name "Batch Approval Export" -Kind export
exit /b %errorlevel%
