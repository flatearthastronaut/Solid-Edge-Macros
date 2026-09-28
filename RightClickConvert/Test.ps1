$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
    & .\Build.cmd
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    & $compiler /nologo /target:exe /platform:x64 /r:Microsoft.CSharp.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /main:RegressionTests /out:tests\RegressionTests.exe tests\RegressionTests.cs Program.cs Conversion.cs SolidEdgeSession.cs ShellMenu.cs
    if ($LASTEXITCODE -ne 0) { throw 'Regression test compilation failed.' }
    & .\tests\RegressionTests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed.' }
}
finally { Pop-Location }
