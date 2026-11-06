@echo off
setlocal
cd /d "%~dp0"
if not exist "Compiled Executables" mkdir "Compiled Executables"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File BuildAssets.ps1
if errorlevel 1 exit /b 1
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /win32icon:"Assets\O-Ring Finder.ico" /resource:"Assets\O-Ring Finder.ico",ORingFinder.Icon /r:Microsoft.CSharp.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /out:"Compiled Executables\O-Ring Finder v1.0.exe" Program.cs RingTable.cs SolidEdgeSession.cs ComSupport.cs
if errorlevel 1 exit /b 1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Package.ps1
exit /b %errorlevel%
