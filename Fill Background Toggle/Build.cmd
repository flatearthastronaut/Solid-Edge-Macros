@echo off
setlocal
cd /d "%~dp0"
if not exist "Compiled Executables" mkdir "Compiled Executables"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\BuildIcon.ps1"
if errorlevel 1 exit /b 1
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /win32icon:"Assets\FillBackgroundToggle.ico" /r:Microsoft.CSharp.dll /r:System.Windows.Forms.dll /out:"Compiled Executables\Fill Background Toggle v1.1.exe" Program.cs ComSupport.cs
if errorlevel 1 exit /b 1


powershell.exe -NoProfile -ExecutionPolicy Bypass -File "..\Hammill Common\Package-CompiledMacro.ps1" -Project . -Executable "Compiled Executables\Fill Background Toggle v1.1.exe" -Icon "Assets\FillBackgroundToggle.ico" -Image "Assets\FillBackgroundToggle.png"
exit /b %errorlevel%
