param([string]$Folder,[string]$Name,[string]$Kind)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$images=@()
foreach($size in @(16,32,48,256)) {
    $bitmap=New-Object Drawing.Bitmap($size,$size)
    $g=[Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.ScaleTransform(($size/128.0),($size/128.0)); $g.Clear([Drawing.Color]::Transparent)
    $green=New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(114,148,3))
    $dark=New-Object Drawing.Pen([Drawing.Color]::FromArgb(64,77,28),5)
    $gray=New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(214,221,225))
    $white=New-Object Drawing.SolidBrush([Drawing.Color]::White)
    try {
        if($Kind -eq 'viewer') {
            $g.FillRectangle($gray,20,25,58,58);$g.DrawRectangle($dark,20,25,58,58)
            $g.FillEllipse($white,42,58,76,43);$g.DrawEllipse($dark,42,58,76,43);$g.FillEllipse($green,66,62,31,34)
        } elseif($Kind -eq 'export') {
            $g.FillRectangle($gray,19,14,65,96);$g.DrawRectangle($dark,19,14,65,96)
            $g.FillRectangle($green,52,52,46,20)
            $g.FillPolygon($green,[Drawing.Point[]]@([Drawing.Point]::new(90,37),[Drawing.Point]::new(119,62),[Drawing.Point]::new(90,87)))
        } else {
            $g.FillPolygon($gray,[Drawing.Point[]]@([Drawing.Point]::new(15,34),[Drawing.Point]::new(89,18),[Drawing.Point]::new(112,91),[Drawing.Point]::new(38,108)))
            $g.DrawPolygon($dark,[Drawing.Point[]]@([Drawing.Point]::new(15,34),[Drawing.Point]::new(89,18),[Drawing.Point]::new(112,91),[Drawing.Point]::new(38,108)))
            $g.FillRectangle($green,54,39,18,51);$g.FillRectangle($green,38,55,51,18)
        }
        $stream=New-Object IO.MemoryStream; $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
        $images+=,$stream.ToArray();$stream.Dispose()
        if($size -eq 256){$bitmap.Save((Join-Path $Folder ($Name+'.png')),[Drawing.Imaging.ImageFormat]::Png)}
    } finally {$green.Dispose();$dark.Dispose();$gray.Dispose();$white.Dispose();$g.Dispose();$bitmap.Dispose()}
}
$writer=New-Object IO.BinaryWriter([IO.File]::Create((Join-Path $Folder ($Name+'.ico'))))
try {
    $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]4)
    $offset=70;$sizes=@(16,32,48,0)
    for($i=0;$i -lt 4;$i++){
        $writer.Write([byte]$sizes[$i]);$writer.Write([byte]$sizes[$i]);$writer.Write([byte]0);$writer.Write([byte]0)
        $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$images[$i].Length);$writer.Write([uint32]$offset);$offset+=$images[$i].Length
    }
    foreach($bytes in $images){$writer.Write([byte[]]$bytes)}
}finally{$writer.Dispose()}
