@echo off
setlocal
cd /d "%~dp0"
set "SEPART=%ProgramFiles%\Siemens\Solid Edge 2026\Program\interop.SolidEdgePartLib.dll"
if not exist "%SEPART%" (
  echo Cannot find Solid Edge 2026 part automation library: %SEPART%
  exit /b 1
)
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /link:"%SEPART%" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\UIAutomationClient.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\UIAutomationTypes.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll" /r:Microsoft.CSharp.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:SketchToModels.exe SketchToModels.cs
if errorlevel 1 exit /b 1
echo Built SketchToModels.exe

