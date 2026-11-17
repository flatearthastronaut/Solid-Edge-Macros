@echo off
setlocal
cd /d "%~dp0"
if not exist "Compiled Executables" mkdir "Compiled Executables"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "..\Hammill Common\Build-Icon.ps1" -Folder Assets -Name "Update Dimension Styles" -Kind sketch
if errorlevel 1 exit /b 1
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /r:Microsoft.CSharp.dll /r:System.Core.dll /r:System.Xml.Linq.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /win32icon:"Assets\Update Dimension Styles.ico" /resource:"Assets\Update Dimension Styles.ico",Macro.Icon /out:"Compiled Executables\Update Dimension Styles v1.0.exe" Program.cs StyleRepair.cs MasterDocument.cs "..\Hammill Common\ComSupport.cs" "..\Hammill Common\MacroHost.cs" "..\Add-ins\Hammill-Macro-Launcher\Catalog.cs" "..\Add-ins\Hammill-Macro-Launcher\VideoTooltip.cs" "..\Add-ins\Hammill-Macro-Launcher\DimensionStyleCycle\StyleSequence.cs"
if errorlevel 1 exit /b 1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "..\Hammill Common\Package-CompiledMacro.ps1" -Project . -Executable "Compiled Executables\Update Dimension Styles v1.0.exe" -Icon "Assets\Update Dimension Styles.ico" -Image "Assets\Update Dimension Styles.png" -DataFiles "normal (new design).dft"
exit /b %errorlevel%
