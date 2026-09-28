# Optional: requires an open, idle Solid Edge session. Creates its own test part.
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$partInterop = Join-Path $env:ProgramFiles 'Siemens\Solid Edge 2026\Program\interop.SolidEdgePartLib.dll'
Push-Location $PSScriptRoot
try {
    & $compiler /nologo /target:exe /platform:x64 /r:Microsoft.CSharp.dll "/link:$partInterop" /out:tests\LiveSmokeTest.exe tests\LiveSmokeTest.cs Conversion.cs SolidEdgeSession.cs
    if ($LASTEXITCODE -ne 0) { throw 'Live test compilation failed.' }
    & .\tests\LiveSmokeTest.exe
    if ($LASTEXITCODE -ne 0) { throw 'Live Solid Edge test failed.' }
}
finally { Pop-Location }
