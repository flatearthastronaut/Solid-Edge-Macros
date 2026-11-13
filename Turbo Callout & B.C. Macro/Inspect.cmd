@echo off
setlocal
cd /d "%~dp0"
if not exist "Inspection" mkdir "Inspection"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /r:Microsoft.CSharp.dll /r:System.Core.dll /r:System.Xml.Linq.dll /resource:"..\DraftingStandards\annotation-presentation-standard.xml",TurboCallout.AnnotationStandard /resource:"..\Turbo Hole Table Callouts\DowelSeats.xml",HoleTableFormatter.DowelSeats.xml /resource:"..\Turbo Hole Table Callouts\DraftCallout.txt",HoleTableFormatter.DraftCallout.txt /out:Inspection\Inspect.exe tests\Inspect.cs Automation.cs NativeHole.cs Notes.cs BoltCircleGeometry.cs BoltCircles.cs "..\Turbo Hole Table Callouts\Rules.cs" "..\Turbo Hole Table Callouts\LinkedCallouts.cs" "..\Turbo Hole Table Callouts\ComSupport.cs"
if errorlevel 1 exit /b 1
Inspection\Inspect.exe %*
exit /b %errorlevel%
