$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    # Read the same version used by the assembly attributes, avoiding a second
    # release number that could silently diverge from the executable's metadata.
    $source = Get-Content -LiteralPath 'Program.cs' -Raw
    $match = [regex]::Match($source, 'internal const string Version = "(\d+\.\d+\.\d+\.\d+)"')
    if (!$match.Success) { throw 'Program.cs is missing the release version.' }
    $version = ([version]$match.Groups[1].Value).ToString(2)
    $null = New-Item -ItemType Directory -Path 'Compiled Executables' -Force
    $release = "Compiled Executables\SolidEdgeConvert-v$version.exe"
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    & $compiler /nologo /target:winexe /platform:x64 /optimize+ /r:Microsoft.CSharp.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /win32icon:Assets\SolidEdgeConvert.ico "/out:$release" Program.cs Conversion.cs SolidEdgeSession.cs ShellMenu.cs BatchConversion.cs ShellSelection.cs ShellServer.cs
    if ($LASTEXITCODE -ne 0) { throw 'Release compilation failed.' }
    # Existing menu registrations point to this stable name. Keep a byte-identical
    # copy so an upgrade does not invalidate their registered executable path.
    Copy-Item -LiteralPath $release -Destination 'Compiled Executables\SolidEdgeConvert.exe' -Force
    Write-Output "Built $release and SolidEdgeConvert.exe"
}
finally { Pop-Location }
