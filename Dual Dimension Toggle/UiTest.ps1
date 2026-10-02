$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
    & $compiler /nologo /target:exe /platform:x64 /main:UiTests /r:Microsoft.CSharp.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /resource:Assets\DualDimensionToggle.ico,DualDimensionToggle.ico /out:tests\UiTests.exe tests\UiTests.cs Program.cs DimensionPreview.cs StyleMap.cs Converter.cs FeatureFrameConverter.cs UnitText.cs CalloutConverter.cs CalloutHistory.cs CalloutFields.cs
    if ($LASTEXITCODE -ne 0) { throw 'Popup test compilation failed.' }
    & .\tests\UiTests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Popup tests failed.' }
}
finally { Pop-Location }
