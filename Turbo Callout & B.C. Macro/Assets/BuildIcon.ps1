$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# Package Greg's selected artwork without redrawing or changing its design.
# PNG-compressed RGBA frames keep transparency at every Windows icon size.
$sourcePath = Join-Path $PSScriptRoot 'Turbo Callout.png'
$iconPath = Join-Path $PSScriptRoot 'Turbo Callout.ico'
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$frames = [Collections.Generic.List[byte[]]]::new()
$source = [Drawing.Image]::FromFile($sourcePath)
try {
    foreach ($size in $sizes) {
        $bitmap = [Drawing.Bitmap]::new($size, $size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.DrawImage($source, [Drawing.Rectangle]::new(0, 0, $size, $size))
            }
            finally { $graphics.Dispose() }
            if ($bitmap.GetPixel(0, 0).A -ne 0) { throw "Transparent corner missing at $size pixels." }
            $buffer = [IO.MemoryStream]::new()
            try {
                $bitmap.Save($buffer, [Drawing.Imaging.ImageFormat]::Png)
                $frames.Add($buffer.ToArray())
            }
            finally { $buffer.Dispose() }
        }
        finally { $bitmap.Dispose() }
    }
}
finally { $source.Dispose() }

$writer = [IO.BinaryWriter]::new([IO.File]::Create($iconPath))
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        # ICO uses a zero byte to represent its 256-pixel frame.
        $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
        $offset += $frames[$i].Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
}
finally { $writer.Dispose() }
Write-Output ('Packaged Turbo Callout.ico at ' + ($sizes -join ', ') + ' pixels.')
