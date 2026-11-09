using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Hammill.MacroLauncher;

internal static class IconResourceTests
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern IntPtr LoadLibraryEx(string name, IntPtr file, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern IntPtr FindResource(IntPtr module, IntPtr name, string type);
    [DllImport("kernel32.dll")] static extern uint SizeofResource(IntPtr module, IntPtr resource);
    [DllImport("kernel32.dll")] static extern IntPtr LoadResource(IntPtr module, IntPtr resource);
    [DllImport("kernel32.dll")] static extern IntPtr LockResource(IntPtr resource);
    [DllImport("kernel32.dll")] static extern bool FreeLibrary(IntPtr module);

    // Read back the actual PE resource bytes, rather than testing an intermediate
    // bitmap. This catches the wrong Win32 resource type or lost alpha at encoding.
    static Bitmap ReadPng(IntPtr module, int id)
    {
        IntPtr resource = FindResource(module, new IntPtr(id), "PNG");
        if (resource == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Missing native PNG resource " + id);
        var bytes = new byte[checked((int)SizeofResource(module, resource))];
        IntPtr data = LockResource(LoadResource(module, resource));
        if (data == IntPtr.Zero) throw new InvalidDataException("Cannot read PNG resource " + id);
        Marshal.Copy(data, bytes, 0, bytes.Length);
        using (var stream = new MemoryStream(bytes))
        using (var image = Image.FromStream(stream))
        {
            if (image.RawFormat.Guid != ImageFormat.Png.Guid) throw new InvalidDataException("Resource is not a PNG.");
            return new Bitmap(image);
        }
    }

    public static void Run(IconResources resources, MacroCatalog catalog, Action<bool, string> check)
    {
        IntPtr module = LoadLibraryEx(resources.FileName, IntPtr.Zero, 2);
        check(module != IntPtr.Zero, "generated ribbon resource DLL loads as data");
        try
        {
            int id = resources.ResourceIds[catalog.Entries.Single(x => x.Key == "tool").CommandId];
            for (int slot = 0; slot < 2; slot++)
            using (var png = ReadPng(module, id + slot))
            {
                int size = slot == 0 ? 32 : 40;
                check(png.Width == size && png.Height == size, "native PNG supplies " + size + "px large artwork for its DPI slot");
                check(png.GetPixel(0, 0).A == 0 && png.GetPixel(size / 2, 0).A == 0 && png.GetPixel(size / 2, size / 2).A == 255,
                    size + "px artwork preserves transparent padding and opaque center");
                bool partialAlpha = false, cleanColor = true;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Color c = png.GetPixel(x, y);
                    if (c.A > 16 && c.A < 240) partialAlpha = true;
                    // Ignore barely covered pixels where 8-bit unpremultiplication
                    // amplifies rounding. All meaningful coverage must remain green.
                    if (c.A > 16 && (Math.Abs(c.R - 114) > 8 || Math.Abs(c.G - 148) > 8 || Math.Abs(c.B - 3) > 8)) cleanColor = false;
                }
                check(partialAlpha && cleanColor, size + "px antialiased edges retain alpha and Hammill green without magenta contamination");
            }
            int fallbackId = resources.ResourceIds[catalog.Entries.Single(x => x.Key == "other").CommandId];
            using (var fallback = ReadPng(module, fallbackId))
                check(fallback.Width == 32 && fallback.Height == 32, "executable/fallback artwork also encodes as a native PNG");
        }
        finally { if (module != IntPtr.Zero) FreeLibrary(module); }
    }

    public static void Preview(string template, string run)
    {
        string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
        string[] paths = { "Bolt Circle Holes/BoltCircleHoles.png", "Dual Dimension Toggle/Assets/DualDimensionToggle.png",
            "Fill Background Toggle/Assets/FillBackgroundToggle.png", "SketchToModels-v2.32/SketchToModels.png" };
        string[] names = { "Bolt Circle Holes", "Dual Dimensions", "Fill Background", "SketchToModels" };
        // Real companion artwork is optional in a checkout of just this add-in.
        if (paths.Any(p => !File.Exists(Path.Combine(root, p)))) return;
        var catalog = new MacroCatalog();
        for (int i = 0; i < paths.Length; i++)
            catalog.Entries.Add(new MacroEntry { CommandId = 100 + i, Name = names[i], IconPath = Path.Combine(root, paths[i]) });
        var resources = IconResources.Create(catalog, template, Path.Combine(run, "Preview"), "actual-artwork");
        IntPtr module = LoadLibraryEx(resources.FileName, IntPtr.Zero, 2);
        if (module == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            using (var preview = new Bitmap(720, 230))
            using (var g = Graphics.FromImage(preview))
            using (var font = new Font("Segoe UI", 10))
            {
                g.Clear(Color.White);
                g.FillRectangle(Brushes.WhiteSmoke, 0, 120, 720, 110);
                for (int i = 0; i < paths.Length; i++)
                {
                    g.DrawString(names[i], font, Brushes.Black, 12 + i * 180, 8);
                    for (int slot = 0; slot < 2; slot++)
                    using (var png = ReadPng(module, resources.ResourceIds[100 + i] + slot))
                    {
                        g.DrawImageUnscaled(png, 18 + i * 180, 45 + slot * 95);
                        g.DrawString(png.Width + "px", font, Brushes.DimGray, 70 + i * 180, 52 + slot * 95);
                    }
                }
                string path = Path.Combine(run, "Ribbon icons preview.png");
                preview.Save(path, ImageFormat.Png);
                Console.WriteLine("Actual resource preview: " + path);
            }
        }
        finally { FreeLibrary(module); }
    }
}
