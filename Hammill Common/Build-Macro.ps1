param([Parameter(Mandatory=$true)][string]$Project,[Parameter(Mandatory=$true)][string]$Name,[string]$Kind='sketch')
$ErrorActionPreference='Stop'
$projectPath=[IO.Path]::GetFullPath($Project)
$output=Join-Path $projectPath 'Compiled Executables'
$assets=Join-Path $projectPath 'Assets'
[IO.Directory]::CreateDirectory($output) | Out-Null
[IO.Directory]::CreateDirectory($assets) | Out-Null
& (Join-Path $PSScriptRoot 'Build-Icon.ps1') -Folder $assets -Name $Name -Kind $Kind
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$icon=Join-Path $assets ($Name+'.ico')
$exe=Join-Path $output ($Name+' v1.0.exe')
$arguments=@('/nologo','/target:winexe','/platform:x64','/optimize+',('/win32icon:'+$icon),('/resource:'+$icon+',Macro.Icon'),'/r:Microsoft.CSharp.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll',('/out:'+$exe))
$arguments+=@(Get-ChildItem -LiteralPath $projectPath -Filter '*.cs' | ForEach-Object FullName)
$arguments+=@(Join-Path $PSScriptRoot 'ComSupport.cs'; Join-Path $PSScriptRoot 'MacroHost.cs')
& $compiler @arguments
if($LASTEXITCODE -ne 0){throw 'Macro compilation failed.'}
$files=@($exe)
foreach($file in @('README.md',('Assets\'+$Name+'.ico'),('Assets\'+$Name+'.png'))) {
    $source=Join-Path $projectPath $file
    Copy-Item -LiteralPath $source -Destination $output -Force
    $files+=Join-Path $output ([IO.Path]::GetFileName($source))
}
Compress-Archive -LiteralPath $files -DestinationPath (Join-Path $output ($Name+' v1.0.zip')) -Force
Write-Output ('Built '+$Name+' v1.0.')
