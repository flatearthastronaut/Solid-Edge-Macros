@echo off
setlocal
cd /d "%~dp0"
set "SEPROGRAM=%ProgramFiles%\Siemens\Solid Edge 2026\Program"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /link:"%SEPROGRAM%\interop.SolidEdgePartLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeFrameworkLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeGeometryLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeFrameworkSupportLib.dll" /r:System.Xml.dll /resource:CounterboreChart.xml,BoltCircleHoles.CounterboreChart.xml /r:System.Data.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /out:BoltCircleHoles-v0.16.exe BoltCircleHoles.cs HoleEngine.cs RunLog.cs PortableChart.cs A2Chart.cs HoleSymbol.cs
if errorlevel 1 exit /b 1
if not exist "Compiled Executables" mkdir "Compiled Executables"
copy /y "BoltCircleHoles-v0.16.exe" "Compiled Executables\BoltCircleHoles-v0.16.exe" >nul
copy /y "C'bore Chart.xls" "Compiled Executables\C'bore Chart.xls" >nul
echo Built BoltCircleHoles-v0.16.exe















