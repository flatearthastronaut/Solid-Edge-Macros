@echo off
setlocal
cd /d "%~dp0\.."
set "SEPROGRAM=%ProgramFiles%\Siemens\Solid Edge 2026\Program"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /link:"%SEPROGRAM%\interop.SolidEdgeAssemblyLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgePartLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeFrameworkLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeGeometryLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeFrameworkSupportLib.dll" /r:Microsoft.CSharp.dll /r:System.Xml.dll /resource:"..\Bolt Circle Holes\CounterboreChart.xml",CounterboreChart.xml /out:tests\Tests.exe Core.cs Geometry.cs Engine.cs tests\Tests.cs tests\LiveTests.cs
exit /b %errorlevel%
