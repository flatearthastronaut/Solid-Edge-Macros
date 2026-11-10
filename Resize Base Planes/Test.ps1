$ErrorActionPreference = 'Stop'
$bin = Join-Path $PSScriptRoot 'tests\bin'
[IO.Directory]::CreateDirectory($bin) | Out-Null
$sources = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object FullName)
$sources += Join-Path $PSScriptRoot '..\Hammill Common\ComSupport.cs'
$sources += Join-Path $PSScriptRoot 'tests\Tests.cs'
$icon = Join-Path $PSScriptRoot 'Assets\3-XY-Size.ico'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /main:Tests /r:Microsoft.CSharp.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "/resource:$icon,Macro.Icon" "/out:$bin\Tests.exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Regression test compilation failed.' }
& (Join-Path $bin 'Tests.exe') $PSScriptRoot
if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed.' }
