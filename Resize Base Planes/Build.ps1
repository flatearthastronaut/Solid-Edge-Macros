param([ValidateSet(1,2,3)][int]$IconChoice = 3)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $PSScriptRoot 'Compiled Executables'
$assets = Join-Path $PSScriptRoot 'Assets'
$work = Join-Path $PSScriptRoot 'work'
foreach ($directory in @($output,$assets,$work)) { [IO.Directory]::CreateDirectory($directory) | Out-Null }
& $compiler /nologo /r:System.Drawing.dll "/out:$work\BuildIcons.exe" (Join-Path $PSScriptRoot 'tools\BuildIcons.cs')
if ($LASTEXITCODE -ne 0) { throw 'Icon builder compilation failed.' }
& (Join-Path $work 'BuildIcons.exe') $assets
if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed.' }
$names = @('1-Three-Planes','2-Fit-Frame','3-XY-Size')
$icon = Join-Path $assets ($names[$IconChoice - 1] + '.ico')
$exe = Join-Path $output 'Resize Base Planes v1.1.exe'
$sources = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object FullName)
$sources += Join-Path $PSScriptRoot '..\Hammill Common\ComSupport.cs'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/win32icon:$icon" "/resource:$icon,Macro.Icon" /r:Microsoft.CSharp.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "/out:$exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Macro compilation failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Read Me.txt') -Destination $output -Force
foreach ($file in (Get-ChildItem -LiteralPath $assets -File)) { Copy-Item -LiteralPath $file.FullName -Destination $output -Force }
Write-Output ('Built ' + $exe + ' with icon ' + $IconChoice + '.')
