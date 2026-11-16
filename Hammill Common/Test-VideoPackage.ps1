$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$work=[string](Join-Path $PSScriptRoot ('tests\.work\video-'+[guid]::NewGuid().ToString('N')))
$project=Join-Path $work 'Demo'
$tips=Join-Path $project 'Tooltips'
New-Item -ItemType Directory -Path $tips | Out-Null
[IO.File]::WriteAllText((Join-Path $project 'Program.cs'),'class Program { static void Main() {} }')
[IO.File]::WriteAllText((Join-Path $project 'README.md'),'Isolated package fixture; never execute.')
Copy-Item -LiteralPath (Join-Path $root 'SDK_2026_2510_English\SDK\Advanced\samples\Addins\VC\tooltips\graphics\nonLocalized\tooltips\hammer.mp4') -Destination (Join-Path $tips 'Demo.mp4')
[IO.File]::WriteAllText((Join-Path $tips 'Demo.wmv'),'WMV packaging fixture, not a playable recording.')
& (Join-Path $PSScriptRoot 'Build-Macro.ps1') -Project $project -Name Demo -Kind sketch
$source=[string](Join-Path $work 'Source');$local=[string](Join-Path $work 'Installed')
New-Item -ItemType Directory -Path $source,$local | Out-Null
Copy-Item -LiteralPath (Join-Path $project 'Compiled Executables\Demo v1.0.zip') -Destination $source
$manager=[Reflection.Assembly]::LoadFrom((Join-Path $root 'Macro-Addin-Manager\Compiled Executables\SE Macro Manager v1.14.exe'))
$packages=$manager.GetType('CoopManager.Catalog').GetMethod('Scan').Invoke($null,[object[]]@($source,$true))
[void]$manager.GetType('CoopManager.Installer').GetMethod('Install').Invoke($null,[object[]]@($packages[0],$source,$local,$null))
foreach($name in @('Demo.mp4','Demo.wmv')){
    if((Get-FileHash -LiteralPath (Join-Path $local ('Demo\Tooltips\'+$name))).Hash -ne (Get-FileHash -LiteralPath (Join-Path $tips $name)).Hash){throw 'Installed video changed.'}
}
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
Write-Output 'PASS package includes MP4/WMV; real manager retains Tooltips beside stable EXE; launcher discovers MP4 then WMV and refreshes ribbon.'
