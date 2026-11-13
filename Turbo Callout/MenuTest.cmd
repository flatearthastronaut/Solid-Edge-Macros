@echo off
setlocal
cd /d "%~dp0"
if not exist "Inspection" mkdir "Inspection"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /main:MenuTests /platform:x64 /optimize+ /r:Microsoft.CSharp.dll /r:System.Core.dll /r:System.Xml.Linq.dll /resource:"..\DraftingStandards\annotation-presentation-standard.xml",TurboCallout.AnnotationStandard /resource:"..\DraftingStandards\dimension-presentation-standard.xml",TurboCallout.DimensionStandard /r:System.Windows.Forms.dll /r:System.Drawing.dll /resource:"..\Turbo Hole Table Callouts\DowelSeats.xml",HoleTableFormatter.DowelSeats.xml /resource:"..\Turbo Hole Table Callouts\DraftCallout.txt",HoleTableFormatter.DraftCallout.txt /win32icon:"Assets\Turbo Callout.ico" /resource:"Assets\Turbo Callout.ico",TurboCallout.Icon /resource:"Assets\Turbo Callout.png",TurboCallout.Image /out:Inspection\MenuTests.exe tests\MenuTests.cs Program.cs MenuForm.cs Automation.cs NativeHole.cs LeaderSide.cs Notes.cs BoltCircleGeometry.cs BoltCircles.cs BoltCircleDimensions.cs "..\Turbo Hole Table Callouts\Rules.cs" "..\Turbo Hole Table Callouts\LinkedCallouts.cs" "..\Turbo Hole Table Callouts\ComSupport.cs"
if errorlevel 1 exit /b 1
Inspection\MenuTests.exe
exit /b %errorlevel%
