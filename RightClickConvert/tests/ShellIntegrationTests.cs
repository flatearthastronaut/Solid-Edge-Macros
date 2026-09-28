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
            if (args.Length != 2) throw new ArgumentException("Pass generated .par and .dft fixture paths.");
            Exercise(args[0], ".stp", "STEP (.stp)");
            Exercise(args[1], ".pdf", "PDF (.pdf)");
            Console.WriteLine("PASS actual Shell multi-selection menus: three STEP files and three PDFs, source hashes preserved.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void Exercise(string fixture, string outputExtension, string label)
    {
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work", "selection-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string[] paths = new string[3];
        string[] hashes = new string[3];
        for (int i = 0; i < paths.Length; i++)
        {
            paths[i] = Path.Combine(folder, "Selected " + i + " & é 100%" + Path.GetExtension(fixture));
            File.Copy(fixture, paths[i]); hashes[i] = Hash(paths[i]);
        }
        Console.WriteLine("Invoking " + label + " on " + paths.Length + " files through IContextMenu.");
        InvokeMenu(paths, label);
        Stopwatch elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed.TotalSeconds < 90)
        {
            bool complete = true;
            foreach (string path in paths) complete &= File.Exists(Path.ChangeExtension(path, outputExtension));
            if (complete) break;
            Thread.Sleep(200);
        }
        foreach (string path in paths)
        {
            string output = Path.ChangeExtension(path, outputExtension);
            if (!File.Exists(output)) throw new Exception("Shell selection did not convert " + path);
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
