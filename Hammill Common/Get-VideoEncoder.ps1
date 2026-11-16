$ErrorActionPreference='Stop'
$ProgressPreference='SilentlyContinue'
$folder=Join-Path $PSScriptRoot '.tools\ffmpeg'
$existing=Join-Path $folder 'bin\ffmpeg.exe'
if(Test-Path -LiteralPath $existing){return $existing}
$command=Get-Command ffmpeg.exe -ErrorAction SilentlyContinue
if($command){return $command.Source}
# Portable build dependency only; no installation or changes to PATH. The
# Windows binary distributor is linked by ffmpeg.org/download.html.
New-Item -ItemType Directory -Force -Path $folder | Out-Null
$zip=Join-Path $folder 'download.zip'
$url='https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip'
Write-Host 'Preparing portable FFmpeg for tooltip conversion (first use only)...'
Invoke-WebRequest -UseBasicParsing -Uri ($url+'.sha256') -OutFile ($zip+'.sha256')
Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $zip
$expected=([IO.File]::ReadAllText($zip+'.sha256').Trim() -split '\s+')[0]
if($expected -notmatch '^[a-fA-F0-9]{64}$' -or (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $expected){throw 'FFmpeg archive checksum mismatch.'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive=[IO.Compression.ZipFile]::OpenRead($zip)
try {
    foreach($name in @('ffmpeg.exe','ffprobe.exe')){
        $entry=@($archive.Entries | Where-Object {$_.FullName -like ('*/bin/'+$name)})
        if($entry.Count -ne 1){throw 'Unexpected FFmpeg archive layout.'}
        New-Item -ItemType Directory -Force -Path (Join-Path $folder 'bin') | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry[0],(Join-Path $folder ('bin\'+$name)),$true)
    }
} finally {$archive.Dispose()}
return $existing
