$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# Draw a top-view flange with six counterbores. Supersampling keeps the circular
# geometry smooth at small toolbar sizes. All GDI resources are disposed locally.
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
public static class BoltCircleArtwork
{
    public static Bitmap Draw(int size)
    {
        var image=new Bitmap(size,size);
        using(var g=Graphics.FromImage(image))
        using(var plate=new LinearGradientBrush(new Rectangle(0,0,256,256),Color.FromArgb(233,242,249),Color.FromArgb(126,154,178),70f))
        using(var dark=new SolidBrush(Color.FromArgb(20,43,64)))
        using(var rim=new Pen(Color.FromArgb(20,43,64),9))
        using(var light=new Pen(Color.FromArgb(245,250,254),3))
        using(var boreRim=new SolidBrush(Color.FromArgb(45,177,215)))
        using(var pitch=new Pen(Color.FromArgb(113,148,174),2))
        {
            g.SmoothingMode=SmoothingMode.AntiAlias;
            g.ScaleTransform(size/256f,size/256f);
            g.Clear(Color.Transparent);
            g.FillEllipse(plate,13,13,230,230);
            g.DrawEllipse(rim,13,13,230,230);
            g.DrawEllipse(light,21,21,214,214);
            pitch.DashStyle=DashStyle.Dash;
            g.DrawEllipse(pitch,52,52,152,152);
            // A central opening and six stepped recesses read as a bolt circle
            // without lettering, even when Windows displays the 16-pixel frame.
            g.FillEllipse(dark,92,92,72,72);
            g.DrawEllipse(light,90,90,76,76);
            for(int i=0;i<6;i++)
            {
                double angle=-Math.PI/2+i*Math.PI/3;
                float x=128+76*(float)Math.Cos(angle),y=128+76*(float)Math.Sin(angle);
                g.FillEllipse(dark,x-21,y-21,42,42);
                g.FillEllipse(boreRim,x-17,y-17,34,34);
                g.FillEllipse(dark,x-11,y-11,22,22);
            }
        }
        return image;
    }
}
'@

$sizes=@(16,24,32,48,64,128,256)
$frames=[Collections.Generic.List[byte[]]]::new()
$source=[BoltCircleArtwork]::Draw(1024)
try {
    foreach($size in $sizes) {
        $bitmap=[Drawing.Bitmap]::new($size,$size,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $g=[Drawing.Graphics]::FromImage($bitmap)
            try {
                $g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
                $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $g.DrawImage($source,[Drawing.Rectangle]::new(0,0,$size,$size))
            } finally {$g.Dispose()}
            $buffer=[IO.MemoryStream]::new()
            try {$bitmap.Save($buffer,[Drawing.Imaging.ImageFormat]::Png);$frames.Add($buffer.ToArray())}
            finally {$buffer.Dispose()}
            if($size -eq 256){$bitmap.Save((Join-Path $PSScriptRoot 'BoltCircleHoles.png'),[Drawing.Imaging.ImageFormat]::Png)}
        } finally {$bitmap.Dispose()}
    }
} finally {$source.Dispose()}

# Windows ICO directory: seven PNG-compressed 32-bit frames, preserving alpha.
$writer=[IO.BinaryWriter]::new([IO.File]::Create((Join-Path $PSScriptRoot 'BoltCircleHoles.ico')))
try {
    $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$sizes.Count)
    $offset=6+16*$sizes.Count
    for($i=0;$i -lt $sizes.Count;$i++) {
        $dimension=if($sizes[$i] -eq 256){0}else{$sizes[$i]}
        $writer.Write([byte]$dimension);$writer.Write([byte]$dimension)
        $writer.Write([byte]0);$writer.Write([byte]0)
        $writer.Write([uint16]1);$writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$i].Length);$writer.Write([uint32]$offset)
        $offset+=$frames[$i].Length
    }
    foreach($frame in $frames){$writer.Write([byte[]]$frame)}
} finally {$writer.Dispose()}
