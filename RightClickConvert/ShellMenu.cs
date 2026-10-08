using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SolidEdgeConvert
{
    internal static class ShellMenu
    {
        // Registration contract for a new conversion: add a stable CLSID here,
        // map its CLI argument and DelegateExecute value, register the matching
        // SelectionCommand in ShellServer, and remove the CLSID on uninstall.
        // Existing IDs must stay stable so installed Explorer verbs still resolve.
        // SystemFileAssociations preserves Solid Edge's file association and
        // remains effective even if another app becomes the default opener.
        internal const string MenuKey = @"Software\Classes\SystemFileAssociations\.par\shell\SolidEdgeMacros.Convert";
        internal const string DraftMenuKey = @"Software\Classes\SystemFileAssociations\.dft\shell\SolidEdgeMacros.Convert";
        internal const string AssemblyMenuKey = @"Software\Classes\SystemFileAssociations\.asm\shell\SolidEdgeMacros.Convert";
        internal const string StpMenuKey = @"Software\Classes\SystemFileAssociations\.stp\shell\SolidEdgeMacros.Convert";
        internal const string StepMenuKey = @"Software\Classes\SystemFileAssociations\.step\shell\SolidEdgeMacros.Convert";
        internal const string StepClass = "B84A6BE1-A4D2-4CD2-A1AE-60EAA476AD11";
        internal const string PdfClass = "AF39D53D-3C70-4055-8197-442F4C5180B2";
        internal const string DatedPdfClass = "D8F750C4-A88D-4F7A-BD12-590D6813B742";
        internal const string PartClass = "46FDDBA1-7601-445B-A3A6-7F7624B196C2";
        internal const string XtMenuKey = @"Software\Classes\SystemFileAssociations\.x_t\shell\SolidEdgeMacros.Convert";
        internal const string XbMenuKey = @"Software\Classes\SystemFileAssociations\.x_b\shell\SolidEdgeMacros.Convert";
        internal const string ParasolidPartClass = "263031DD-6A5B-4776-B4BC-39EB97D16A95";
        internal const string ParasolidAssemblyClass = "826FDBF0-726C-4EE8-B8B2-AE63E63ACAF9";
        internal const string StepAssemblyClass = "B5CC13CF-4069-451E-87E6-6E74686DD5AF";
        internal const string ParasolidExportClass = "BE83A0E7-B71A-416E-B129-7E798A0D14C7";
        internal const string StlClass = "91A68B80-217F-4B8F-B6B4-F49391163E7F";
        internal const string PdfSameTypeClass = "2240657E-EB65-4490-938E-A86F51D8DE87";
        internal const string DatedPdfSameTypeClass = "6AF4605B-E0A4-4DF7-8020-7A2B87FA506C";

        internal static string Command(string executable, ConversionFormat format = ConversionFormat.Step)
        {
            if (String.IsNullOrWhiteSpace(executable) || executable.IndexOf('"') >= 0 || !Path.IsPathRooted(executable))
                throw new ArgumentException("An absolute executable path is required.");
            // Explorer launches the executable directly, with both paths quoted.
            // Do not use a command shell: CAD names may contain &, %, or spaces.
            Conversion.FormatName(format); // reject unsupported formats before registry changes
            string argument = format == ConversionFormat.Step ? "--step" : format == ConversionFormat.Pdf ? "--pdf" : format == ConversionFormat.Part ? "--part"
                : format == ConversionFormat.PdfSameType ? "--pdf-same-type" : format == ConversionFormat.PdfWithDateSameType ? "--pdf-date-same-type"
                : format == ConversionFormat.ParasolidPart ? "--parasolid-part" : format == ConversionFormat.ParasolidAssembly ? "--parasolid-assembly"
                : format == ConversionFormat.StepAssembly ? "--step-assembly"
                : format == ConversionFormat.ParasolidExport ? "--parasolid" : format == ConversionFormat.Stl ? "--stl" : "--pdf-date";
            return "\"" + executable + "\" " + argument + " \"%1\"";
        }

        internal static void Install(RegistryKey root, string executable)
        {
            // Reapplying installation updates the existing keys to this executable
            // location. No registry-wide deletion or file-association replacement
            // is needed when upgrading; each verb is scoped to this application's key.
            if (!File.Exists(executable)) throw new FileNotFoundException("The converter executable could not be found.", executable);
            RegisterServer(root, executable, StepClass);
            RegisterServer(root, executable, PdfClass);
            RegisterServer(root, executable, DatedPdfClass);
            RegisterServer(root, executable, PartClass);
            RegisterServer(root, executable, ParasolidPartClass);
            RegisterServer(root, executable, ParasolidAssemblyClass);
            RegisterServer(root, executable, StepAssemblyClass);
            RegisterServer(root, executable, ParasolidExportClass);
            RegisterServer(root, executable, StlClass);
            RegisterServer(root, executable, PdfSameTypeClass);
            RegisterServer(root, executable, DatedPdfSameTypeClass);
            InstallFormat(root, executable, MenuKey, "01Step", "STEP (.stp)", ConversionFormat.Step);
            InstallFormat(root, executable, MenuKey, "02Parasolid", "Parasolid (.x_t)", ConversionFormat.ParasolidExport);
            InstallFormat(root, executable, MenuKey, "03Stl", "STL (.stl)", ConversionFormat.Stl);
            // Use the same export handlers for parts and assemblies. This also
            // lets Explorer deliver a mixed .par/.asm selection in one batch.
            InstallFormat(root, executable, AssemblyMenuKey, "01Step", "STEP (.stp)", ConversionFormat.Step);
            InstallFormat(root, executable, AssemblyMenuKey, "02Parasolid", "Parasolid (.x_t)", ConversionFormat.ParasolidExport);
            // Retain the existing active-sheet verb keys/CLSIDs on upgrade.
            InstallFormat(root, executable, DraftMenuKey, "01Pdf", "PDF - Active sheet only", ConversionFormat.Pdf);
            InstallFormat(root, executable, DraftMenuKey, "01PdfSameType", "PDF - All sheets of same type", ConversionFormat.PdfSameType);
            InstallFormat(root, executable, DraftMenuKey, "02PdfWithDate", "PDF with Date - Active sheet only", ConversionFormat.PdfWithDate);
            InstallFormat(root, executable, DraftMenuKey, "02PdfWithDateSameType", "PDF with Date - All sheets of same type", ConversionFormat.PdfWithDateSameType);
            InstallFormat(root, executable, StpMenuKey, "01Part", "Solid Edge Part (.par)", ConversionFormat.Part);
            InstallFormat(root, executable, StepMenuKey, "01Part", "Solid Edge Part (.par)", ConversionFormat.Part);
            foreach (string key in new[] { StpMenuKey, StepMenuKey })
                InstallFormat(root, executable, key, "02Assembly", "Solid Edge Assembly (.asm)", ConversionFormat.StepAssembly);
            foreach (string key in new[] { XtMenuKey, XbMenuKey })
            {
                InstallFormat(root, executable, key, "01Part", "Solid Edge Part (.par)", ConversionFormat.ParasolidPart);
                InstallFormat(root, executable, key, "02Assembly", "Solid Edge Assembly (.asm)", ConversionFormat.ParasolidAssembly);
            }
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
            // Player on both menu levels permits a complete multiple selection.
            // DelegateExecute uses the local COM handler for that selection; the
            // quoted command remains the single-file command representation.
            // Keep the command/CLSID mappings in agreement with Program's dispatch.
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
                        string id = format == ConversionFormat.Step ? StepClass : format == ConversionFormat.Pdf ? PdfClass : format == ConversionFormat.Part ? PartClass
                            : format == ConversionFormat.PdfSameType ? PdfSameTypeClass : format == ConversionFormat.PdfWithDateSameType ? DatedPdfSameTypeClass
                            : format == ConversionFormat.ParasolidPart ? ParasolidPartClass : format == ConversionFormat.ParasolidAssembly ? ParasolidAssemblyClass
                            : format == ConversionFormat.StepAssembly ? StepAssemblyClass
                            : format == ConversionFormat.ParasolidExport ? ParasolidExportClass : format == ConversionFormat.Stl ? StlClass : DatedPdfClass;
                        action.SetValue("DelegateExecute", "{" + id + "}");
                    }
                }
            }
        }

        internal static void Uninstall(RegistryKey root)
        {
            // Delete only this application's uniquely named verbs, not file types,
            // its association, or other programs' Convert commands.
            root.DeleteSubKeyTree(MenuKey, false);
            root.DeleteSubKeyTree(DraftMenuKey, false);
            root.DeleteSubKeyTree(AssemblyMenuKey, false);
            root.DeleteSubKeyTree(StpMenuKey, false);
            root.DeleteSubKeyTree(StepMenuKey, false);
            root.DeleteSubKeyTree(XtMenuKey, false);
            root.DeleteSubKeyTree(XbMenuKey, false);
            root.DeleteSubKeyTree(@"Software\Classes\CLSID\{" + StepClass + "}", false);
            root.DeleteSubKeyTree(@"Software\Classes\CLSID\{" + PdfClass + "}", false);
            root.DeleteSubKeyTree(@"Software\Classes\CLSID\{" + DatedPdfClass + "}", false);
            root.DeleteSubKeyTree(@"Software\Classes\CLSID\{" + PartClass + "}", false);
            root.DeleteSubKeyTree(@"Software\Classes\CLSID\{" + ParasolidPartClass + "}", false);
            root.DeleteSubKeyTree(@"Software\Classes\CLSID\{" + ParasolidAssemblyClass + "}", false);
            root.DeleteSubKeyTree(@"Software\Classes\CLSID\{" + StepAssemblyClass + "}", false);
            root.DeleteSubKeyTree(@"Software\Classes\CLSID\{" + ParasolidExportClass + "}", false);
            root.DeleteSubKeyTree(@"Software\Classes\CLSID\{" + StlClass + "}", false);
            root.DeleteSubKeyTree(@"Software\Classes\CLSID\{" + PdfSameTypeClass + "}", false);
            root.DeleteSubKeyTree(@"Software\Classes\CLSID\{" + DatedPdfSameTypeClass + "}", false);
        }

        internal static void NotifyExplorer() { SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero); }
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);
    }
}
