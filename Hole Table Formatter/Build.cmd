@echo off
setlocal
cd /d "%~dp0"
if not exist "Compiled Executables" mkdir "Compiled Executables"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /r:Microsoft.CSharp.dll /r:System.Core.dll /r:System.Xml.Linq.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /resource:DowelSeats.xml,HoleTableFormatter.DowelSeats.xml /out:"Compiled Executables\Hole Table Formatter v0.1.exe" Program.cs Automation.cs Rules.cs ComSupport.cs
if errorlevel 1 exit /b 1
echo Built Hole Table Formatter v0.1.exe
