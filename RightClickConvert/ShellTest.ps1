# Run after the live fixture tests and TestMenu.ps1. Invokes the real multi-file
# context menus on copies of generated parts/drafts; does not touch user CAD data.
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
    $part = Get-ChildItem -LiteralPath 'tests\work' -Recurse -File -Filter 'Cylinder*.par' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $draft = Get-ChildItem -LiteralPath 'tests\work' -Recurse -File -Filter 'Drawing*.dft' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($null -eq $part -or $null -eq $draft) { throw 'Run LiveTest.ps1 and LivePdfTest.ps1 to generate fixtures first.' }
    & $compiler /nologo /target:exe /platform:x64 /r:Microsoft.CSharp.dll /out:tests\ShellIntegrationTests.exe tests\ShellIntegrationTests.cs BatchConversion.cs ShellSelection.cs Conversion.cs
    if ($LASTEXITCODE -ne 0) { throw 'Shell integration test compilation failed.' }
    & .\tests\ShellIntegrationTests.exe $part.FullName $draft.FullName
    if ($LASTEXITCODE -ne 0) { throw 'Shell integration test failed.' }
}
finally { Pop-Location }
