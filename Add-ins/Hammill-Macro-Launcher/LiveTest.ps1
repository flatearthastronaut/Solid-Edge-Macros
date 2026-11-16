$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
    & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /optimize+ /r:System.Core.dll /r:System.Xml.Linq.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Microsoft.CSharp.dll /r:lib\Interop.SolidEdgeFramework.dll /out:tests\LiveStyleCycle.exe tests\LiveStyleCycle.cs DimensionStyleCycle\StyleSequence.cs DimensionStyleCycle\StyleSequenceFile.cs DimensionStyleCycle\CycleEngine.cs DimensionStyleCycle\StyleIndex.cs Catalog.cs RibbonState.cs IconResources.cs Addin.cs ComponentDisplay.cs '..\..\Hammill Common\ComSupport.cs'
    if($LASTEXITCODE -ne 0){throw 'Live test compilation failed.'}
    Copy-Item -LiteralPath 'lib\Interop.SolidEdgeFramework.dll' -Destination 'tests\Interop.SolidEdgeFramework.dll' -Force
    & .\tests\LiveStyleCycle.exe
    if($LASTEXITCODE -ne 0){throw 'Live style cycle test failed.'}
} finally {Pop-Location}
