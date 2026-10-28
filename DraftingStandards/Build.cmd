@echo off
setlocal
cd /d "%~dp0"
if not exist "Compiled Executables" mkdir "Compiled Executables"
set "SEPROGRAM=%ProgramFiles%\Siemens\Solid Edge 2026\Program"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /platform:x64 /target:exe /link:"%SEPROGRAM%\interop.SolidEdgeDraftLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeFrameworkSupportLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeFrameworkLib.dll" /r:Microsoft.CSharp.dll /r:System.Xml.Linq.dll /out:"Compiled Executables\DimensionStandardTool.exe" DimensionStandardTool.cs AnnotationStandards.cs ReferenceAnnotationStyle.cs ReferenceDimensionStyle.cs AxisConstraints.cs
exit /b %errorlevel%
