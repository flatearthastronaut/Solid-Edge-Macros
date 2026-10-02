@echo off
setlocal
cd /d "%~dp0"
if not exist "Compiled Executables" mkdir "Compiled Executables"
if errorlevel 1 exit /b 1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\BuildIcon.ps1"
if errorlevel 1 exit /b 1
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /r:Microsoft.CSharp.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /win32icon:"Assets\DualDimensionToggle.ico" /resource:"Assets\DualDimensionToggle.ico",DualDimensionToggle.ico /out:"Compiled Executables\Dual Dimension Toggle v1.6.exe" Program.cs StyleMap.cs Converter.cs FeatureFrameConverter.cs UnitText.cs CalloutConverter.cs CalloutHistory.cs CalloutFields.cs
if errorlevel 1 exit /b 1
echo Built Compiled Executables\Dual Dimension Toggle v1.6.exe
