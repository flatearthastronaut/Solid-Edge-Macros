@echo off
setlocal
cd /d "%~dp0"
if not exist "Compiled Executables" mkdir "Compiled Executables"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /r:Microsoft.CSharp.dll /r:System.Core.dll /r:System.Xml.Linq.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /resource:DowelSeats.xml,HoleTableFormatter.DowelSeats.xml /resource:DraftCallout.txt,HoleTableFormatter.DraftCallout.txt /win32icon:"Assets\Turbo Hole Table Callouts.ico" /resource:"Assets\Turbo Hole Table Callouts.ico",TurboHoleTableCallouts.Icon /out:"Compiled Executables\Turbo Hole Table Callouts v0.6.exe" Program.cs Presentation.cs Automation.cs Rules.cs LinkedCallouts.cs ComSupport.cs
if errorlevel 1 exit /b 1
echo Built Turbo Hole Table Callouts v0.6.exe
