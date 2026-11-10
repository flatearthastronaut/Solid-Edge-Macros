$ErrorActionPreference = 'Stop'
$bin = Join-Path $PSScriptRoot 'tests\bin'
[IO.Directory]::CreateDirectory($bin) | Out-Null
$sources = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object FullName)
$sources += Join-Path $PSScriptRoot '..\Hammill Common\ComSupport.cs'
$sources += Join-Path $PSScriptRoot 'tests\LiveTests.cs'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /main:LiveTests /r:Microsoft.CSharp.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "/out:$bin\LiveTests.exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Live test compilation failed.' }
& (Join-Path $bin 'LiveTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Live tests failed.' }
