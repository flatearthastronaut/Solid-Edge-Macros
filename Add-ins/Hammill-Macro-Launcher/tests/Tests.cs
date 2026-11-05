using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Hammill.MacroLauncher;

internal static class Tests
{
    static int passed;
    static string run, local, settings, documents;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    static void FileAt(string relative) { string path = MacroCatalog.Child(local, relative); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, "test fixture; never executed"); }
    static void Package(string key, bool stable, params string[] paths)
    {
        new XDocument(new XElement("package", new XAttribute("version", "1.2"), new XAttribute("stableCopies", stable ? "1" : "0"),
            paths.Select(p => new XElement("file", new XAttribute("path", p), new XAttribute("hash", "fixture")))))
            .Save(Path.Combine(local, ".se-manager", key + ".xml"));
    }
    static MacroCatalog Scan() { return MacroCatalog.Scan(settings, documents); }
    static void Override(params object[] attributes)
    { new XDocument(new XElement("macros", new XElement("macro", attributes))).Save(Path.Combine(local, "HammillMacros.xml")); }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern IntPtr LoadLibraryEx(string name, IntPtr file, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern IntPtr FindResource(IntPtr module, IntPtr name, IntPtr type);
    [DllImport("kernel32.dll")] static extern uint SizeofResource(IntPtr module, IntPtr resource);
    [DllImport("kernel32.dll")] static extern bool FreeLibrary(IntPtr module);

    static int Main()
    {
        try
        {
            // All filesystem activity remains inside this project's ignored test workspace.
            run = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".work", Guid.NewGuid().ToString("N"));
            local = Path.Combine(run, "Installed Macros"); documents = Path.Combine(run, "Documents"); settings = Path.Combine(run, "settings.xml");
            Directory.CreateDirectory(Path.Combine(local, ".se-manager"));
            Check(MacroCatalog.ManagerFolder(settings, documents) == Path.Combine(documents, "Add-ins"), "manager default uses user's Documents/Add-ins");
            new XDocument(new XElement("settings", new XElement("source", "Z:\\NeverScanThePublishShare"), new XElement("local", local))).Save(settings);
            Check(MacroCatalog.ManagerFolder(settings, documents) == local, "manager local preference wins over source and default");
            FileAt("Tool.exe"); FileAt("Tool-v1.2.exe"); FileAt("Helper.exe");
            using (var icon = new Bitmap(32, 32)) { using (var g = Graphics.FromImage(icon)) g.Clear(Color.FromArgb(114, 148, 3)); icon.Save(Path.Combine(local, "Tool.png")); }
            Package("tool", true, "Tool.exe", "Tool-v1.2.exe", "Helper.exe", "Tool.png");
            FileAt("WindowsProgram.exe"); Package("windowsprogram", false, "WindowsProgram.exe");
            FileAt("Unmanaged.exe");
            FileAt("Other\\Compiled Executables\\Other.exe"); FileAt("Other-v1.2\\Compiled Executables\\Other-v1.2.exe");
            Package("other", true, "Other\\Compiled Executables\\Other.exe", "Other-v1.2\\Compiled Executables\\Other-v1.2.exe");
            var catalog = Scan();
            Check(catalog.Entries.Count == 2, "one main EXE per managed macro; no numbered releases, helpers, Windows programs or unmanaged EXEs");
            Check(catalog.Entries.Single(x => x.Key == "tool").IconPath == Path.Combine(local, "Tool.png"), "stable companion icon is discovered");
            Check(catalog.Entries.Single(x => x.Key == "other").Path.EndsWith("Other\\Compiled Executables\\Other.exe"), "nested stable package target works");
            Check(catalog.Notices.Count == 0, "valid manager packages produce no warnings");

            string statePath = Path.Combine(run, "State", "ribbon.xml");
            var state1 = RibbonState.Update(statePath, catalog); int toolId = catalog.Entries.Single(x => x.Key == "tool").CommandId;
            Check(state1.Version == 1 && toolId >= 100, "first scan allocates a GUI version and command IDs");
            var unchanged = Scan(); var state2 = RibbonState.Update(statePath, unchanged);
            Check(state2.Version == state1.Version && unchanged.Entries.Single(x => x.Key == "tool").CommandId == toolId, "unchanged startup preserves GUI version and IDs");
            File.WriteAllText(Path.Combine(local, "Tool.exe"), "updated macro version");
            Check(RibbonState.Update(statePath, Scan()).Version == state2.Version, "updating executable contents does not rebuild ribbon");

            Override(new XAttribute("package", "tool"), new XAttribute("name", "Tool renamed"), new XAttribute("group", "Drawing"), new XAttribute("environments", "draft"));
            var named = Scan(); var state3 = RibbonState.Update(statePath, named);
            Check(named.Entries.Single(x => x.Key == "tool").Name == "Tool renamed" && named.Entries.Single(x => x.Key == "tool").Environments == "draft", "configuration controls labels, groups and document environments");
            Check(state3.Version == state2.Version + 1 && named.Entries.Single(x => x.Key == "tool").CommandId == toolId, "layout changes refresh GUI while preserving identity");
            Override(new XAttribute("package", "tool"), new XAttribute("enabled", false));
            var disabled = Scan(); var state4 = RibbonState.Update(statePath, disabled);
            Check(disabled.Entries.Count == 1 && state4.Version == state3.Version + 1, "disabled macro is removed from managed ribbon");
            File.Delete(Path.Combine(local, "HammillMacros.xml"));
            var restored = Scan(); RibbonState.Update(statePath, restored);
            Check(restored.Entries.Single(x => x.Key == "tool").CommandId == toolId, "restored macro keeps its original command ID");
            FileAt("Added.exe"); Package("added", true, "Added.exe");
            var added = Scan(); var state5 = RibbonState.Update(statePath, added);
            Check(added.Entries.Count == 3 && added.Entries.Single(x => x.Key == "added").CommandId != toolId, "new installation receives a distinct button without renumbering existing tools");
            File.Delete(Path.Combine(local, "Added.exe"));
            var missing = Scan();
            Check(missing.Entries.Count == 2 && missing.Notices.Any(x => x.Contains("missing")), "missing target produces a repair notice, not a runnable button");
            File.Delete(Path.Combine(local, ".se-manager", "added.xml"));

            File.WriteAllText(Path.Combine(local, ".se-manager", "broken.xml"), "<package");
            Check(Scan().Entries.Count == 2 && Scan().Notices.Count == 1, "one malformed manifest does not hide other valid macros");
            File.Delete(Path.Combine(local, ".se-manager", "broken.xml"));
            bool rejected = false; try { MacroCatalog.Child(local, "..\\outside.exe"); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "path traversal is rejected");
            rejected = false; try { MacroCatalog.Child(local, "Tool.exe:stream"); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "alternate data stream paths are rejected");
            Override(new XAttribute("package", "tool"), new XAttribute("path", "Unmanaged.exe"));
            Check(Scan().Entries.Count == 1, "configuration cannot launch an EXE outside its owning package manifest");
            Override(new XAttribute("package", "tool"), new XAttribute("name", "Bad\nInjected"));
            Check(Scan().Entries.Count == 1, "ribbon command metadata cannot be injected through labels");
            Override(new XAttribute("package", "tool"), new XAttribute("environments", "unknown"));
            Check(Scan().Entries.Count == 1, "invalid document environments are reported");
            File.Delete(Path.Combine(local, "HammillMacros.xml"));
            FileAt("second\\Tool.exe"); Package("tool", true, "Tool.exe", "second\\Tool.exe", "Tool.png");
            Check(Scan().Entries.Count == 1, "ambiguous main executables require explicit selection");
            Override(new XAttribute("package", "tool"), new XAttribute("path", "Tool.exe"));
            Check(Scan().Entries.Count == 2, "explicit manifest-owned path resolves an ambiguous package");
            File.Delete(Path.Combine(local, "HammillMacros.xml")); Package("tool", true, "Tool.exe", "Tool.png");

            var resourcesCatalog = Scan(); var resourcesState = RibbonState.Update(statePath, resourcesCatalog);
            string template = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Compiled Executables", "Hammill-Macro-Launcher-v0.1", "Hammill.MacroLauncher.Resources.dll"));
            var resources = IconResources.Create(resourcesCatalog, template, Path.Combine(run, "IconCache"), resourcesState.Signature);
            IntPtr module = LoadLibraryEx(resources.FileName, IntPtr.Zero, 2);
            Check(module != IntPtr.Zero, "generated ribbon resource DLL loads as data");
            try
            {
                int id = resources.ResourceIds[resourcesCatalog.Entries.Single(x => x.Key == "tool").CommandId];
                IntPtr small = FindResource(module, new IntPtr(id), new IntPtr(2)), large = FindResource(module, new IntPtr(id + 1), new IntPtr(2));
                Check(small != IntPtr.Zero && large != IntPtr.Zero && SizeofResource(module, large) > SizeofResource(module, small), "small and large bitmap resources are present");
            }
            finally { if (module != IntPtr.Zero) FreeLibrary(module); }
            Check(IconResources.Create(resourcesCatalog, template, Path.Combine(run, "IconCache"), resourcesState.Signature).FileName == resources.FileName, "unchanged icon resources are reused");
            using (var changedIcon = new Bitmap(32, 32)) { using (var g = Graphics.FromImage(changedIcon)) g.Clear(Color.Gray); changedIcon.Save(Path.Combine(local, "Tool.png")); }
            var newIcons = RibbonState.Update(statePath, Scan());
            Check(newIcons.Version == resourcesState.Version + 1, "changed companion artwork refreshes the managed ribbon");
            var concurrentA = Task.Factory.StartNew(() => RibbonState.Update(statePath, Scan()));
            var concurrentB = Task.Factory.StartNew(() => RibbonState.Update(statePath, Scan()));
            Task.WaitAll(concurrentA, concurrentB);
            Check(concurrentA.Result.Version == newIcons.Version && concurrentB.Result.Version == newIcons.Version, "concurrent startups preserve a consistent GUI version");
            string saved = File.ReadAllText(settings);
            File.WriteAllText(settings, "<!DOCTYPE settings [<!ENTITY ext SYSTEM 'file:///not-read'>]><settings><local>&ext;</local></settings>");
            Check(Scan().Entries.Count == 0 && Scan().Notices.Any(x => x.Contains("stopped")), "external XML entities are rejected without falling back to a different folder");
            File.WriteAllText(settings, saved);
            string newFolder = Path.Combine(run, "New Install Folder"); Directory.CreateDirectory(newFolder);
            new XDocument(new XElement("settings", new XElement("local", newFolder))).Save(settings);
            Check(Scan().Folder == newFolder && Scan().Entries.Count == 0, "the next startup follows a changed manager install folder");
            Console.WriteLine("Passed " + passed + " checks. Fixtures: " + run); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
