$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$assets = Join-Path $PSScriptRoot 'Assets'
[IO.Directory]::CreateDirectory($assets) | Out-Null
# Draw a geometric shaded ring in the approved Hammill green. Multi-size ICO
# entries keep the executable and ribbon legible at normal Windows icon sizes.
$images = @()
foreach ($size in @(16,32,48,256)) {
    $bitmap = New-Object Drawing.Bitmap($size,$size)
    $g = [Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.ScaleTransform(($size / 128.0),($size / 128.0))
    $g.Clear([Drawing.Color]::Transparent)
    $lower = New-Object Drawing.Drawing2D.GraphicsPath
    $top = New-Object Drawing.Drawing2D.GraphicsPath
    $dark = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(58,76,2))
    $gradient = New-Object Drawing.Drawing2D.LinearGradientBrush([Drawing.Point]::new(0,26),[Drawing.Point]::new(0,103),[Drawing.Color]::FromArgb(168,198,75),[Drawing.Color]::FromArgb(114,148,3))
    try {
        $lower.AddEllipse(13,34,102,78); $lower.AddEllipse(36,50,56,44)
        $g.FillPath($dark,$lower)
        $top.AddEllipse(13,25,102,78); $top.AddEllipse(36,42,56,44)
        $g.FillPath($gradient,$top)
        $stream = New-Object IO.MemoryStream
        $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
        $images += ,$stream.ToArray()
        $stream.Dispose()
        if ($size -eq 256) { $bitmap.Save((Join-Path $assets 'O-Ring Finder.png'),[Drawing.Imaging.ImageFormat]::Png) }
    } finally { $gradient.Dispose(); $dark.Dispose(); $top.Dispose(); $lower.Dispose(); $g.Dispose(); $bitmap.Dispose() }
}
$file = [IO.File]::Create((Join-Path $assets 'O-Ring Finder.ico'))
$writer = New-Object IO.BinaryWriter($file)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]4)
    $offset = 6 + 16 * 4
    $sizes = @(16,32,48,0)
    for ($i=0; $i -lt 4; $i++) {
        $writer.Write([byte]$sizes[$i]); $writer.Write([byte]$sizes[$i]); $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
        $offset += $images[$i].Length
    }
    foreach ($bytes in $images) { $writer.Write([byte[]]$bytes) }
} finally { $writer.Dispose() }
