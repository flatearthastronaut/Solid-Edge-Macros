@echo off
setlocal
cd /d "%~dp0"
if not exist "tests\bin" mkdir "tests\bin"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /main:Tests /resource:"Assets\ManagerIcon.ico",ManagerIcon.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Xml.Linq.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /out:"tests\bin\Tests.exe" Core.cs Program.cs Diagnostics.cs PackageIcons.cs Categories.cs UiArtwork.cs Publishing.cs ImplementationHelp.cs StableMacros.cs tests\Tests.cs tests\PublishingTests.cs tests\ImplementationHelpTests.cs tests\StableMacroTests.cs
if errorlevel 1 exit /b 1
"tests\bin\Tests.exe"
exit /b %errorlevel%
