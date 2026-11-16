param(
    [Parameter(Mandatory=$true)][string]$Project,
    [Parameter(Mandatory=$true)][string]$Executable,
    [string]$Icon='', [string]$Image='',
    [string]$DataFiles='', [string]$OptionalFiles=''
)
$ErrorActionPreference='Stop'
$projectPath=[IO.Path]::GetFullPath($Project)
$output=Join-Path $projectPath 'Compiled Executables'
$exe=Join-Path $projectPath $Executable
if(-not(Test-Path -LiteralPath $exe -PathType Leaf)){throw ('Compiled macro missing: '+$exe)}
$release=[IO.Path]::GetFileNameWithoutExtension($exe)
$name=[regex]::Replace($release,'[\s_.-]*[vV]?\d+(?:\.\d+){1,3}$','')
$stageRoot=[IO.Path]::GetFullPath((Join-Path $projectPath '.tooltip-build'))
$stage=Join-Path $stageRoot ([guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
try {
    # Explicit runtime allowlist: never collect old EXEs, logs, source files, or
    # unrelated output-folder artifacts into the new macro release.
    Copy-Item -LiteralPath $exe -Destination $stage
    $seen=@{};$seen[[IO.Path]::GetFileName($exe)]=$true
    foreach($pair in @(@($Icon,($name+'.ico')),@($Image,($name+'.png')))){
        if($pair[0]){
            Copy-Item -LiteralPath (Join-Path $projectPath $pair[0]) -Destination (Join-Path $stage $pair[1])
            $seen[$pair[1]]=$true
        }
    }
    $required=@($DataFiles -split '\|' | Where-Object {$_})
    $optional=@($OptionalFiles -split '\|' | Where-Object {$_})+@('README.md','Read Me.txt',($Executable+'.config'))
    foreach($relative in ($required+$optional)){
        $path=Join-Path $projectPath $relative
        if(-not(Test-Path -LiteralPath $path -PathType Leaf)){
            if($required -contains $relative){throw ('Required runtime file missing: '+$relative)}
            continue
        }
        $leaf=[IO.Path]::GetFileName($path)
        if($seen.ContainsKey($leaf)){throw ('Duplicate package filename: '+$leaf)}
        Copy-Item -LiteralPath $path -Destination $stage;$seen[$leaf]=$true
    }
    & (Join-Path $PSScriptRoot 'Prepare-VideoTooltips.ps1') -Source (Join-Path $projectPath 'Tooltips') -Destination (Join-Path $stage 'Tooltips')
    $temp=Join-Path $stage 'package.zip'
    $payload=@(Get-ChildItem -LiteralPath $stage | ForEach-Object FullName)
    Compress-Archive -LiteralPath $payload -DestinationPath $temp
    $target=Join-Path $output ($release+'.zip')
    # Conversion/ZIP failures leave the previous package untouched.
    if(Test-Path -LiteralPath $target){[IO.File]::Replace($temp,$target,[NullString]::Value)}else{[IO.File]::Move($temp,$target)}
    # Some older projects retain a second ZIP beside Build.cmd. Keep an already
    # existing current-release copy identical so Manager does not report two
    # conflicting builds. Do not create root copies or change older releases.
    $legacy=Join-Path $projectPath ($release+'.zip')
    if(Test-Path -LiteralPath $legacy -PathType Leaf){Copy-Item -LiteralPath $target -Destination $legacy -Force}
    Write-Output ('Packaged '+$target)
} finally {
    if(-not [IO.Path]::GetFullPath($stage).StartsWith($stageRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid package cleanup path.'}
    Remove-Item -LiteralPath $stage -Recurse -Force
}
