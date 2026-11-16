param([Parameter(Mandatory=$true)][string]$Source,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
if(-not(Test-Path -LiteralPath $Source -PathType Container)){return}
if(Test-Path -LiteralPath $Destination){throw 'Tooltip staging destination must be new.'}
# Stage copies: source recordings and manually supplied MP4s are never edited.
Copy-Item -LiteralPath $Source -Destination $Destination -Recurse
$encoder=$null
foreach($wmv in Get-ChildItem -LiteralPath $Destination -Filter '*.wmv' -File -Recurse){
    $mp4=[IO.Path]::ChangeExtension($wmv.FullName,'.mp4')
    if(Test-Path -LiteralPath $mp4){continue} # Explicit MP4 supplied by the author wins.
    if(-not $encoder){$encoder=& (Join-Path $PSScriptRoot 'Get-VideoEncoder.ps1')}
    # Cache by input contents and conversion recipe, never by timestamps alone.
    $key=(Get-FileHash -LiteralPath $wmv.FullName -Algorithm SHA256).Hash
    $cache=Join-Path $PSScriptRoot ('.tools\video-cache\h264-v1-'+$key+'.mp4')
    if(-not(Test-Path -LiteralPath $cache)){
        New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($cache)) | Out-Null
        $temp=$cache+'.'+[guid]::NewGuid().ToString('N')+'.mp4'
        try {
            # Even dimensions, widely supported 8-bit H.264, no tooltip audio,
            # and a front-loaded MP4 index for immediate browser playback.
            & $encoder -hide_banner -loglevel error -nostdin -y -i $wmv.FullName -map 0:v:0 -an -vf 'pad=ceil(iw/2)*2:ceil(ih/2)*2' -c:v libx264 -pix_fmt yuv420p -preset fast -crf 20 -movflags +faststart $temp
            if($LASTEXITCODE -ne 0 -or -not(Test-Path -LiteralPath $temp)){throw ('Tooltip conversion failed: '+$wmv.Name)}
            & $encoder -hide_banner -loglevel error -nostdin -xerror -i $temp -map 0:v:0 -f null NUL
            if($LASTEXITCODE -ne 0){throw ('Converted tooltip failed playback validation: '+$wmv.Name)}
            # Publish only a complete, decoded file. If another build finished
            # this same immutable cache key first, use its completed conversion.
            try {[IO.File]::Move($temp,$cache)} catch [IO.IOException] {if(-not(Test-Path -LiteralPath $cache)){throw}}
        } finally {if(Test-Path -LiteralPath $temp){Remove-Item -LiteralPath $temp -Force}}
    }
    Copy-Item -LiteralPath $cache -Destination $mp4
    Write-Host ('Prepared tooltip MP4: '+$wmv.Name)
}
