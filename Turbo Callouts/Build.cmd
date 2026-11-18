@echo off
setlocal
cd /d "%~dp0"
if not exist "Compiled Executables" mkdir "Compiled Executables"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /r:Microsoft.CSharp.dll /r:System.Core.dll /r:System.Xml.Linq.dll /resource:"..\DraftingStandards\annotation-presentation-standard.xml",TurboCallout.AnnotationStandard /resource:"..\DraftingStandards\dimension-presentation-standard.xml",TurboCallout.DimensionStandard /r:System.Windows.Forms.dll /r:System.Drawing.dll /resource:"..\Turbo Hole Table Callouts\DowelSeats.xml",HoleTableFormatter.DowelSeats.xml /resource:"..\Turbo Hole Table Callouts\DraftCallout.txt",HoleTableFormatter.DraftCallout.txt /win32icon:"Assets\Turbo Callouts.ico" /resource:"Assets\Turbo Callouts.ico",TurboCallout.Icon /resource:"Assets\Turbo Callouts.png",TurboCallout.Image /out:"Compiled Executables\Turbo Callouts v1.8.exe" Program.cs MenuForm.cs Automation.cs NativeHole.cs LeaderSide.cs Notes.cs BoltCircleGeometry.cs BoltCircles.cs BoltCircleDimensions.cs BoltCircleAngles.cs "..\Turbo Hole Table Callouts\Rules.cs" "..\Turbo Hole Table Callouts\LinkedCallouts.cs" "..\Turbo Hole Table Callouts\ComSupport.cs"
if errorlevel 1 exit /b 1
copy /y "Assets\Turbo Callouts.ico" "Compiled Executables\Turbo Callouts.ico" >nul
copy /y "Assets\Turbo Callouts.png" "Compiled Executables\Turbo Callouts.png" >nul
echo Built "Turbo Callouts v1.8.exe"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "..\Hammill Common\Package-CompiledMacro.ps1" -Project . -Executable "Compiled Executables\Turbo Callouts v1.8.exe" -Icon "Assets\Turbo Callouts.ico" -Image "Assets\Turbo Callouts.png"
exit /b %errorlevel%
