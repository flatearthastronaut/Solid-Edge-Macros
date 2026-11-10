using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

// Repository-native vector artwork, following Hammill Common/Build-Icon.ps1.
// Draw each size from geometry, then package RGBA PNG frames into Windows ICO.
// No external artwork or imaging library is required; every GDI object is scoped.
internal static class BuildIcons
{
    private static readonly string[] Names = { "1-Three-Planes", "2-Fit-Frame", "3-XY-Size" };
    private static readonly string[] Labels = { "Three planes", "Fit frame", "X / Y size" };
    private static Color Ink = Color.FromArgb(34, 60, 79);

    public static void Main(string[] args)
    {
        string folder = args[0];
        Directory.CreateDirectory(folder);
        int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };
        for (int option = 0; option < 3; option++)
        {
            var frames = new List<byte[]>();
            foreach (int size in sizes)
            {
                using (Bitmap bitmap = Render(option, size))
                using (MemoryStream stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    frames.Add(stream.ToArray());
                    if (size == 256) bitmap.Save(Path.Combine(folder, Names[option] + ".png"), ImageFormat.Png);
                }
            }
            using (BinaryWriter writer = new BinaryWriter(File.Create(Path.Combine(folder, Names[option] + ".ico"))))
            {
                writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                    writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                    writer.Write((byte)0); writer.Write((byte)0);
                    writer.Write((ushort)1); writer.Write((ushort)32);
                    writer.Write(frames[i].Length); writer.Write(offset);
                    offset += frames[i].Length;
                }
                foreach (byte[] frame in frames) writer.Write(frame);
            }
        }

        using (Bitmap sheet = new Bitmap(840, 348))
        using (Graphics g = Graphics.FromImage(sheet))
        using (Font title = new Font("Segoe UI", 17, FontStyle.Bold))
        using (Font small = new Font("Segoe UI", 10))
        using (Brush ink = new SolidBrush(Ink))
        using (Brush muted = new SolidBrush(Color.FromArgb(97, 111, 123)))
        using (Pen border = new Pen(Color.FromArgb(220, 227, 232)))
        {
            g.Clear(Color.FromArgb(242, 246, 249));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = 0; i < 3; i++)
            {
                int left = 16 + i * 276;
                g.FillRectangle(Brushes.White, left, 16, 256, 316);
                g.DrawRectangle(border, left, 16, 256, 316);
                using (Bitmap art = Render(i, 144)) g.DrawImageUnscaled(art, left + 56, 35);
                g.DrawString((i + 1) + "  " + Labels[i], title, ink, left + 18, 197);
                g.DrawString("Toolbar sizes", small, muted, left + 18, 240);
                int x = left + 151;
                foreach (int size in new[] { 16, 24, 32 })
                {
                    using (Bitmap art = Render(i, size)) g.DrawImageUnscaled(art, x, 237 + (32 - size) / 2);
                    x += size + 7;
                }
                g.DrawString("Transparent PNG + Windows ICO", small, muted, left + 18, 290);
            }
            sheet.Save(Path.Combine(folder, "Icon-Choices.png"), ImageFormat.Png);
        }
    }

    private static Bitmap Render(int option, int size)
    {
        // Supersampling keeps diagonal lines clean even in the 16px frame.
        using (Bitmap canvas = new Bitmap(size * 3, size * 3, PixelFormat.Format32bppArgb))
        {
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.ScaleTransform(canvas.Width / 128f, canvas.Height / 128f);
                if (option == 0)
                {
                    Plane(g, Color.FromArgb(160, 72, 169, 222), new[] { P(12,64), P(64,36), P(116,64), P(64,92) });
                    Plane(g, Color.FromArgb(180, 39, 184, 158), new[] { P(38,18), P(90,48), P(90,110), P(38,80) });
                    Plane(g, Color.FromArgb(180, 250, 182, 65), new[] { P(90,18), P(38,48), P(38,110), P(90,80) });
                }
                else if (option == 1)
                {
                    using (Pen outer = new Pen(Color.FromArgb(141, 158, 169), 5))
                    {
                        outer.DashStyle = DashStyle.Dash;
                        g.DrawRectangle(outer, 16, 16, 96, 96);
                    }
                    using (Brush fill = new SolidBrush(Color.FromArgb(216, 229, 185)))
                    using (Pen outline = new Pen(Color.FromArgb(87, 111, 32), 5))
                    { g.FillRectangle(fill, 42, 42, 44, 44); g.DrawRectangle(outline, 42, 42, 44, 44); }
                    Arrow(g, P(12,12), P(37,37), Color.FromArgb(98, 130, 18), false);
                    Arrow(g, P(116,116), P(91,91), Color.FromArgb(98, 130, 18), false);
                }
                else
                {
                    using (Brush fill = new SolidBrush(Color.FromArgb(215, 237, 247)))
                    using (Pen grid = new Pen(Color.FromArgb(144, 197, 220), 2))
                    using (Pen outline = new Pen(Ink, 4))
                    {
                        g.FillRectangle(fill, 41, 41, 71, 71);
                        for(int line=1; line<3; line++)
                        { g.DrawLine(grid, 41 + line * 24, 41, 41 + line * 24, 112); g.DrawLine(grid, 41, 41 + line * 24, 112, 41 + line * 24); }
                        g.DrawRectangle(outline, 41, 41, 71, 71);
                    }
                    Arrow(g, P(38,21), P(115,21), Color.FromArgb(219, 126, 27), true);
                    Arrow(g, P(21,38), P(21,115), Color.FromArgb(34, 135, 174), true);
                }
            }
            Bitmap result = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(canvas, new Rectangle(0, 0, size, size));
            }
            return result;
        }
    }

    private static PointF P(float x, float y) { return new PointF(x, y); }
    private static void Plane(Graphics g, Color fill, PointF[] corners)
    {
        using (Brush brush = new SolidBrush(fill))
        using (Pen pen = new Pen(Ink, 3)) { g.FillPolygon(brush, corners); g.DrawPolygon(pen, corners); }
    }
    private static void Arrow(Graphics g, PointF a, PointF b, Color color, bool both)
    {
        using (Pen pen = new Pen(color, 6))
        using (AdjustableArrowCap cap = new AdjustableArrowCap(3.2f, 3.5f, true))
        {
            pen.CustomEndCap = cap;
            if (both) pen.CustomStartCap = cap;
            g.DrawLine(pen, a, b);
        }
    }
}
