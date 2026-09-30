$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# Package the generated artwork into standard Windows icon sizes. Keep its
# alpha channel and use high-quality resampling for small Explorer/taskbar sizes.
$artPath = Join-Path $PSScriptRoot 'Assets\DualDimensionToggle.png'
$iconPath = Join-Path $PSScriptRoot 'Assets\DualDimensionToggle.ico'
$outputDir = Join-Path $PSScriptRoot 'Compiled Executables'
[IO.Directory]::CreateDirectory($outputDir) | Out-Null
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$frames = [Collections.Generic.List[byte[]]]::new()
$source = [Drawing.Image]::FromFile($artPath)
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
            $buffer = [IO.MemoryStream]::new()
            try {
                $bitmap.Save($buffer, [Drawing.Imaging.ImageFormat]::Png)
                $frames.Add($buffer.ToArray())
            }
            finally { $buffer.Dispose() }
            if ($size -eq 256) {
                $bitmap.Save((Join-Path $outputDir 'Dual Dimension Toggle.png'), [Drawing.Imaging.ImageFormat]::Png)
            }
        }
        finally { $bitmap.Dispose() }
    }
}
finally { $source.Dispose() }

# ICO directory entries reference PNG-compressed, 32-bit RGBA frames. The ICO
# format represents a 256-pixel width/height as zero in its one-byte fields.
$stream = [IO.File]::Create($iconPath)
$writer = [IO.BinaryWriter]::new($stream)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$i].Length)
        $writer.Write([uint32]$offset)
        $offset += $frames[$i].Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
}
finally { $writer.Dispose(); $stream.Dispose() }
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $outputDir 'Dual Dimension Toggle.ico') -Force
