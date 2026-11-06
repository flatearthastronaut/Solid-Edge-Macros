$ErrorActionPreference = 'Stop'
$project = [string][IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$repository = [string][IO.Path]::GetFullPath((Join-Path $project '..'))
$work = [string](Join-Path $PSScriptRoot ('.work\package-' + [guid]::NewGuid().ToString('N')))
$source = [string](Join-Path $work 'Source')
$local = [string](Join-Path $work 'Installed')
New-Item -ItemType Directory -Path $source,$local | Out-Null
$zip = Join-Path $project 'Compiled Executables\O-Ring Finder v1.0.zip'
Copy-Item -LiteralPath $zip -Destination $source

# Exercise the actual manager package installer in an isolated project-local
# destination. A null registrar ensures this macro test cannot register add-ins.
$manager = [Reflection.Assembly]::LoadFrom((Join-Path $repository 'Macro-Addin-Manager\Compiled Executables\SE Macro Manager v1.14.exe'))
$catalog = $manager.GetType('CoopManager.Catalog')
$packages = $catalog.GetMethod('Scan').Invoke($null,[object[]]@($source,$true))
if ($packages.Count -ne 1) { throw 'Expected one macro package.' }
$package = $packages[0]
$category = $manager.GetType('CoopManager.Categories').GetMethod('Detect').Invoke($null,[object[]]@($package))
if ($category -ne 'Solid Edge Macros') { throw ('Wrong category: ' + $category) }
[void]$manager.GetType('CoopManager.Installer').GetMethod('Install').Invoke($null,[object[]]@($package,$source,$local,$null))
$stable = Join-Path $local 'O-Ring Finder'
foreach ($name in @('O-Ring Finder.exe','Oring_table.txt','O-Ring Finder.png','README.md')) {
    if (-not (Test-Path -LiteralPath (Join-Path $stable $name))) { throw ('Missing installed runtime file: ' + $name) }
}
if ((Get-FileHash -LiteralPath (Join-Path $stable 'Oring_table.txt')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $project 'Oring_table.txt')).Hash) { throw 'Installed table differs from original.' }
$xml = New-Object Xml.XmlDocument
$xml.LoadXml('<settings><local /></settings>')
$xml.DocumentElement.SelectSingleNode('local').InnerText = $local
$settings = Join-Path $work 'settings.xml'
$xml.Save($settings)
$checker = Join-Path $repository 'Add-ins\Hammill-Macro-Launcher\Compiled Executables\Hammill-Macro-Launcher-v0.1\Hammill Macro Catalog Check.exe'
$output = & $checker --settings $settings --documents $work
if ($LASTEXITCODE -ne 0 -or -not ($output -match '^O-Ring Finder \|')) { throw ('Launcher discovery failed: ' + ($output -join "`n")) }
Write-Output 'PASS real Macro Manager install, stable EXE/table/icon, and Hammill ribbon discovery'
Write-Output $output
