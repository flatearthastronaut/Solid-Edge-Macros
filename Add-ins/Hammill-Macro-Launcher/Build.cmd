@echo off
setlocal
cd /d "%~dp0"
set "HAMMILL_OUTPUT=Compiled Executables\Hammill-Macro-Launcher-v0.1"
if not exist "%HAMMILL_OUTPUT%" mkdir "%HAMMILL_OUTPUT%"
if errorlevel 1 exit /b 1
set "HAMMILL_CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "HAMMILL_REFS=/r:System.Core.dll /r:System.Xml.Linq.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll"
"%HAMMILL_CSC%" /nologo /target:library /platform:x64 /optimize+ %HAMMILL_REFS% /r:"lib\Interop.SolidEdgeFramework.dll" /out:"%HAMMILL_OUTPUT%\Hammill.MacroLauncher.dll" Catalog.cs RibbonState.cs IconResources.cs Addin.cs
if errorlevel 1 exit /b 1
"%HAMMILL_CSC%" /nologo /target:library /platform:x64 /optimize+ /out:"%HAMMILL_OUTPUT%\Hammill.MacroLauncher.Resources.dll" ResourceTemplate.cs
if errorlevel 1 exit /b 1
"%HAMMILL_CSC%" /nologo /target:exe /platform:x64 /optimize+ /r:System.Core.dll /r:System.Xml.Linq.dll /out:"%HAMMILL_OUTPUT%\Hammill Macro Catalog Check.exe" Catalog.cs CatalogCheck.cs
if errorlevel 1 exit /b 1
copy /y "lib\Interop.SolidEdgeFramework.dll" "%HAMMILL_OUTPUT%\Interop.SolidEdgeFramework.dll" >nul
if errorlevel 1 exit /b 1
copy /y Register.cmd "%HAMMILL_OUTPUT%\Register.cmd" >nul
if errorlevel 1 exit /b 1
copy /y UNRegister.cmd "%HAMMILL_OUTPUT%\UNRegister.cmd" >nul
if errorlevel 1 exit /b 1
copy /y README.md "%HAMMILL_OUTPUT%\README.md" >nul
if errorlevel 1 exit /b 1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Package.ps1
if errorlevel 1 exit /b 1
echo Built Hammill Macro Launcher v0.1.
