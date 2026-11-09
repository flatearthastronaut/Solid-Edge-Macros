$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    & .\Build.cmd
    if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed.' }
    & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /r:System.Core.dll /r:System.Xml.Linq.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Microsoft.CSharp.dll /r:lib\Interop.SolidEdgeFramework.dll /out:tests\Tests.exe tests\Tests.cs tests\RibbonTarget.cs tests\ComponentDisplayTests.cs tests\IconResourceTests.cs Catalog.cs RibbonState.cs IconResources.cs Addin.cs ComponentDisplay.cs '..\..\Hammill Common\ComSupport.cs'
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
    Copy-Item -LiteralPath 'lib\Interop.SolidEdgeFramework.dll' -Destination 'tests\Interop.SolidEdgeFramework.dll' -Force
    & .\tests\Tests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Launcher tests failed.' }
} finally { Pop-Location }
