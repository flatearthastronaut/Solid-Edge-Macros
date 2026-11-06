@echo off
setlocal
cd /d "%~dp0"
if not exist "tests\bin" mkdir "tests\bin"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /r:System.Core.dll /r:System.Xml.Linq.dll /resource:DowelSeats.xml,HoleTableFormatter.DowelSeats.xml /out:"tests\bin\RulesTests.exe" Rules.cs tests\RulesTests.cs
if errorlevel 1 exit /b 1
"tests\bin\RulesTests.exe"
exit /b %errorlevel%
