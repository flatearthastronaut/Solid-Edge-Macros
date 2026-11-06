@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "..\Hammill Common\Build-Macro.ps1" -Project . -Name "Add Sketch" -Kind sketch
exit /b %errorlevel%
