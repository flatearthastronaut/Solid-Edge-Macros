@echo off
setlocal
cd /d "%~dp0"
if not exist "tests\bin" mkdir "tests\bin"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /main:Tests /resource:"Assets\O-Ring Finder.ico",ORingFinder.Icon /r:Microsoft.CSharp.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /out:tests\bin\Tests.exe Program.cs RingTable.cs SolidEdgeSession.cs ComSupport.cs tests\Tests.cs
if errorlevel 1 exit /b 1
tests\bin\Tests.exe
exit /b %errorlevel%
