$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$work=[string](Join-Path $PSScriptRoot ('tests\.work\video-'+[guid]::NewGuid().ToString('N')))
$project=Join-Path $work 'Demo'
$tips=Join-Path $project 'Tooltips'
New-Item -ItemType Directory -Path $tips | Out-Null
[IO.File]::WriteAllText((Join-Path $project 'Program.cs'),'class Program { static void Main() {} }')
[IO.File]::WriteAllText((Join-Path $project 'README.md'),'Isolated package fixture; never execute.')
$encoder=& (Join-Path $PSScriptRoot 'Get-VideoEncoder.ps1')
$wmv=Join-Path $tips 'Demo.wmv'
& $encoder -hide_banner -loglevel error -nostdin -y -i (Join-Path $root 'SDK_2026_2510_English\SDK\Advanced\samples\Addins\VC\tooltips\graphics\nonLocalized\tooltips\hammer.mp4') -t 1 -c:v wmv2 -an $wmv
if($LASTEXITCODE -ne 0){throw 'WMV fixture generation failed.'}
$originalHash=(Get-FileHash -LiteralPath $wmv).Hash
& (Join-Path $PSScriptRoot 'Build-Macro.ps1') -Project $project -Name Demo -Kind sketch
# The generic packager used by Turbo and the other previously EXE-only builds
# must preserve explicit dependencies and never scoop up obsolete executables.
[IO.File]::WriteAllText((Join-Path $project 'runtime.txt'),'required runtime data')
[IO.File]::WriteAllText((Join-Path $project 'Compiled Executables\Demo v0.9.exe'),'old release fixture')
[IO.File]::WriteAllText((Join-Path $project 'Demo v1.0.zip'),'legacy current ZIP fixture')
& (Join-Path $PSScriptRoot 'Package-CompiledMacro.ps1') -Project $project -Executable 'Compiled Executables\Demo v1.0.exe' -DataFiles runtime.txt -Icon 'Assets\Demo.ico' -Image 'Assets\Demo.png'
if((Get-FileHash -LiteralPath (Join-Path $project 'Demo v1.0.zip')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $project 'Compiled Executables\Demo v1.0.zip')).Hash){throw 'Legacy current ZIP differs from compiled-output package.'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive=[IO.Compression.ZipFile]::OpenRead((Join-Path $project 'Compiled Executables\Demo v1.0.zip'))
try {
    if(@($archive.Entries | Where-Object {$_.Name -like '*.exe'}).Count -ne 1 -or -not($archive.Entries.Name -contains 'runtime.txt')){throw 'Generic package lost a dependency or included an old executable.'}
} finally {$archive.Dispose()}
$source=[string](Join-Path $work 'Source');$local=[string](Join-Path $work 'Installed')
New-Item -ItemType Directory -Path $source,$local | Out-Null
Copy-Item -LiteralPath (Join-Path $project 'Compiled Executables\Demo v1.0.zip') -Destination $source
$manager=[Reflection.Assembly]::LoadFrom((Join-Path $root 'Macro-Addin-Manager\Compiled Executables\SE Macro Manager v1.14.exe'))
$packages=$manager.GetType('CoopManager.Catalog').GetMethod('Scan').Invoke($null,[object[]]@($source,$true))
[void]$manager.GetType('CoopManager.Installer').GetMethod('Install').Invoke($null,[object[]]@($packages[0],$source,$local,$null))
if((Get-FileHash -LiteralPath (Join-Path $local 'Demo\Tooltips\Demo.wmv')).Hash -ne $originalHash -or (Get-FileHash -LiteralPath $wmv).Hash -ne $originalHash){throw 'Original WMV changed.'}
if(Test-Path -LiteralPath (Join-Path $tips 'Demo.mp4')){throw 'Conversion must not write into source Tooltips.'}
$probe=Join-Path ([IO.Path]::GetDirectoryName($encoder)) 'ffprobe.exe'
$info=(& $probe -v error -select_streams v:0 -show_entries stream=codec_name,pix_fmt -of json (Join-Path $local 'Demo\Tooltips\Demo.mp4')) | ConvertFrom-Json
if($LASTEXITCODE -ne 0 -or $info.streams[0].codec_name -ne 'h264' -or $info.streams[0].pix_fmt -ne 'yuv420p'){throw 'Converted MP4 is not browser-compatible H.264.'}
$bytes=[IO.File]::ReadAllBytes((Join-Path $local 'Demo\Tooltips\Demo.mp4'))
$atoms=[Text.Encoding]::ASCII.GetString($bytes)
if($atoms.IndexOf('moov') -lt 0 -or $atoms.IndexOf('moov') -gt $atoms.IndexOf('mdat')){throw 'MP4 must place its playback index before video data.'}
$settings=[string](Join-Path $work 'settings.xml')
$xml=New-Object Xml.XmlDocument;$xml.LoadXml('<settings><local /></settings>');$xml.DocumentElement.SelectSingleNode('local').InnerText=$local;$xml.Save($settings)
$addin=[Reflection.Assembly]::LoadFrom((Join-Path $root 'Add-ins\Hammill-Macro-Launcher\Compiled Executables\Hammill-Macro-Launcher-v0.8\Hammill.MacroLauncher.dll'))
$catalog=$addin.GetType('Hammill.MacroLauncher.MacroCatalog').GetMethod('Scan').Invoke($null,[object[]]@($settings,$work))
if($catalog.Entries.Count -ne 1 -or $catalog.Entries[0].VideoPath -ne (Join-Path $local 'Demo\Tooltips\Demo.mp4')){throw 'Installed tooltip was not discovered.'}
$before=$catalog.GetType().GetMethod('RibbonSignature',[Reflection.BindingFlags]'Instance,NonPublic').Invoke($catalog,@())
Rename-Item -LiteralPath (Join-Path $local 'Demo\Tooltips\Demo.mp4') -NewName Demo.disabled
$updated=$addin.GetType('Hammill.MacroLauncher.MacroCatalog').GetMethod('Scan').Invoke($null,[object[]]@($settings,$work))
$after=$updated.GetType().GetMethod('RibbonSignature',[Reflection.BindingFlags]'Instance,NonPublic').Invoke($updated,@())
if($updated.Entries[0].VideoPath -notlike '*.wmv' -or $before -eq $after){throw 'Changing video selection must refresh ribbon registration.'}
$zip=Join-Path $project 'Compiled Executables\Demo v1.0.zip'
$beforeZip=(Get-FileHash -LiteralPath $zip).Hash
$cached=Join-Path $PSScriptRoot ('.tools\video-cache\h264-v1-'+$originalHash+'.mp4')
$cachedTime=(Get-Item -LiteralPath $cached).LastWriteTimeUtc
$repeat=Join-Path $work 'Repeat';New-Item -ItemType Directory -Path $repeat | Out-Null
& (Join-Path $PSScriptRoot 'Prepare-VideoTooltips.ps1') -Source $tips -Destination (Join-Path $repeat 'Tooltips')
if((Get-Item -LiteralPath $cached).LastWriteTimeUtc -ne $cachedTime){throw 'Unchanged recording was unnecessarily re-encoded.'}
[IO.File]::WriteAllText((Join-Path $tips 'Demo.mp4'),'explicit author MP4 fixture')
[IO.File]::WriteAllText($wmv,'invalid original must be ignored when explicit MP4 supplied')
$manual=Join-Path $work 'Manual';New-Item -ItemType Directory -Path $manual | Out-Null
& (Join-Path $PSScriptRoot 'Prepare-VideoTooltips.ps1') -Source $tips -Destination (Join-Path $manual 'Tooltips')
if([IO.File]::ReadAllText((Join-Path $manual 'Tooltips\Demo.mp4')) -ne 'explicit author MP4 fixture'){throw 'Explicit MP4 was overwritten.'}
Remove-Item -LiteralPath (Join-Path $tips 'Demo.mp4')
[IO.File]::WriteAllText($wmv,'invalid recording')
$failed=$false
try {& (Join-Path $PSScriptRoot 'Build-Macro.ps1') -Project $project -Name Demo -Kind sketch} catch {$failed=$true}
if(-not $failed -or (Get-FileHash -LiteralPath $zip).Hash -ne $beforeZip){throw 'Bad WMV must fail without replacing the previous package.'}
$failed=$false
try {& (Join-Path $PSScriptRoot 'Package-CompiledMacro.ps1') -Project $project -Executable 'Compiled Executables\Demo v1.0.exe'} catch {$failed=$true}
if(-not $failed -or (Get-FileHash -LiteralPath $zip).Hash -ne $beforeZip){throw 'Generic package must also preserve prior ZIP on conversion failure.'}
Write-Output 'PASS real WMV converts to H.264/yuv420p MP4, source stays unchanged, real manager installs both, launcher prefers MP4, and invalid recording preserves prior ZIP.'
