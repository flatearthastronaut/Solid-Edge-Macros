$ErrorActionPreference = 'Stop'
$outputFolder = Join-Path $PSScriptRoot 'Compiled Executables\Hammill-Macro-Launcher-v0.3'
$names = @('Hammill.MacroLauncher.dll', 'Hammill.MacroLauncher.Resources.dll', 'Interop.SolidEdgeFramework.dll', 'Hammill Macro Catalog Check.exe', 'Register.cmd', 'UNRegister.cmd', 'README.md')
$files = @($names | ForEach-Object { Join-Path $outputFolder $_ })
foreach ($file in $files) { if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Package file missing: $file" } }
# Keep registration scripts and the diagnostic EXE together in one package folder;
# loose root EXEs would otherwise appear as separate macros in the manager's catalog.
Compress-Archive -LiteralPath $files -DestinationPath (Join-Path $PSScriptRoot 'Compiled Executables\Hammill-Macro-Launcher-v0.3.zip') -Force
