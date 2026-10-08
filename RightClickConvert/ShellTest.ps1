# Run after the live fixture tests and TestMenu.ps1. Invokes the real multi-file
# context menus on copies of generated parts/drafts; does not touch user CAD data.
param([switch]$StepAssemblyOnly, [switch]$ParasolidExportOnly, [switch]$AssemblyExportOnly, [switch]$StlOnly, [switch]$PdfSheetsOnly, [switch]$PdfNoGrindOnly)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
    if (([int]$StepAssemblyOnly.IsPresent + [int]$ParasolidExportOnly.IsPresent + [int]$AssemblyExportOnly.IsPresent + [int]$StlOnly.IsPresent + [int]$PdfSheetsOnly.IsPresent + [int]$PdfNoGrindOnly.IsPresent) -gt 1) { throw 'Choose only one targeted test.' }
    $part = Get-ChildItem -LiteralPath 'tests\work' -Recurse -File -Filter 'Cylinder*.par' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $draft = Get-ChildItem -LiteralPath 'tests\work' -Recurse -File -Filter 'Drawing*.dft' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $step = Get-ChildItem -LiteralPath 'tests\work' -Recurse -File -Filter 'Cylinder*.stp' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($StepAssemblyOnly) {
        $step = Get-ChildItem -LiteralPath 'tests\work' -Recurse -File -Filter 'Two cylinders.stp' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    }
    if ($AssemblyExportOnly) {
        $step = Get-ChildItem -LiteralPath 'tests\work' -Recurse -File -Filter 'Two cylinders.asm' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    }
    if ($PdfNoGrindOnly) {
        $draft = Get-Item -LiteralPath 'tests\work\grind-inspect\GS sample.dft'
    }
    elseif ($PdfSheetsOnly) {
        $draft = Get-ChildItem -LiteralPath 'tests\work' -Recurse -File -Filter 'Drawing scopes.dft' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($null -eq $draft) { throw 'Run LivePdfSheetTest.ps1 first.' }
    }
    elseif ($AssemblyExportOnly) { if ($null -eq $step) { throw 'Generate the Two cylinders.asm test fixture first.' } }
    elseif ($null -eq $part -or (!$ParasolidExportOnly -and !$StlOnly -and ($null -eq $draft -or $null -eq $step))) { throw 'Run the live fixture tests first.' }
    & $compiler /nologo /target:exe /platform:x64 /r:Microsoft.CSharp.dll /main:ShellIntegrationTests /out:tests\ShellIntegrationTests.exe tests\ShellIntegrationTests.cs tests\LivePdfSheetTest.cs BatchConversion.cs ShellSelection.cs Conversion.cs SolidEdgeSession.cs
    if ($LASTEXITCODE -ne 0) { throw 'Shell integration test compilation failed.' }
    if ($PdfNoGrindOnly) { & .\tests\ShellIntegrationTests.exe unused $draft.FullName unused --pdf-no-grind-only }
    elseif ($PdfSheetsOnly) { & .\tests\ShellIntegrationTests.exe unused $draft.FullName unused --pdf-sheets-only }
    elseif ($StlOnly) { & .\tests\ShellIntegrationTests.exe $part.FullName unused unused --stl-only }
    elseif ($AssemblyExportOnly) { & .\tests\ShellIntegrationTests.exe unused unused $step.FullName --assembly-export-only }
    elseif ($ParasolidExportOnly) { & .\tests\ShellIntegrationTests.exe $part.FullName unused unused --parasolid-export-only }
    elseif ($StepAssemblyOnly) { & .\tests\ShellIntegrationTests.exe $part.FullName $draft.FullName $step.FullName --step-assembly-only }
    else { & .\tests\ShellIntegrationTests.exe $part.FullName $draft.FullName $step.FullName }
    if ($LASTEXITCODE -ne 0) { throw 'Shell integration test failed.' }
}
finally { Pop-Location }
