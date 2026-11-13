@echo off
setlocal
cd /d "%~dp0"
if not exist "Compiled Executables" mkdir "Compiled Executables"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /r:Microsoft.CSharp.dll /r:System.Core.dll /r:System.Xml.Linq.dll /resource:"..\DraftingStandards\annotation-presentation-standard.xml",TurboCallout.AnnotationStandard /resource:"..\DraftingStandards\dimension-presentation-standard.xml",TurboCallout.DimensionStandard /r:System.Windows.Forms.dll /r:System.Drawing.dll /resource:"..\Turbo Hole Table Callouts\DowelSeats.xml",HoleTableFormatter.DowelSeats.xml /resource:"..\Turbo Hole Table Callouts\DraftCallout.txt",HoleTableFormatter.DraftCallout.txt /win32icon:"Assets\Turbo Callout.ico" /resource:"Assets\Turbo Callout.ico",TurboCallout.Icon /resource:"Assets\Turbo Callout.png",TurboCallout.Image /out:"Compiled Executables\Turbo Callout v1.3.exe" Program.cs MenuForm.cs Automation.cs NativeHole.cs LeaderSide.cs Notes.cs BoltCircleGeometry.cs BoltCircles.cs BoltCircleDimensions.cs "..\Turbo Hole Table Callouts\Rules.cs" "..\Turbo Hole Table Callouts\LinkedCallouts.cs" "..\Turbo Hole Table Callouts\ComSupport.cs"
if errorlevel 1 exit /b 1
copy /y "Assets\Turbo Callout.ico" "Compiled Executables\Turbo Callout.ico" >nul
copy /y "Assets\Turbo Callout.png" "Compiled Executables\Turbo Callout.png" >nul
echo Built "Turbo Callout v1.3.exe"
