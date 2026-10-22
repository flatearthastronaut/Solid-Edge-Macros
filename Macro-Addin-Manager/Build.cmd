@echo off
setlocal
cd /d "%~dp0"
if not exist "Compiled Executables" mkdir "Compiled Executables"
if errorlevel 1 exit /b 1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\BuildIcon.ps1"
if errorlevel 1 exit /b 1
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "OPTIONS=/nologo /target:winexe /platform:x64 /optimize+ /win32icon:Assets\ManagerIcon.ico /resource:Assets\ManagerIcon.ico,ManagerIcon.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Xml.Linq.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll"
set "SOURCES=Core.cs Program.cs Diagnostics.cs PackageIcons.cs Categories.cs UiArtwork.cs Publishing.cs"
"%CSC%" %OPTIONS% /out:"Compiled Executables\SE Macro Manager v1.9.exe" %SOURCES%
if errorlevel 1 exit /b 1
"%CSC%" %OPTIONS% /define:ADMIN /out:"Compiled Executables\SE Macro Manager Admin v1.9.exe" %SOURCES%
if errorlevel 1 exit /b 1
echo Built SE Macro Manager v1.9.exe and SE Macro Manager Admin v1.9.exe
