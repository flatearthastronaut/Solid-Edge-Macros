using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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

    static int Main()
    {
        try
        {
            ComponentDisplayTests.Run(Check);
            // All filesystem activity remains inside this project's ignored test workspace.
            run = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".work", Guid.NewGuid().ToString("N"));
            local = Path.Combine(run, "Installed Macros"); documents = Path.Combine(run, "Documents"); settings = Path.Combine(run, "settings.xml");
            Directory.CreateDirectory(Path.Combine(local, ".se-manager"));
            Check(MacroCatalog.ManagerFolder(settings, documents) == Path.Combine(documents, "Add-ins"), "manager default uses user's Documents/Add-ins");
            new XDocument(new XElement("settings", new XElement("source", "Z:\\NeverScanThePublishShare"), new XElement("local", local))).Save(settings);
            Check(MacroCatalog.ManagerFolder(settings, documents) == local, "manager local preference wins over source and default");
            FileAt("Tool.exe"); FileAt("Tool-v1.2.exe"); FileAt("Helper.exe");
            // Non-square transparent artwork with semi-transparent green edges:
            // flattening this onto the old magenta key must fail the halo check.
            using (var icon = new Bitmap(64, 48))
            {
                using (var g = Graphics.FromImage(icon))
                using (var edge = new SolidBrush(Color.FromArgb(96, 114, 148, 3)))
                using (var fill = new SolidBrush(Color.FromArgb(114, 148, 3)))
                { g.Clear(Color.Transparent); g.FillRectangle(edge, 12, 8, 40, 32); g.FillRectangle(fill, 16, 12, 32, 24); }
                icon.Save(Path.Combine(local, "Tool.png"));
            }
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

            var target = new RibbonTarget();
            var addin = new Addin();
            addin.RegisterButton(target, Addin.ApplicationEnvironment, 100, "tool", "Tool", "Installed macros", "Run Tool", 120);
            Check(target.Style == (int)SolidEdgeFramework.SeButtonStyle.seButtonIconAndCaptionBelow && target.SmallImage == 120 && target.LargeImage == 121,
                "native macro registration requests a large icon with its caption below and the correct large resource");
            Check(target.CommandId == 100 && target.CommandCount == 1 && target.Group == "Hammill Macros\nInstalled macros" && target.Names.Contains("Hammill.MacroLauncher.tool"),
                "large button retains the macro command identity and group");
            addin.RegisterButton(target, Addin.ApplicationEnvironment, 1, "status", "Macro folder", "Launcher", "Show folder", -1);
            Check(target.Style == (int)SolidEdgeFramework.SeButtonStyle.seButtonCaption && target.SmallImage == -1 && target.LargeImage == -1 && target.CommandId == 1,
                "caption-only controls do not request an unavailable large image");

            // A real v0.1 signature snapshot: simulate upgrading with the same
            // installed macros, captions and artwork, plus pre-existing IDs.
            var oldLines = new System.Collections.Generic.List<string> { "hammill-launcher-ribbon-v1", catalog.Folder.ToLowerInvariant() };
            foreach (var entry in catalog.Entries.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                oldLines.Add(entry.Key + "|" + entry.Path.ToLowerInvariant() + "|" + entry.Name + "|" + entry.Group + "|" + entry.Environments + "|" +
                    (entry.IconPath == null ? "default" : MacroCatalog.Hash(File.ReadAllBytes(entry.IconPath))));
            string migrationPath = Path.Combine(run, "migration.xml");
            new XDocument(new XElement("ribbon", new XAttribute("version", 7), new XAttribute("signature", MacroCatalog.Hash(Encoding.UTF8.GetBytes(String.Join("\n", oldLines)))),
                new XElement("command", new XAttribute("key", "tool"), new XAttribute("id", 150)),
                new XElement("command", new XAttribute("key", "other"), new XAttribute("id", 151)))).Save(migrationPath);
            var upgraded = RibbonState.Update(migrationPath, catalog);
            Check(upgraded.Version == 8 && catalog.Entries.Single(x => x.Key == "tool").CommandId == 150 && catalog.Entries.Single(x => x.Key == "other").CommandId == 151,
                "upgrading small-button cache refreshes GUI once without changing existing macro IDs");
            Check(RibbonState.Update(migrationPath, Scan()).Version == 8, "subsequent large-button startup reuses the refreshed GUI");

            // v0.2 already had large buttons, but its resource cache contains
            // magenta-matted BMPs. Upgrade it without requiring artwork edits.
            oldLines[0] = "hammill-launcher-ribbon-v2-large-buttons";
            string oldSignature = MacroCatalog.Hash(Encoding.UTF8.GetBytes(String.Join("\n", oldLines)));
            var oldState = XDocument.Load(migrationPath);
            oldState.Root.SetAttributeValue("signature", oldSignature);
            oldState.Save(migrationPath);
            var alphaUpgrade = RibbonState.Update(migrationPath, catalog);
            Check(alphaUpgrade.Version == 9 && alphaUpgrade.Signature != oldSignature && catalog.Entries.Single(x => x.Key == "tool").CommandId == 150,
                "v0.2 bitmap cache upgrades to alpha PNGs without changing macro IDs");
            Check(RibbonState.Update(migrationPath, Scan()).Version == 9, "alpha PNG upgrade refreshes GUI only once");

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
            string template = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Compiled Executables", "Hammill-Macro-Launcher-v0.3", "Hammill.MacroLauncher.Resources.dll"));
            string stale = Path.Combine(run, "IconCache", "ribbon-" + oldSignature + ".dll");
            Directory.CreateDirectory(Path.GetDirectoryName(stale));
            File.WriteAllText(stale, "old bitmap cache fixture");
            var resources = IconResources.Create(resourcesCatalog, template, Path.Combine(run, "IconCache"), resourcesState.Signature);
            IconResourceTests.Run(resources, resourcesCatalog, Check);
            // A stale DLL remains untouched, while the upgraded signature selects
            // a distinct PNG DLL. No manual deletion of user caches is needed.
            Check(resources.FileName != stale && File.ReadAllText(stale) == "old bitmap cache fixture",
                "alpha upgrade uses a new resource path and preserves the old cache");
            Check(IconResources.Create(resourcesCatalog, template, Path.Combine(run, "IconCache"), resourcesState.Signature).FileName == resources.FileName, "unchanged icon resources are reused");
            IconResourceTests.Preview(template, run);
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
