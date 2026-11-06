$ErrorActionPreference='Stop'
$root=[string][IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$work=[string](Join-Path $PSScriptRoot ('tests\.work\packages-'+[guid]::NewGuid().ToString('N')))
$source=[string](Join-Path $work 'Source');$local=[string](Join-Path $work 'Installed')
New-Item -ItemType Directory -Path $source,$local|Out-Null
foreach($name in @('O-Ring Finder','Add Sketch','Occurrence Viewer','Batch Approval Export')){
    Copy-Item -LiteralPath (Join-Path $root ($name+'\Compiled Executables\'+$name+' v1.0.zip')) -Destination $source
}
$manager=[Reflection.Assembly]::LoadFrom((Join-Path $root 'Macro-Addin-Manager\Compiled Executables\SE Macro Manager v1.14.exe'))
$catalog=$manager.GetType('CoopManager.Catalog');$detect=$manager.GetType('CoopManager.Categories').GetMethod('Detect')
$packages=$catalog.GetMethod('Scan').Invoke($null,[object[]]@($source,$true))
if($packages.Count -ne 4){throw 'Expected exactly four macro packages.'}
foreach($package in $packages){
    if($detect.Invoke($null,[object[]]@($package)) -ne 'Solid Edge Macros'){throw 'Wrong macro category.'}
    [void]$manager.GetType('CoopManager.Installer').GetMethod('Install').Invoke($null,[object[]]@($package,$source,$local,$null))
}
foreach($name in @('O-Ring Finder','Add Sketch','Occurrence Viewer','Batch Approval Export')){
    foreach($extension in @('.exe','.png','.ico')){
        if(-not(Test-Path -LiteralPath (Join-Path $local ($name+'\'+$name+$extension)))){throw ('Missing stable runtime file: '+$name+$extension)}
    }
}
if(-not(Test-Path -LiteralPath (Join-Path $local 'O-Ring Finder\Oring_table.txt'))){throw 'O-ring table missing.'}
$xml=New-Object Xml.XmlDocument;$xml.LoadXml('<settings><local /></settings>');$xml.DocumentElement.SelectSingleNode('local').InnerText=$local
$settings=Join-Path $work 'settings.xml';$xml.Save($settings)
$addinFolder=[string](Join-Path $root 'Add-ins\Hammill-Macro-Launcher\Compiled Executables')
$builds=$manager.GetType('CoopManager.BuildCatalog').GetMethod('Scan').Invoke($null,[object[]]@($addinFolder))
if($builds.Count -ne 1 -or $builds[0].Path -notlike '*v0.2.zip' -or $detect.Invoke($null,[object[]]@($builds[0])) -ne 'Solid Edge Add-ins'){throw 'Latest combined add-in is not one unambiguous add-in package.'}
$checker=Join-Path $addinFolder 'Hammill-Macro-Launcher-v0.2\Hammill Macro Catalog Check.exe'
$output=& $checker --settings $settings --documents $work
if($LASTEXITCODE -ne 0 -or @($output|Where-Object{$_ -match ' \| part,assembly,draft,sheetmetal \| '}).Count -ne 4){throw ('Launcher discovery failed: '+($output -join "`n"))}
Write-Output 'PASS one combined add-in package, four macro installs, stable EXEs/icons, external table, and four launcher commands'
Write-Output $output
