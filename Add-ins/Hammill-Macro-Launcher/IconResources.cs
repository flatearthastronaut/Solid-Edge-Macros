using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Hammill.MacroLauncher
{
    internal sealed class IconResources
    {
        internal const int StyleCycleImage = 10;
        internal const int LimitToleranceImage = 12;
        public string FileName;
        public readonly Dictionary<int, int> ResourceIds = new Dictionary<int, int>();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr BeginUpdateResource(string name, bool deleteExisting);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool UpdateResource(IntPtr update, string type, IntPtr name, ushort language, byte[] data, uint size);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool EndUpdateResource(IntPtr update, bool discard);

        public static IconResources Create(MacroCatalog catalog, string template, string cacheFolder, string signature)
        {
            Directory.CreateDirectory(cacheFolder);
            var result = new IconResources();
            int resource = 100;
            foreach (var entry in catalog.Entries)
            {
                if (resource > 65000) throw new InvalidDataException("Too many macros for a ribbon resource file.");
                result.ResourceIds.Add(entry.CommandId, resource); resource += 2;
            }
            result.FileName = Path.Combine(cacheFolder, "ribbon-" + signature + ".dll");
            if (File.Exists(result.FileName)) return result;
            string temporary = Path.Combine(cacheFolder, "building-" + Guid.NewGuid().ToString("N") + ".dll");
            File.Copy(template, temporary);
            IntPtr handle = IntPtr.Zero;
            try
            {
                // SE's SetAddInInfoEx2 takes a native resource filename. A private data-only
                // copy avoids modifying the loaded add-in or holding 32-bit module handles.
                handle = BeginUpdateResource(temporary, false);
                if (handle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
                using (var cycle = CreateStyleCycleIcon())
                {
                    WritePng(handle, StyleCycleImage, cycle, 32);
                    WritePng(handle, StyleCycleImage + 1, cycle, 40);
                }
                using (var limits = CreateCycleIcon(true))
                {
                    WritePng(handle, LimitToleranceImage, limits, 32);
                    WritePng(handle, LimitToleranceImage + 1, limits, 40);
                }
                foreach (var entry in catalog.Entries)
                {
                    using (var image = Load(entry, catalog.Notices))
                    {
                        // These slots select display DPI, not small versus large buttons.
                        // SDK Addins/VC/SEAddin.cpp supplies 32/40px for large buttons;
                        // AsmLoc.rc declares their native resource type as "PNG".
                        WritePng(handle, result.ResourceIds[entry.CommandId], image, 32);
                        WritePng(handle, result.ResourceIds[entry.CommandId] + 1, image, 40);
                    }
                }
                bool committed = EndUpdateResource(handle, false); handle = IntPtr.Zero;
                if (!committed) throw new Win32Exception(Marshal.GetLastWin32Error());
                try { File.Move(temporary, result.FileName); }
                catch (IOException) { if (!File.Exists(result.FileName)) throw; }
                return result;
            }
            finally
            {
                if (handle != IntPtr.Zero) EndUpdateResource(handle, true);
                if (File.Exists(temporary)) File.Delete(temporary); // only our uniquely named temporary file
            }
        }

        private static Image Load(MacroEntry entry, IList<string> notices)
        {
            try
            {
                if (entry.IconPath != null)
                {
                    using (var original = Image.FromFile(entry.IconPath)) return new Bitmap(original);
                }
                using (var icon = Icon.ExtractAssociatedIcon(entry.Path))
                    if (icon != null) return icon.ToBitmap();
            }
            catch (Exception error) { notices.Add(entry.Name + ": using a generic icon; " + error.Message); }
            return SystemIcons.Application.ToBitmap();
        }

        private static Bitmap CreateStyleCycleIcon()
        { return CreateCycleIcon(false); }

        private static Bitmap CreateCycleIcon(bool tolerance)
        {
            // The same code-drawn Hammill green artwork style as the standalone
            // tools, encoded with alpha like the other ribbon resources.
            var result = new Bitmap(128, 128, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(result))
            using (var green = new SolidBrush(Color.FromArgb(114, 148, 3)))
            using (var pen = new Pen(green, 9))
            using (var font = new Font("Segoe UI", tolerance ? 58 : 33, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                g.Clear(Color.Transparent); g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawString(tolerance ? "\u00b1" : ".000", font, green, tolerance ? 35 : 9, tolerance ? 23 : 39);
                g.DrawArc(pen, 16, 14, 96, 100, 205, 105);
                g.DrawArc(pen, 16, 14, 96, 100, 25, 105);
                g.FillPolygon(green, new[] { new Point(103, 15), new Point(106, 40), new Point(83, 29) });
                g.FillPolygon(green, new[] { new Point(25, 113), new Point(22, 88), new Point(45, 99) });
            }
            return result;
        }

        private static void WritePng(IntPtr handle, int id, Image original, int size)
        {
            using (var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    // Preserve coverage in the alpha channel throughout resampling.
                    // A color-keyed bitmap blends partial-alpha edges with the key
                    // color; Solid Edge removes only the exact key, leaving a halo.
                    graphics.Clear(Color.Transparent);
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    // Bilinear filtering avoids cubic overshoot at alpha boundaries,
                    // which can brighten a few edge pixels when coverage is clamped.
                    graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    double scale = Math.Min((double)size / original.Width, (double)size / original.Height);
                    int width = Math.Max(1, (int)Math.Round(original.Width * scale)), height = Math.Max(1, (int)Math.Round(original.Height * scale));
                    graphics.DrawImage(original, new Rectangle((size - width) / 2, (size - height) / 2, width, height));
                }
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    byte[] png = stream.ToArray();
                    // Store the complete PNG in a named native resource, as in the
                    // SDK sample. SE composites its alpha onto the active ribbon.
                    if (!UpdateResource(handle, "PNG", new IntPtr(id), 0, png, (uint)png.Length)) throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }
    }
}
