@echo off
setlocal
cd /d "%~dp0"
if not exist "Compiled Executables" mkdir "Compiled Executables"
set "SEPROGRAM=%ProgramFiles%\Siemens\Solid Edge 2026\Program"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /link:"%SEPROGRAM%\interop.SolidEdgeAssemblyLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgePartLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeFrameworkLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeGeometryLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeFrameworkSupportLib.dll" /r:Microsoft.CSharp.dll /r:System.Xml.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /resource:"..\Bolt Circle Holes\CounterboreChart.xml",CounterboreChart.xml /out:"Compiled Executables\PopulateSHCS-v1.0.exe" Core.cs Geometry.cs Engine.cs StandardParts.cs Program.cs
if errorlevel 1 exit /b 1

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "..\Hammill Common\Package-CompiledMacro.ps1" -Project . -Executable "Compiled Executables\PopulateSHCS-v1.0.exe"
exit /b %errorlevel%
