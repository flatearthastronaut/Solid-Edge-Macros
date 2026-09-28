# Optional: requires an open, idle Solid Edge session. Creates only its own draft.
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
    & $compiler /nologo /target:exe /platform:x64 /r:Microsoft.CSharp.dll /out:tests\LivePdfSmokeTest.exe tests\LivePdfSmokeTest.cs Conversion.cs SolidEdgeSession.cs
    if ($LASTEXITCODE -ne 0) { throw 'Live PDF test compilation failed.' }
    & .\tests\LivePdfSmokeTest.exe
    if ($LASTEXITCODE -ne 0) { throw 'Live draft PDF test failed.' }
}
finally { Pop-Location }
