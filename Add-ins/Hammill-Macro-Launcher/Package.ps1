$ErrorActionPreference = 'Stop'
$outputFolder = Join-Path $PSScriptRoot 'Compiled Executables\Hammill-Macro-Launcher-v0.6'
$names = @('Hammill.MacroLauncher.dll', 'Hammill.MacroLauncher.Resources.dll', 'Interop.SolidEdgeFramework.dll', 'Hammill Macro Catalog Check.exe', 'Register.cmd', 'UNRegister.cmd', 'README.md', 'Shared Configuration')
$files = @($names | ForEach-Object { Join-Path $outputFolder $_ })
foreach ($file in $files) { if (-not (Test-Path -LiteralPath $file)) { throw "Package file missing: $file" } }
if (-not (Test-Path -LiteralPath (Join-Path $outputFolder 'Shared Configuration\Hammill.DimensionStyles.txt') -PathType Leaf)) { throw 'Shared configuration handoff file missing.' }
# Keep registration scripts and the diagnostic EXE together in one package folder;
# loose root EXEs would otherwise appear as separate macros in the manager's catalog.
Compress-Archive -LiteralPath $files -DestinationPath (Join-Path $PSScriptRoot 'Compiled Executables\Hammill-Macro-Launcher-v0.6.zip') -Force
