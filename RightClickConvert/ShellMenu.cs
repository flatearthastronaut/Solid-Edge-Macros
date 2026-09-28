using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SolidEdgeConvert
{
    internal static class ShellMenu
    {
        // SystemFileAssociations preserves Solid Edge's file association and
        // remains effective even if another app becomes the default opener.
        internal const string MenuKey = @"Software\Classes\SystemFileAssociations\.par\shell\SolidEdgeMacros.Convert";
        internal const string DraftMenuKey = @"Software\Classes\SystemFileAssociations\.dft\shell\SolidEdgeMacros.Convert";
        internal const string StepClass = "B84A6BE1-A4D2-4CD2-A1AE-60EAA476AD11";
        internal const string PdfClass = "AF39D53D-3C70-4055-8197-442F4C5180B2";

        internal static string Command(string executable, ConversionFormat format = ConversionFormat.Step)
        {
            if (String.IsNullOrWhiteSpace(executable) || executable.IndexOf('"') >= 0 || !Path.IsPathRooted(executable))
                throw new ArgumentException("An absolute executable path is required.");
            // Explorer launches the executable directly, with both paths quoted.
            // Do not use a command shell: CAD names may contain &, %, or spaces.
            Conversion.FormatName(format); // reject unsupported formats before registry changes
            return "\"" + executable + "\" " + (format == ConversionFormat.Step ? "--step" : "--pdf") + " \"%1\"";
        }

        internal static void Install(RegistryKey root, string executable)
        {
            if (!File.Exists(executable)) throw new FileNotFoundException("The converter executable could not be found.", executable);
            RegisterServer(root, executable, StepClass);
            RegisterServer(root, executable, PdfClass);
            InstallFormat(root, executable, MenuKey, "01Step", "STEP (.stp)", ConversionFormat.Step);
            InstallFormat(root, executable, DraftMenuKey, "01Pdf", "PDF (.pdf)", ConversionFormat.Pdf);
        }

        private static void RegisterServer(RegistryKey root, string executable, string id)
        {
            // Register the EXE as a per-user local COM server. No DLL is loaded
            // into Explorer, and no machine-wide registration/admin is required.
            Command(executable);
            using (RegistryKey server = root.CreateSubKey(@"Software\Classes\CLSID\{" + id + @"}\LocalServer32"))
            {
                server.SetValue("", "\"" + executable + "\" --shell-server");
                server.SetValue("ServerExecutable", executable);
            }
        }

        private static void InstallFormat(RegistryKey root, string executable, string key, string verb,
            string label, ConversionFormat format)
        {
            string command = Command(executable, format);
            using (RegistryKey menu = root.CreateSubKey(key))
            {
                menu.SetValue("MUIVerb", "Convert");
                menu.SetValue("SubCommands", "");
                menu.SetValue("Icon", "\"" + executable + "\",0");
                menu.SetValue("MultiSelectModel", "Player");
                using (RegistryKey step = menu.CreateSubKey(@"shell\" + verb))
                {
                    step.SetValue("MUIVerb", label);
                    step.SetValue("MultiSelectModel", "Player");
                    using (RegistryKey action = step.CreateSubKey("command"))
                    {
                        action.SetValue("", command);
                        action.SetValue("DelegateExecute", "{" + (format == ConversionFormat.Step ? StepClass : PdfClass) + "}");
                    }
                }
            }
        }

        internal static void Uninstall(RegistryKey root)
        {
            // Delete only this application's uniquely named verbs, not .par/.dft,
            // its association, or other programs' Convert commands.
            root.DeleteSubKeyTree(MenuKey, false);
            root.DeleteSubKeyTree(DraftMenuKey, false);
            root.DeleteSubKeyTree(@"Software\Classes\CLSID\{" + StepClass + "}", false);
            root.DeleteSubKeyTree(@"Software\Classes\CLSID\{" + PdfClass + "}", false);
        }

        internal static void NotifyExplorer() { SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero); }
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);
    }
}
