param([Parameter(Mandatory=$true)][string]$Project,[Parameter(Mandatory=$true)][string]$Name)
$ErrorActionPreference='Stop'
$projectPath=[IO.Path]::GetFullPath($Project)
$output=Join-Path $projectPath 'tests\bin'
[IO.Directory]::CreateDirectory($output)|Out-Null
$arguments=@('/nologo','/target:exe','/platform:x64','/main:Tests','/r:Microsoft.CSharp.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll',('/resource:'+(Join-Path $projectPath ('Assets\'+$Name+'.ico'))+',Macro.Icon'),('/out:'+(Join-Path $output 'Tests.exe')))
$arguments+=@(Get-ChildItem -LiteralPath $projectPath -Filter '*.cs' | ForEach-Object FullName)
$arguments+=@(Get-ChildItem -LiteralPath (Join-Path $projectPath 'tests') -Filter '*.cs' | ForEach-Object FullName)
$arguments+=@(Join-Path $PSScriptRoot 'ComSupport.cs';Join-Path $PSScriptRoot 'MacroHost.cs')
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') @arguments
if($LASTEXITCODE -ne 0){throw 'Test compilation failed.'}
& (Join-Path $output 'Tests.exe')
if($LASTEXITCODE -ne 0){throw 'Regression tests failed.'}
