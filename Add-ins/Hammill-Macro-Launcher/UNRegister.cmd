@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -Command "if (Get-Process -Name Edge -ErrorAction SilentlyContinue) { Write-Error 'Close Solid Edge before unregistering the launcher.'; exit 1 }"
if errorlevel 1 exit /b 1
if not exist "Hammill.MacroLauncher.dll" exit /b 1
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe" /nologo /unregister "Hammill.MacroLauncher.dll"
exit /b %errorlevel%
