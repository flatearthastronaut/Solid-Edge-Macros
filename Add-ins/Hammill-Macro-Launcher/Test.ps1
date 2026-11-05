$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    & .\Build.cmd
    if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed.' }
    & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /r:System.Core.dll /r:System.Xml.Linq.dll /r:System.Drawing.dll /out:tests\Tests.exe tests\Tests.cs Catalog.cs RibbonState.cs IconResources.cs
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
    & .\tests\Tests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Launcher tests failed.' }
} finally { Pop-Location }
