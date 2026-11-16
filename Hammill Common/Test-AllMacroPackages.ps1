$ErrorActionPreference='Stop'
$root=[string][IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$work=[string](Join-Path $PSScriptRoot ('tests\.work\ap-'+[guid]::NewGuid().ToString('N').Substring(0,8)))
$source=[string](Join-Path $work 'Source');$local=[string](Join-Path $work 'Installed')
New-Item -ItemType Directory -Path $source,$local | Out-Null
$manager=[Reflection.Assembly]::LoadFrom((Join-Path $root 'Macro-Addin-Manager\Compiled Executables\SE Macro Manager v1.14.exe'))
$folders=@('Turbo Callouts','Turbo Hole Table Callouts','Bolt Circle Holes','Dual Dimension Toggle','Fill Background Toggle','SketchToModels-v2.32','Populate SHCS','Resize Base Planes','RightClickConvert','Add Sketch','Occurrence Viewer','Batch Approval Export','O-Ring Finder')
foreach($folder in $folders){
    $output=[string](Join-Path $root ($folder+'\Compiled Executables'))
    $builds=$manager.GetType('CoopManager.BuildCatalog').GetMethod('Scan').Invoke($null,[object[]]@($output))
    if($builds.Count -ne 1 -or $builds[0].Kind -ne 'ZIP'){throw ('Expected one latest ZIP for '+$folder)}
    Copy-Item -LiteralPath $builds[0].Path -Destination $source
}
$packages=$manager.GetType('CoopManager.Catalog').GetMethod('Scan').Invoke($null,[object[]]@($source,$true))
if($packages.Count -ne $folders.Count){throw 'Some package families collided.'}
foreach($package in $packages){
    [void]$manager.GetType('CoopManager.Installer').GetMethod('Install').Invoke($null,[object[]]@($package,$source,$local,$null))
}
foreach($file in @('SketchToModels\template.txt',"BoltCircleHoles\C'bore Chart.xls",'O-Ring Finder\Oring_table.txt','Turbo Callouts\Turbo Callouts.png')){
    if(-not(Test-Path -LiteralPath (Join-Path $local $file))){throw ('Installed dependency missing: '+$file)}
}
$settings=[string](Join-Path $work 'settings.xml')
$xml=New-Object Xml.XmlDocument;$xml.LoadXml('<settings><local /></settings>');$xml.DocumentElement.SelectSingleNode('local').InnerText=$local;$xml.Save($settings)
$checker=Join-Path $root 'Add-ins\Hammill-Macro-Launcher\Compiled Executables\Hammill-Macro-Launcher-v0.8\Hammill Macro Catalog Check.exe'
$commands=& $checker --settings $settings --documents $work
if($LASTEXITCODE -ne 0){throw 'Launcher scan failed.'}
if(-not($commands -match '^Turbo Callouts \|') -or -not($commands -match '^Turbo Hole Table Callouts \|')){throw 'Turbo buttons were not discovered.'}
Write-Output ('PASS '+$packages.Count+' distinct latest ZIPs installed with runtime dependencies and discoverable Turbo commands.')
Write-Output $commands
