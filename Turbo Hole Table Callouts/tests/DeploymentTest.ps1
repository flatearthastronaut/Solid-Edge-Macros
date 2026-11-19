# Run with 64-bit Windows PowerShell and Solid Edge open. The fixture should be
# a saved draft with at least one recognized hole-table row.
param([Parameter(Mandatory=$true)][string]$Fixture)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$inspection = Join-Path $project 'Inspection'
$runId = [guid]::NewGuid().ToString('N')
$testFolder = Join-Path $inspection ('deployment-' + $runId)
$fixturePath = (Resolve-Path -LiteralPath $Fixture).Path
$release = 'Turbo Hole Table Callouts v0.9'
$zip = Join-Path $project ("Compiled Executables\$release.zip")
$exe = Join-Path $testFolder ($release + '.exe')
New-Item -ItemType Directory -Path $testFolder | Out-Null

# Install only the EXE from the distributed ZIP. Deliberately leave stock XML
# beside it to catch accidental current-directory configuration lookups.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    $entry = $archive.GetEntry($release + '.exe')
    [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $exe)
} finally { $archive.Dispose() }
if ((Get-FileHash -LiteralPath $exe).Hash -ne (Get-FileHash -LiteralPath (Join-Path $project "Compiled Executables\$release.exe")).Hash) {
    throw 'The ZIP does not contain the compiled executable.'
}
[IO.File]::WriteAllText((Join-Path $testFolder 'Custom.xml'), '<CustomSettings/>')

$app = [Runtime.InteropServices.Marshal]::GetActiveObject('SolidEdge.Application')
$docs = $app.Documents
$previous = $null
$copy = $null
try {
    if ($docs.Count -gt 0) { $previous = $app.ActiveDocument }
    $copyPath = Join-Path $inspection ("deployment-fixture-$runId.dft")
    Copy-Item -LiteralPath $fixturePath -Destination $copyPath
    $copy = $docs.Open($copyPath)
    $output = Join-Path $testFolder 'scan.xml'
    $scanArgs = '--scan "{0}" "{1}"' -f $copyPath, $output
    $process = Start-Process -FilePath $exe -ArgumentList $scanArgs -WorkingDirectory $testFolder -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) {
        if (Test-Path -LiteralPath ($output + '.error.txt')) { throw [IO.File]::ReadAllText($output + '.error.txt') }
        throw "Packaged scan exited with code $($process.ExitCode)."
    }
    [xml]$report = [IO.File]::ReadAllText($output)
    $scan = $report.HoleTableScan
    $recognized = @($scan.SelectNodes('Row[@slot > 0]')).Count
    if ($scan.settings -ne 'Packaged Turbo Hole Table Callouts hole presets' -or
        $scan.document -ne $copyPath -or [int]$scan.tables -lt 1 -or $recognized -lt 1) {
        throw 'The packaged EXE did not scan the fixture and recognize any rows using its own hole presets.'
    }
    Write-Output ("Packaged scan read {0} tables and {1} rows. Recognized {2}. Report saved to {3}" -f $scan.tables, $scan.SelectNodes('Row').Count, $recognized, $output)
} finally {
    try { if ($null -ne $copy) { $copy.Close($false) } }
    finally {
        try { if ($null -ne $previous) { $previous.Activate() } }
        finally {
            foreach ($item in @($copy, $previous, $docs, $app)) {
                if ($null -ne $item) { [void][Runtime.InteropServices.Marshal]::ReleaseComObject($item) }
            }
        }
    }
}
