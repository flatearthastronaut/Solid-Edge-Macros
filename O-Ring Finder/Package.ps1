$ErrorActionPreference = 'Stop'
$output = Join-Path $PSScriptRoot 'Compiled Executables'
# A developer may edit the runtime table too. Rebuilding code must not replace it.
$table = Join-Path $output 'Oring_table.txt'
if (-not (Test-Path -LiteralPath $table)) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Oring_table.txt') -Destination $table }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $output -Force
foreach ($name in @('O-Ring Finder.png','O-Ring Finder.ico')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('Assets\' + $name)) -Destination $output -Force
}
$names = @('O-Ring Finder v1.0.exe','Oring_table.txt','O-Ring Finder.png','O-Ring Finder.ico','README.md')
$files = @($names | ForEach-Object { Join-Path $output $_ })
Compress-Archive -LiteralPath $files -DestinationPath (Join-Path $output 'O-Ring Finder v1.0.zip') -Force
