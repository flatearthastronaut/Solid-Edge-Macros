@echo off
setlocal
rem Compile both documented sources; keep runnable artifacts in the required subfolder.
cd /d "%~dp0"
if not exist "Compiled Executables" mkdir "Compiled Executables"
if errorlevel 1 exit /b 1
set "SEPART=%ProgramFiles%\Siemens\Solid Edge 2026\Program\interop.SolidEdgePartLib.dll"
if not exist "%SEPART%" (
  echo Cannot find Solid Edge 2026 part automation library: %SEPART%
  exit /b 1
)
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /link:"%SEPART%" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\UIAutomationClient.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\UIAutomationTypes.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll" /r:Microsoft.CSharp.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:"Compiled Executables\SketchToModels.exe" SketchToModels.cs ComSupport.cs
if errorlevel 1 exit /b 1
rem Bundle the template setting and matching ribbon images beside the executable.
copy /y "Compiled Executables\SketchToModels.exe" "Compiled Executables\SketchToModels-v2.31.exe" >nul
if errorlevel 1 exit /b 1
copy /y "template.txt" "Compiled Executables\template.txt" >nul
if errorlevel 1 exit /b 1
copy /y "SketchToModels.png" "Compiled Executables\SketchToModels.png" >nul
if errorlevel 1 exit /b 1
copy /y "SketchToModels-v2.31.png" "Compiled Executables\SketchToModels-v2.31.png" >nul
if errorlevel 1 exit /b 1
copy /y "SketchToModels.ico" "Compiled Executables\SketchToModels.ico" >nul
if errorlevel 1 exit /b 1
echo Built Compiled Executables\SketchToModels-v2.31.exe



