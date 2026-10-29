@echo off
setlocal
cd /d "%~dp0"
set "SEPROGRAM=%ProgramFiles%\Siemens\Solid Edge 2026\Program"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /link:"%SEPROGRAM%\interop.SolidEdgeDraftLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgePartLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeFrameworkLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeGeometryLib.dll" /link:"%SEPROGRAM%\interop.SolidEdgeFrameworkSupportLib.dll" /r:Microsoft.CSharp.dll /r:System.Xml.Linq.dll /out:"Compiled Executables\CapDetailTool.exe" CapDetailTool.cs CreateCapDrawing.cs Cap19679.cs Verify19679.cs Placement19679.cs ComSupport.cs ..\DraftingStandards\AxisConstraints.cs ..\DraftingStandards\BaseOriginConstraints.cs ..\DraftingStandards\ReferenceDimensionStyle.cs ..\DraftingStandards\ReferenceAnnotationStyle.cs
exit /b %errorlevel%
