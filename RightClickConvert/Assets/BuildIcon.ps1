# Editable vector geometry rendered to six PNG-backed Windows icon sizes.
# No fonts, external artwork, packages, or machine-specific paths are required.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
public static class ConvertIconArtwork
{
    private static void Polygon(Graphics g, string color, params PointF[] points)
    {
        using (Brush brush = new SolidBrush(ColorTranslator.FromHtml(color))) g.FillPolygon(brush, points);
    }
    public static void Write(string folder)
    {
        int[] sizes = { 16, 24, 32, 48, 128, 256 };
        List<byte[]> images = new List<byte[]>();
        foreach (int size in sizes)
        {
            using (Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.ScaleTransform(size / 256f, size / 256f);
                using (GraphicsPath tile = new GraphicsPath())
                using (Brush navy = new SolidBrush(ColorTranslator.FromHtml("#132C43")))
                {
                    tile.AddArc(8, 8, 48, 48, 180, 90); tile.AddArc(200, 8, 48, 48, 270, 90);
                    tile.AddArc(200, 200, 48, 48, 0, 90); tile.AddArc(8, 200, 48, 48, 90, 90);
                    tile.CloseFigure(); g.FillPath(navy, tile);
                }
                // Three faces suggest a CAD solid. Broad shapes remain readable
                // in Explorer's 16px menus; opposing arrows identify conversion.
                Polygon(g, "#B3EFF4", new PointF(128,72),new PointF(192,105),new PointF(128,139),new PointF(64,105));
                Polygon(g, "#36BCCA", new PointF(64,111),new PointF(125,144),new PointF(125,202),new PointF(64,168));
                Polygon(g, "#2083AE", new PointF(131,144),new PointF(192,111),new PointF(192,168),new PointF(131,202));
                Polygon(g, "#FFB454", new PointF(49,37),new PointF(181,37),new PointF(181,24),new PointF(213,46),new PointF(181,68),new PointF(181,55),new PointF(49,55));
                Polygon(g, "#FFB454", new PointF(207,208),new PointF(75,208),new PointF(75,195),new PointF(43,217),new PointF(75,239),new PointF(75,226),new PointF(207,226));
                using (MemoryStream png = new MemoryStream())
                {
                    bitmap.Save(png, ImageFormat.Png); images.Add(png.ToArray());
                    if (size == 256) bitmap.Save(Path.Combine(folder, "SolidEdgeConvert.png"), ImageFormat.Png);
                }
            }
        }
        // ICO directory entries contain offsets to the PNG payloads. A zero
        // width/height byte represents 256 pixels in the Windows ICO format.
        using (BinaryWriter writer = new BinaryWriter(File.Create(Path.Combine(folder, "SolidEdgeConvert.ico"))))
        {
            writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)sizes.Length);
            int offset = 6 + sizes.Length * 16;
            for (int i = 0; i < sizes.Length; i++)
            {
                writer.Write((byte)(sizes[i] % 256)); writer.Write((byte)(sizes[i] % 256));
                writer.Write((byte)0); writer.Write((byte)0); writer.Write((ushort)1); writer.Write((ushort)32);
                writer.Write(images[i].Length); writer.Write(offset); offset += images[i].Length;
            }
            foreach (byte[] image in images) writer.Write(image);
        }
    }
}
'@
[ConvertIconArtwork]::Write($PSScriptRoot)
