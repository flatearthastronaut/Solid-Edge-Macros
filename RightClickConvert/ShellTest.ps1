# Run after the live fixture tests and TestMenu.ps1. Invokes the real multi-file
# context menus on copies of generated parts/drafts; does not touch user CAD data.
param([switch]$StepAssemblyOnly, [switch]$ParasolidExportOnly)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
    if ($StepAssemblyOnly -and $ParasolidExportOnly) { throw 'Choose only one targeted test.' }
    $part = Get-ChildItem -LiteralPath 'tests\work' -Recurse -File -Filter 'Cylinder*.par' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $draft = Get-ChildItem -LiteralPath 'tests\work' -Recurse -File -Filter 'Drawing*.dft' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $step = Get-ChildItem -LiteralPath 'tests\work' -Recurse -File -Filter 'Cylinder*.stp' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($StepAssemblyOnly) {
        $step = Get-ChildItem -LiteralPath 'tests\work' -Recurse -File -Filter 'Two cylinders.stp' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    }
    if ($null -eq $part -or (!$ParasolidExportOnly -and ($null -eq $draft -or $null -eq $step))) { throw 'Run the live fixture tests first.' }
    & $compiler /nologo /target:exe /platform:x64 /r:Microsoft.CSharp.dll /out:tests\ShellIntegrationTests.exe tests\ShellIntegrationTests.cs BatchConversion.cs ShellSelection.cs Conversion.cs SolidEdgeSession.cs
    if ($LASTEXITCODE -ne 0) { throw 'Shell integration test compilation failed.' }
    if ($ParasolidExportOnly) { & .\tests\ShellIntegrationTests.exe $part.FullName unused unused --parasolid-export-only }
    elseif ($StepAssemblyOnly) { & .\tests\ShellIntegrationTests.exe $part.FullName $draft.FullName $step.FullName --step-assembly-only }
    else { & .\tests\ShellIntegrationTests.exe $part.FullName $draft.FullName $step.FullName }
    if ($LASTEXITCODE -ne 0) { throw 'Shell integration test failed.' }
}
finally { Pop-Location }
