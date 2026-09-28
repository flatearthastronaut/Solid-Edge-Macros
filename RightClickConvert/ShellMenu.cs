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

        internal static string Command(string executable)
        {
            if (String.IsNullOrWhiteSpace(executable) || executable.IndexOf('"') >= 0 || !Path.IsPathRooted(executable))
                throw new ArgumentException("An absolute executable path is required.");
            // Explorer launches the executable directly, with both paths quoted.
            // Do not use a command shell: CAD names may contain &, %, or spaces.
            return "\"" + executable + "\" --step \"%1\"";
        }

        internal static void Install(RegistryKey root, string executable)
        {
            if (!File.Exists(executable)) throw new FileNotFoundException("The converter executable could not be found.", executable);
            string command = Command(executable);
            using (RegistryKey menu = root.CreateSubKey(MenuKey))
            {
                menu.SetValue("MUIVerb", "Convert");
                menu.SetValue("SubCommands", "");
                menu.SetValue("Icon", "\"" + executable + "\",0");
                menu.SetValue("MultiSelectModel", "Single");
                using (RegistryKey step = menu.CreateSubKey(@"shell\01Step"))
                {
                    step.SetValue("MUIVerb", "STEP (.stp)");
                    step.SetValue("MultiSelectModel", "Single");
                    using (RegistryKey action = step.CreateSubKey("command"))
                        action.SetValue("", command);
                }
            }
        }

        internal static void Uninstall(RegistryKey root)
        {
            // Delete only this application's uniquely named verb, not .par,
            // its association, or other programs' Convert commands.
            root.DeleteSubKeyTree(MenuKey, false);
        }

        internal static void NotifyExplorer() { SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero); }
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);
    }
}
