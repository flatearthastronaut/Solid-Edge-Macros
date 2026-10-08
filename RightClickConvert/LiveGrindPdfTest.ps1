# Uses copies of an explicitly supplied sample; never saves that source draft.
param([string] $Sample = "$PSScriptRoot\tests\work\grind-inspect\GS sample.dft")
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
    & $compiler /nologo /target:exe /platform:x64 /r:Microsoft.CSharp.dll /main:LiveGrindPdfTest /out:tests\LiveGrindPdfTest.exe tests\LiveGrindPdfTest.cs tests\LivePdfSheetTest.cs Conversion.cs SolidEdgeSession.cs
    if ($LASTEXITCODE -ne 0) { throw 'Grind PDF test compilation failed.' }
    & .\tests\LiveGrindPdfTest.exe $Sample
    if ($LASTEXITCODE -ne 0) { throw 'Grind PDF test failed.' }
}
finally { Pop-Location }
