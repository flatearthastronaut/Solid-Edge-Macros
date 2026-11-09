@echo off
setlocal
cd /d "%~dp0"
if not exist "tests\bin" mkdir "tests\bin"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /r:Microsoft.CSharp.dll /r:System.Core.dll /r:System.Xml.Linq.dll /resource:DowelSeats.xml,HoleTableFormatter.DowelSeats.xml /out:"tests\bin\LiveTest.exe" Automation.cs Rules.cs ComSupport.cs tests\LiveTest.cs
if errorlevel 1 exit /b 1
"tests\bin\LiveTest.exe" "%CD%\Inspection"
exit /b %errorlevel%
