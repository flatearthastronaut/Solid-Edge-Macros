@echo off
setlocal
cd /d "%~dp0"
if not exist "Compiled Executables" mkdir "Compiled Executables"
if errorlevel 1 exit /b 1
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /r:Microsoft.CSharp.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:"Compiled Executables\SolidEdgeConvert.exe" Program.cs Conversion.cs SolidEdgeSession.cs ShellMenu.cs
if errorlevel 1 exit /b 1
echo Built Compiled Executables\SolidEdgeConvert.exe
