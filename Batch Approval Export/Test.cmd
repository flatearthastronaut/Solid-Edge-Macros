@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "..\Hammill Common\Test-Macro.ps1" -Project . -Name "Batch Approval Export"
exit /b %errorlevel%
