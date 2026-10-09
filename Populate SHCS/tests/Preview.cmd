@echo off
cd /d "%~dp0"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /r:System.Drawing.dll /r:System.Windows.Forms.dll /out:Preview.exe Preview.cs
if errorlevel 1 exit /b 1
Preview.exe
