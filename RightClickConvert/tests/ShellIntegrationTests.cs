using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SolidEdgeConvert;

// Exercises the actual Explorer context-menu selection path, including COM
// activation of the release executable. Uses copies of generated test fixtures.
internal static class ShellIntegrationTests
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 4 && args[3] == "--assembly-export-only")
            {
                Exercise(args[2], ".stp", "STEP (.stp)", false, null, true);
                Exercise(args[2], ".x_t", "Parasolid (.x_t)", false, null, true);
                Console.WriteLine("PASS assembly exports: six STEP/Parasolid outputs reimported with two component occurrences and geometry; assembly hashes unchanged.");
                return 0;
            }
            if (args.Length == 4 && args[3] == "--parasolid-export-only")
            {
                Exercise(args[0], ".x_t", "Parasolid (.x_t)");
                Console.WriteLine("PASS Parasolid export selection: three exports reopened with geometry; source hashes unchanged.");
                return 0;
            }
            if (args.Length == 4 && args[3] == "--step-assembly-only")
            {
                Exercise(args[2], ".asm", "Solid Edge Assembly (.asm)");
                Console.WriteLine("PASS STEP assembly selection: both extensions, three assemblies, component geometry/links, and source hashes.");
                return 0;
            }
            if (args.Length != 3) throw new ArgumentException("Pass generated .par, .dft, and .stp fixture paths.");
            Exercise(args[0], ".stp", "STEP (.stp)");
            Exercise(args[0], ".x_t", "Parasolid (.x_t)");
            Exercise(args[1], ".pdf", "PDF (.pdf)");
            Exercise(args[1], ".pdf", "PDF with Date", true);
            string[] imports = CreateImportFixtures(args[0]);
            // STEP can encode even a single exported part inside assembly
            // structure. Solid Edge may request a flattening confirmation here.
            // Use --step-assembly-only to verify assembly import unattended.
            Exercise(args[2], ".par", "Solid Edge Part (.par)");
            Exercise(imports[2], ".asm", "Solid Edge Assembly (.asm)");
            Exercise(imports[0], ".par", "Solid Edge Part (.par)", false, imports[1]);
            Exercise(imports[0], ".asm", "Solid Edge Assembly (.asm)", false, imports[1]);
            Console.WriteLine("PASS actual Shell multi-selection menus: all eight conversions; all twenty-four source hashes preserved.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void Exercise(string fixture, string outputExtension, string label, bool dated = false, string binaryFixture = null, bool assemblyExport = false)
    {
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work", "selection-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string[] paths = new string[3];
        string[] hashes = new string[3];
        for (int i = 0; i < paths.Length; i++)
        {
            // Include both STEP spellings in the same actual Explorer selection.
            string input = binaryFixture != null && i == 1 ? binaryFixture : fixture;
            string extension = binaryFixture != null ? Path.GetExtension(input).ToUpperInvariant()
                : Path.GetExtension(fixture).Equals(".stp", StringComparison.OrdinalIgnoreCase)
                    ? (i == 1 ? ".STEP" : ".stp") : Path.GetExtension(fixture);
            paths[i] = Path.Combine(folder, "Selected " + i + " & é 100%" + extension);
            File.Copy(input, paths[i]); hashes[i] = Hash(paths[i]);
        }
        Console.WriteLine("Invoking " + label + " on " + paths.Length + " files through IContextMenu.");
        string dateSuffix = dated ? " " + DateTime.Today.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture) : "";
        Func<string, string> expectedOutput = path => Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + dateSuffix + outputExtension);
        InvokeMenu(paths, label);
        Stopwatch elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed.TotalSeconds < 90)
        {
            bool complete = true;
            foreach (string path in paths) complete &= File.Exists(expectedOutput(path));
            if (complete) break;
            Thread.Sleep(200);
        }
        foreach (string path in paths)
        {
            string output = expectedOutput(path);
            if (!File.Exists(output)) throw new Exception("Shell selection did not convert " + path);
            if (assemblyExport) { CheckAssemblyExport(output); continue; }
            if (outputExtension == ".par" || outputExtension == ".asm" || outputExtension == ".x_t") { CheckNativePart(output, outputExtension == ".asm"); continue; }
            string text = Encoding.ASCII.GetString(File.ReadAllBytes(output));
            if (outputExtension == ".pdf" ? !text.StartsWith("%PDF-") || !text.Contains("%%EOF") : !text.StartsWith("ISO-10303-21;") || !text.Contains("END-ISO-10303-21;"))
                throw new Exception("Incomplete export: " + output);
        }
        for (int i = 0; i < paths.Length; i++) if (hashes[i] != Hash(paths[i])) throw new Exception("Source changed: " + paths[i]);
        // Close only a completed results window from our converter process;
        // leave unrelated applications and any still-busy conversion alone.
        bool closed = false;
        for (int retry = 0; retry < 30 && !closed; retry++)
        {
            EnumWindows(delegate(IntPtr window, IntPtr unused)
            {
                uint pid; GetWindowThreadProcessId(window, out pid);
                Process process;
                try { process = Process.GetProcessById((int)pid); }
                catch (ArgumentException) { return true; }
                using (process)
                {
                    if (process.ProcessName != "SolidEdgeConvert") return true;
                    StringBuilder title = new StringBuilder(256); GetWindowText(window, title, title.Capacity);
                    if (title.ToString() != "Solid Edge Convert") return true;
                    // WinForms class suffix changes by runtime; inspect child
                    // captions instead of relying on a specific generated name.
                    bool summary = false;
                    EnumChildWindows(window, delegate(IntPtr child, IntPtr ignored)
                    {
                        StringBuilder caption = new StringBuilder(512); GetWindowText(child, caption, caption.Capacity);
                        if (caption.ToString().Contains("3 converted, 0 skipped, 0 failed.")) summary = true;
                        return true;
                    }, IntPtr.Zero);
                    if (summary) { PostMessage(window, 0x0010, IntPtr.Zero, IntPtr.Zero); closed = true; }
                }
                return true;
            }, IntPtr.Zero);
            if (!closed) Thread.Sleep(100);
        }
        if (!closed) throw new Exception("Expected one summary window for the full selection.");
        Console.WriteLine("PASS " + label + ": all three outputs and one batch summary.");
        Thread.Sleep(200);
    }

    private static void InvokeMenu(string[] paths, string caption)
    {
        IShellItemArray selection = ShellSelection.Create(paths);
        IContextMenu context = null;
        IntPtr menu = CreatePopupMenu();
        IntPtr pointer = IntPtr.Zero;
        try
        {
            Guid handler = new Guid("3981E225-F559-11D3-8E3A-00C04F6837D5"); // BHID_SFUIObject
            Guid iid = typeof(IContextMenu).GUID;
            selection.BindToHandler(IntPtr.Zero, ref handler, ref iid, out pointer);
            context = (IContextMenu)Marshal.GetObjectForIUnknown(pointer);
            Marshal.ThrowExceptionForHR(context.QueryContextMenu(menu, 0, 1, 0x7FFF, 0));
            uint id = FindCommand(menu, caption, context);
            if (id == UInt32.MaxValue) throw new Exception("Convert > " + caption + " is missing for multiple files.");
            CommandInfo info = new CommandInfo { Size = Marshal.SizeOf(typeof(CommandInfo)), Verb = new IntPtr(id - 1), Show = 1 };
            context.InvokeCommand(ref info);
        }
        finally
        {
            if (context != null) Marshal.ReleaseComObject(context);
            if (pointer != IntPtr.Zero) Marshal.Release(pointer);
            DestroyMenu(menu);
            Marshal.ReleaseComObject(selection);
        }
    }
    private static uint FindCommand(IntPtr menu, string caption, IContextMenu context)
    {
        for (int i = 0; i < GetMenuItemCount(menu); i++)
        {
            StringBuilder text = new StringBuilder(256);
            GetMenuString(menu, (uint)i, text, text.Capacity, 0x400);
            if (text.ToString().Replace("&", "") == caption) return GetMenuItemID(menu, i);
            IntPtr child = GetSubMenu(menu, i);
            if (child != IntPtr.Zero)
            {
                // Explorer sends this before opening a cascading menu; some
                // handlers defer generating subcommands until that message.
                IContextMenu2 lazy = context as IContextMenu2;
                if (lazy != null) lazy.HandleMenuMsg(0x0117, child, new IntPtr(i));
                uint found = FindCommand(child, caption, context);
                if (found != UInt32.MaxValue) return found;
            }
        }
        return UInt32.MaxValue;
    }
    private static string Hash(string path)
    {
        using (SHA256 hash = SHA256.Create())
        using (FileStream stream = File.OpenRead(path)) return Convert.ToBase64String(hash.ComputeHash(stream));
    }
    private static void CheckNativePart(string path, bool assembly = false)
    {
        using (OleMessageFilter filter = new OleMessageFilter())
        {
            object app = null, documents = null, part = null, models = null;
            try
            {
                app = Marshal.GetActiveObject("SolidEdge.Application");
                documents = ((dynamic)app).Documents;
                int before = ((dynamic)documents).Count;
                object adapterBefore = null;
                ((dynamic)app).GetGlobalParameter(458, ref adapterBefore);
                // Validate the exported Parasolid by importing into an explicit
                // part template; do not invoke the STEP assembly-flattening path.
                part = path.EndsWith(".x_t", StringComparison.OrdinalIgnoreCase)
                    ? ((dynamic)documents).OpenWithTemplate(path, "normal.par")
                    : ((dynamic)documents).Open(path);
                if ((int)((dynamic)part).Type != (assembly ? 3 : 1)) throw new Exception("Output is not the requested native type: " + path);
                if (assembly)
                {
                    models = ((dynamic)part).Occurrences;
                    if ((int)((dynamic)models).Count < 2) throw new Exception("Assembly lost its components: " + path);
                    for (int i = 1; i <= (int)((dynamic)models).Count; i++)
                    {
                        object occurrence = null, child = null, childModels = null;
                        try
                        {
                            occurrence = ((dynamic)models).Item(i);
                            string file = ((dynamic)occurrence).OccurrenceFileName;
                            if (!File.Exists(file)) throw new Exception("Missing assembly component: " + file);
                            child = ((dynamic)occurrence).OccurrenceDocument;
                            childModels = ((dynamic)child).Models;
                            if ((int)((dynamic)childModels).Count < 1) throw new Exception("Empty assembly component: " + file);
                        }
                        finally { ComLifetime.Release(ref childModels); ComLifetime.Release(ref child); ComLifetime.Release(ref occurrence); }
                    }
                }
                else
                {
                    models = ((dynamic)part).Models;
                    if ((int)((dynamic)models).Count < 1) throw new Exception("Imported part has no model: " + path);
                }
                ComLifetime.Release(ref models);
                ((dynamic)part).Close(false);
                ComLifetime.Release(ref part);
                ((dynamic)app).DoIdle();
                if ((int)((dynamic)documents).Count != before) throw new Exception("Native validation left a document open.");
                object adapterAfter = null;
                ((dynamic)app).GetGlobalParameter(458, ref adapterAfter);
                if (!Object.Equals(adapterBefore, adapterAfter)) throw new Exception("Native validation changed STEP settings.");
            }
            finally
            {
                ComLifetime.Release(ref models);
                try { if (part != null) ((dynamic)part).Close(false); }
                finally { ComLifetime.Release(ref part); ComLifetime.Release(ref documents); ComLifetime.Release(ref app); }
            }
        }
    }
    private static void CheckAssemblyExport(string output)
    {
        // Reimport a copy in an isolated test folder. Always select normal.asm
        // through the assembly path; never trigger assembly-to-Part prompts.
        // Keep the verification path short: assembly import adds a component
        // folder and staged filename, and native SaveAs has legacy path limits.
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work", "verify-" + Guid.NewGuid().ToString("N").Substring(0, 12));
        Directory.CreateDirectory(folder);
        string copy = Path.Combine(folder, "model" + Path.GetExtension(output));
        File.Copy(output, copy);
        string native;
        using (OleMessageFilter filter = new OleMessageFilter())
        {
            ConversionFormat format = Path.GetExtension(output) == ".stp" ? ConversionFormat.StepAssembly : ConversionFormat.ParasolidAssembly;
            native = Conversion.Run(copy, false, delegate { return new SolidEdgeSession(); }, delegate { }, format);
        }
        // CheckNativePart owns its own message filter; do not nest COM filters.
        CheckNativePart(native, true);
    }
    private static string[] CreateImportFixtures(string cylinder)
    {
        using (OleMessageFilter filter = new OleMessageFilter())
        {
            object app = null, docs = null, assembly = null, occurrences = null, occurrence = null;
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work", "imports-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string[] paths = { Path.Combine(folder, "Two cylinders.x_t"), Path.Combine(folder, "Two cylinders.x_b"), Path.Combine(folder, "Two cylinders.stp") };
            try
            {
                app = Marshal.GetActiveObject("SolidEdge.Application");
                docs = ((dynamic)app).Documents;
                assembly = ((dynamic)docs).Add("SolidEdge.AssemblyDocument");
                occurrences = ((dynamic)assembly).Occurrences;
                for (int i = 0; i < 2; i++)
                {
                    occurrence = ((dynamic)occurrences).AddByFilename(cylinder);
                    if (i == 1) ((dynamic)occurrence).Move(0.05, 0.0, 0.0);
                    ComLifetime.Release(ref occurrence);
                }
                ((dynamic)assembly).SaveAs(Path.Combine(folder, "Two cylinders.asm"));
                ((dynamic)assembly).SaveAs(paths[0]);
                ((dynamic)assembly).SaveAs(paths[1]);
                object previousAdapter = null;
                ((dynamic)app).GetGlobalParameter(458, ref previousAdapter);
                try
                {
                    ((dynamic)app).SetGlobalParameter(458, true);
                    ((dynamic)assembly).SaveAs(paths[2]);
                }
                finally { ((dynamic)app).SetGlobalParameter(458, previousAdapter); }
                return paths;
            }
            finally
            {
                ComLifetime.Release(ref occurrence); ComLifetime.Release(ref occurrences);
                try { if (assembly != null) ((dynamic)assembly).Close(false); }
                finally { ComLifetime.Release(ref assembly); ComLifetime.Release(ref docs); ComLifetime.Release(ref app); }
            }
        }
    }
    [ComImport, Guid("000214E4-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IContextMenu
    {
        [PreserveSig] int QueryContextMenu(IntPtr menu, uint index, uint first, uint last, uint flags);
        void InvokeCommand(ref CommandInfo command);
        void GetCommandString(UIntPtr id, uint flags, IntPtr reserved, IntPtr text, uint max);
    }
    [ComImport, Guid("000214F4-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IContextMenu2
    {
        [PreserveSig] int QueryContextMenu(IntPtr menu, uint index, uint first, uint last, uint flags);
        void InvokeCommand(ref CommandInfo command);
        void GetCommandString(UIntPtr id, uint flags, IntPtr reserved, IntPtr text, uint max);
        [PreserveSig] int HandleMenuMsg(uint message, IntPtr wparam, IntPtr lparam);
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct CommandInfo
    {
        internal int Size;
        internal uint Mask;
        internal IntPtr Window, Verb, Parameters, Directory;
        internal int Show;
        internal uint HotKey;
        internal IntPtr Icon;
    }
    private delegate bool WindowCallback(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, WindowCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wparam, IntPtr lparam);
    [DllImport("user32.dll")] private static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll")] private static extern bool DestroyMenu(IntPtr menu);
    [DllImport("user32.dll")] private static extern int GetMenuItemCount(IntPtr menu);
    [DllImport("user32.dll")] private static extern IntPtr GetSubMenu(IntPtr menu, int position);
    [DllImport("user32.dll")] private static extern uint GetMenuItemID(IntPtr menu, int position);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetMenuString(IntPtr menu, uint id, StringBuilder text, int count, uint flags);
}
