$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
    & $compiler /nologo /target:exe /platform:x64 /r:Microsoft.CSharp.dll /out:tests\LiveSmokeTests.exe tests\LiveSmokeTests.cs StyleMap.cs Converter.cs
    if ($LASTEXITCODE -ne 0) { throw 'Live test compilation failed.' }
    & .\tests\LiveSmokeTests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Live Solid Edge tests failed.' }
}
finally { Pop-Location }
