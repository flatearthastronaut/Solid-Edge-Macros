# Compile and run the SDK lifetime tests and existing regression checks.
# These use simulated CAD objects and temporary Windows controls, not CAD files.
# Stop on any compiler/test failure; never report a partial run as successful.
$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testFolder=Join-Path $PSScriptRoot 'tests'
$macro=Join-Path $PSScriptRoot 'Compiled Executables\SketchToModels.exe'
Copy-Item -LiteralPath $macro -Destination (Join-Path $testFolder 'SketchToModels.exe') -Force
$names=@('ComSupportTests','PartColorTests','SaveHandoffTests','ManualValidationTests','AxisSetupTests','SurfaceProjectionTests','PlacementTests','BushingAxisTests','ComboExTest','ChainComboTest')
foreach($name in $names) {
    $source=Join-Path $testFolder ($name+'.cs')
    $executable=Join-Path $testFolder ($name+'.exe')
    & $compiler /nologo /platform:x64 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Microsoft.CSharp.dll "/r:$macro" "/out:$executable" $source
    if($LASTEXITCODE -ne 0){throw "Compilation failed: $name"}
    & $executable
    if($LASTEXITCODE -ne 0){throw "Test failed: $name"}
}
Write-Output 'All ten regression programs passed. Live Solid Edge modeling was not exercised.'


