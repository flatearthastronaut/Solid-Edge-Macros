param([Parameter(Mandatory=$true)][string]$Project,[Parameter(Mandatory=$true)][string]$Name,[string]$Kind='sketch',[ValidatePattern('^\d+\.\d+(\.\d+){0,2}$')][string]$Version='1.0')
$ErrorActionPreference='Stop'
$projectPath=[IO.Path]::GetFullPath($Project)
$output=Join-Path $projectPath 'Compiled Executables'
$assets=Join-Path $projectPath 'Assets'
[IO.Directory]::CreateDirectory($output) | Out-Null
[IO.Directory]::CreateDirectory($assets) | Out-Null
& (Join-Path $PSScriptRoot 'Build-Icon.ps1') -Folder $assets -Name $Name -Kind $Kind
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$icon=Join-Path $assets ($Name+'.ico')
$exe=Join-Path $output ($Name+' v'+$Version+'.exe')
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
$tooltipFolder=Join-Path $projectPath 'Tooltips'
$stage=$null
try {
if(Test-Path -LiteralPath $tooltipFolder -PathType Container){
    $stage=Join-Path $projectPath ('.tooltip-build\'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage | Out-Null
    $prepared=Join-Path $stage 'Tooltips'
    & (Join-Path $PSScriptRoot 'Prepare-VideoTooltips.ps1') -Source $tooltipFolder -Destination $prepared
    $files+=$prepared
}
Compress-Archive -LiteralPath $files -DestinationPath (Join-Path $output ($Name+' v'+$Version+'.zip')) -Force
} finally {
    if($stage){
        $allowed=[IO.Path]::GetFullPath((Join-Path $projectPath '.tooltip-build'))+'\'
        if(-not [IO.Path]::GetFullPath($stage).StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid tooltip staging cleanup path.'}
        Remove-Item -LiteralPath $stage -Recurse -Force
    }
}
Write-Output ('Built '+$Name+' v'+$Version+'.')
