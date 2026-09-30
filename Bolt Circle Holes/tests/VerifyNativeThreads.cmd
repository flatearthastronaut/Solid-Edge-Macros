@echo off
setlocal
cd /d "%~dp0.."
set "SEPROGRAM=%ProgramFiles%\Siemens\Solid Edge 2026\Program"
call Build.cmd
if errorlevel 1 exit /b 1
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /r:BoltCircleHoles-v0.19.exe /link:"%SEPROGRAM%\interop.SolidEdgePartLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeFrameworkLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeGeometryLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeFrameworkSupportLib.dll" /out:tests\VerifyNativeThreads.exe tests\VerifyNativeThreads.cs
if errorlevel 1 exit /b 1
copy /y BoltCircleHoles-v0.19.exe tests\BoltCircleHoles-v0.19.exe >nul
rem Requires Solid Edge running. Creates and closes its own unsaved scratch part.
tests\VerifyNativeThreads.exe
exit /b %errorlevel%
