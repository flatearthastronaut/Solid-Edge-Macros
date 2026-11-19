param([string]$Package='Compiled Executables/Turbo Callouts v1.10.zip')
$ErrorActionPreference='Stop'
$projectRoot=$PSScriptRoot
$packagePath=Join-Path $projectRoot $Package
$testRoot=Join-Path $projectRoot ('Inspection/Deployment-'+[guid]::NewGuid().ToString('N'))
$extract=Join-Path $testRoot 'Package'
$isolated=Join-Path $testRoot 'Only Executable'
New-Item -ItemType Directory -Path $extract,$isolated | Out-Null
Expand-Archive -LiteralPath $packagePath -DestinationPath $extract
$executables=@(Get-ChildItem -LiteralPath $extract -File -Filter '*.exe')
if($executables.Count -ne 1){throw 'The release must contain exactly one executable.'}
$exe=$executables[0]
$original=Join-Path $projectRoot ('Compiled Executables/'+$exe.Name)
if((Get-FileHash -LiteralPath $exe.FullName).Hash -ne (Get-FileHash -LiteralPath $original).Hash){throw 'Packaged EXE differs from the compiled release.'}
Copy-Item -LiteralPath $exe.FullName -Destination $isolated
# Test the shipped EXE without copying README, artwork, source, or data alongside
# it. The separate harness reflects the production loader and note generation.
$runner=Join-Path $isolated 'DeploymentTests.exe'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /r:System.Core.dll "/out:$runner" (Join-Path $projectRoot 'tests/DeploymentTests.cs')
if($LASTEXITCODE -ne 0){throw 'Deployment harness did not compile.'}
Push-Location $isolated
try {
    & $runner $exe.Name
    if($LASTEXITCODE -ne 0){throw 'Isolated release checks failed.'}
} finally {Pop-Location}
Write-Output ('Verified package: '+$packagePath)
