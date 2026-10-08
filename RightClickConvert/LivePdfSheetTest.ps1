# Requires an open, idle Solid Edge session. Uses only generated test documents.
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
    & $compiler /nologo /target:exe /platform:x64 /r:Microsoft.CSharp.dll /out:tests\LivePdfSheetTest.exe tests\LivePdfSheetTest.cs Conversion.cs SolidEdgeSession.cs
    if ($LASTEXITCODE -ne 0) { throw 'PDF sheet test compilation failed.' }
    & .\tests\LivePdfSheetTest.exe
    if ($LASTEXITCODE -ne 0) { throw 'PDF sheet test failed.' }
}
finally { Pop-Location }
