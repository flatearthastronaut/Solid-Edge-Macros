$ErrorActionPreference = 'Stop'
$output = Join-Path $PSScriptRoot 'Compiled Executables'
# A developer may edit the runtime table too. Rebuilding code must not replace it.
$table = Join-Path $output 'Oring_table.txt'
if (-not (Test-Path -LiteralPath $table)) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Oring_table.txt') -Destination $table }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $output -Force
foreach ($name in @('O-Ring Finder.png','O-Ring Finder.ico')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('Assets\' + $name)) -Destination $output -Force
}
$names = @('O-Ring Finder v1.1.exe','Oring_table.txt','O-Ring Finder.png','O-Ring Finder.ico','README.md')
$files = @($names | ForEach-Object { Join-Path $output $_ })
$tooltipFolder=Join-Path $PSScriptRoot 'Tooltips'
$stage=$null
try {
if(Test-Path -LiteralPath $tooltipFolder -PathType Container){
    $stage=Join-Path $PSScriptRoot ('.tooltip-build\'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage | Out-Null
    $prepared=Join-Path $stage 'Tooltips'
    & (Join-Path $PSScriptRoot '..\Hammill Common\Prepare-VideoTooltips.ps1') -Source $tooltipFolder -Destination $prepared
    $files+=$prepared
}
Compress-Archive -LiteralPath $files -DestinationPath (Join-Path $output 'O-Ring Finder v1.1.zip') -Force
} finally {
    if($stage){
        $allowed=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '.tooltip-build'))+'\'
        if(-not [IO.Path]::GetFullPath($stage).StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid tooltip staging cleanup path.'}
        Remove-Item -LiteralPath $stage -Recurse -Force
    }
}
