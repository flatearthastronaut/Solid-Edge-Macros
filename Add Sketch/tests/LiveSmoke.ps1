$ErrorActionPreference='Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    New-Item -ItemType Directory -Force -Path 'tests\bin' | Out-Null
    & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /r:Microsoft.CSharp.dll /out:tests\bin\LiveSmoke.exe tests\LiveSmoke.cs SketchEngine.cs '..\Hammill Common\ComSupport.cs'
    if($LASTEXITCODE -ne 0){throw 'Live acceptance compilation failed.'}
    & .\tests\bin\LiveSmoke.exe
    if($LASTEXITCODE -ne 0){throw 'Live Add Sketch acceptance failed.'}
} finally {Pop-Location}
