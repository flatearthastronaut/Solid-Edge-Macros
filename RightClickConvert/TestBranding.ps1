# Validate the actual deliverables without opening CAD or changing menus.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$source = Get-Content -LiteralPath "$PSScriptRoot\Program.cs" -Raw
$version = [regex]::Match($source, 'internal const string Version = "([^"]+)"').Groups[1].Value
$release = Join-Path $PSScriptRoot ("Compiled Executables\SolidEdgeConvert-v" + ([version]$version).ToString(2) + '.exe')
$stable = Join-Path $PSScriptRoot 'Compiled Executables\SolidEdgeConvert.exe'
if ((Get-FileHash -LiteralPath $release).Hash -ne (Get-FileHash -LiteralPath $stable).Hash) {
    throw 'Versioned and stable executable copies differ.'
}
$info = [Diagnostics.FileVersionInfo]::GetVersionInfo($release)
if ($info.FileVersion -ne $version -or $info.ProductVersion -ne $version -or $info.ProductName -ne 'Solid Edge Convert') {
    throw 'Windows version/product metadata does not match the release.'
}
if ([Reflection.AssemblyName]::GetAssemblyName($release).Version.ToString() -ne $version) {
    throw 'Assembly version does not match the release.'
}
# Windows must be able to extract the embedded icon without any adjacent assets.
# Verify the artwork's distinctive colors, not merely the presence of an icon
# (the compiler's default program icon would otherwise produce a false pass).
$icon = [Drawing.Icon]::ExtractAssociatedIcon($release)
if ($null -eq $icon) { throw 'Executable has no extractable icon.' }
try {
    $bitmap = $icon.ToBitmap()
    try {
        $orange = 0; $teal = 0
        for ($y = 0; $y -lt $bitmap.Height; $y++) {
            for ($x = 0; $x -lt $bitmap.Width; $x++) {
                $color = $bitmap.GetPixel($x, $y)
                if ($color.A -gt 128 -and $color.R -gt 220 -and $color.G -gt 120 -and $color.G -lt 220 -and $color.B -lt 120) { $orange++ }
                if ($color.A -gt 128 -and $color.R -lt 100 -and $color.G -gt 130 -and $color.B -gt 150) { $teal++ }
            }
        }
        if ($orange -lt 5 -or $teal -lt 5) { throw 'Embedded icon does not contain the conversion artwork.' }
    }
    finally { $bitmap.Dispose() }
}
finally { $icon.Dispose() }
$bytes = [IO.File]::ReadAllBytes("$PSScriptRoot\Assets\SolidEdgeConvert.ico")
if ([BitConverter]::ToUInt16($bytes, 4) -ne 6) { throw 'Missing icon resolutions.' }
$expected = @(16,24,32,48,128,0)
for ($i = 0; $i -lt $expected.Count; $i++) {
    if ($bytes[6 + 16 * $i] -ne $expected[$i] -or $bytes[7 + 16 * $i] -ne $expected[$i]) { throw 'Incorrect icon dimensions.' }
}
Write-Output "PASS version $version, matching release copies, and embedded custom icon with six resolutions."
