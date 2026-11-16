$ErrorActionPreference='Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    New-Item -ItemType Directory -Force -Path 'tests\bin' | Out-Null
    & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /main:InspectPlanes /r:Microsoft.CSharp.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll '/resource:Assets\Add Sketch.ico,Macro.Icon' /out:tests\bin\InspectPlanes.exe tests\InspectPlanes.cs Program.cs SketchEngine.cs '..\Hammill Common\ComSupport.cs' '..\Hammill Common\MacroHost.cs'
    if($LASTEXITCODE -ne 0){throw 'Diagnostic compilation failed.'}
    & .\tests\bin\InspectPlanes.exe
    if($LASTEXITCODE -ne 0){throw 'Read-only plane diagnostic failed.'}
} finally {Pop-Location}
