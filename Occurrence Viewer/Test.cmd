@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "..\Hammill Common\Test-Macro.ps1" -Project . -Name "Occurrence Viewer"
exit /b %errorlevel%
