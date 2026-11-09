@echo off
setlocal
cd /d "%~dp0"
if not exist "tests\bin" mkdir "tests\bin"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /r:System.Core.dll /r:System.Xml.Linq.dll /resource:DowelSeats.xml,HoleTableFormatter.DowelSeats.xml /resource:DraftCallout.txt,HoleTableFormatter.DraftCallout.txt /out:"tests\bin\RulesTests.exe" Rules.cs LinkedCallouts.cs tests\RulesTests.cs
if not "%errorlevel%"=="0" exit /b %errorlevel%
"tests\bin\RulesTests.exe"
if not "%errorlevel%"=="0" exit /b %errorlevel%
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /main:UiTests /platform:x64 /r:Microsoft.CSharp.dll /r:System.Core.dll /r:System.Xml.Linq.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /resource:DowelSeats.xml,HoleTableFormatter.DowelSeats.xml /resource:DraftCallout.txt,HoleTableFormatter.DraftCallout.txt /resource:"Assets\Turbo Hole Table Callouts.ico",TurboHoleTableCallouts.Icon /out:"tests\bin\UiTests.exe" Program.cs Presentation.cs Automation.cs Rules.cs LinkedCallouts.cs ComSupport.cs tests\UiTests.cs
if not "%errorlevel%"=="0" exit /b %errorlevel%
"tests\bin\UiTests.exe"
exit /b %errorlevel%
