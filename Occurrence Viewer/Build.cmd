@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "..\Hammill Common\Build-Macro.ps1" -Project . -Name "Occurrence Viewer" -Kind viewer -Version 1.1
exit /b %errorlevel%
