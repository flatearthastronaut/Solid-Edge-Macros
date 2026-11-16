$ErrorActionPreference='Stop'
$root=[string][IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$work=[string](Join-Path $PSScriptRoot ('tests\.work\packages-'+[guid]::NewGuid().ToString('N')))
$source=[string](Join-Path $work 'Source');$local=[string](Join-Path $work 'Installed')
New-Item -ItemType Directory -Path $source,$local|Out-Null
foreach($name in @('O-Ring Finder','Add Sketch','Occurrence Viewer','Batch Approval Export')){
    $version=if($name -eq 'Add Sketch'){'1.1'}else{'1.0'}
    Copy-Item -LiteralPath (Join-Path $root ($name+'\Compiled Executables\'+$name+' v'+$version+'.zip')) -Destination $source
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
$sketchExe=Join-Path $local 'Add Sketch\Add Sketch.exe'
if([Diagnostics.FileVersionInfo]::GetVersionInfo($sketchExe).FileVersion -ne '1.1.0.0'){throw 'Stable Add Sketch target must contain the v1.1 repair.'}
$sketchBuilds=$manager.GetType('CoopManager.BuildCatalog').GetMethod('Scan').Invoke($null,[object[]]@([string](Join-Path $root 'Add Sketch\Compiled Executables')))
if($sketchBuilds.Count -ne 1 -or $sketchBuilds[0].Path -notlike '*v1.1.zip'){throw 'Publisher must offer one latest Add Sketch v1.1 package.'}
$xml=New-Object Xml.XmlDocument;$xml.LoadXml('<settings><local /></settings>');$xml.DocumentElement.SelectSingleNode('local').InnerText=$local
$settings=Join-Path $work 'settings.xml';$xml.Save($settings)
$addinFolder=[string](Join-Path $root 'Add-ins\Hammill-Macro-Launcher\Compiled Executables')
$builds=$manager.GetType('CoopManager.BuildCatalog').GetMethod('Scan').Invoke($null,[object[]]@($addinFolder))
if($builds.Count -ne 1 -or $builds[0].Path -notlike '*v0.7.zip' -or $detect.Invoke($null,[object[]]@($builds[0])) -ne 'Solid Edge Add-ins'){throw 'Latest combined add-in is not one unambiguous add-in package.'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead($builds[0].Path)
try {
    $defaults=@($zip.Entries|Where-Object{$_.Name -eq 'Hammill.DimensionStyles.txt'})
    if($defaults.Count -ne 1 -or $defaults[0].FullName.Replace('\','/') -ne 'Shared Configuration/Hammill.DimensionStyles.txt'){throw 'The add-in must package one clearly identified shared configuration handoff file.'}
    if(@($zip.Entries|Where-Object{$_.Name -eq 'DimensionStyles.txt'}).Count -ne 0){throw 'Obsolete local starter filename must not be packaged.'}
    $reader=[IO.StreamReader]::new($defaults[0].Open())
    try {
        $content=$reader.ReadToEnd()
        foreach($family in @('[Inch]','[Inch-Metric]','[Vertical Inch]','[Vertical Inch-Metric]')) {if(-not $content.Contains($family)){throw ('Missing style family: '+$family)}}
        if($content -ne [IO.File]::ReadAllText((Join-Path $root 'Add-ins\Hammill-Macro-Launcher\DimensionStyleCycle\Hammill.DimensionStyles.txt'))){throw 'Packaged shared list differs from approved project list.'}
        [IO.File]::WriteAllText((Join-Path $source 'Hammill.DimensionStyles.txt'),$content)
    }
    finally {$reader.Dispose()}
} finally {$zip.Dispose()}
$checker=Join-Path $addinFolder 'Hammill-Macro-Launcher-v0.7\Hammill Macro Catalog Check.exe'
$output=& $checker --settings $settings --documents $work
if($LASTEXITCODE -ne 0 -or @($output|Where-Object{$_ -match ' \| part,assembly,draft,sheetmetal \| '}).Count -ne 4){throw ('Launcher discovery failed: '+($output -join "`n"))}
if($catalog.GetMethod('Scan').Invoke($null,[object[]]@($source,$true)).Count -ne 4){throw 'Loose shared style file must not appear as an installable macro package.'}
Write-Output 'PASS one combined add-in package, approved shared style handoff file, four macro installs, stable EXEs/icons, external table, and four launcher commands'
Write-Output $output
