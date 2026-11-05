using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Hammill.MacroLauncher
{
    // This is deliberately independent of Solid Edge: discovery must never execute a macro.
    internal sealed class MacroEntry
    {
        public string Key, Name, Path, IconPath, Group = "Installed macros", Environments = "part,assembly,draft,sheetmetal";
        public int CommandId { get; set; }
    }

    internal sealed class MacroCatalog
    {
        public string Folder = "";
        public readonly List<MacroEntry> Entries = new List<MacroEntry>();
        public readonly List<string> Notices = new List<string>();

        internal static XDocument ReadXml(string path)
        {
            // Configuration is data. Never resolve external entities or allow unbounded documents.
            var options = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 4000000 };
            using (var reader = XmlReader.Create(path, options)) return XDocument.Load(reader);
        }

        public static string ManagerFolder(string settingsPath, string documentsFolder)
        {
            string path = System.IO.Path.Combine(documentsFolder, "Add-ins");
            if (File.Exists(settingsPath))
            {
                var root = ReadXml(settingsPath).Root;
                if (root == null || root.Name != "settings") throw new InvalidDataException("Unexpected Macro Manager settings format.");
                string configured = (string)root.Element("local");
                if (!String.IsNullOrWhiteSpace(configured)) path = configured.Trim();
            }
            // Read the USER manager's local field, never the admin publisher's destination/source.
            if (!System.IO.Path.IsPathRooted(path)) throw new InvalidDataException("Macro Manager's local folder must be an absolute path.");
            return System.IO.Path.GetFullPath(path);
        }

        internal static string FamilyKey(string name)
        { return Regex.Replace(name, @"[\s_.-]+", "").ToLowerInvariant(); }

        internal static bool Versioned(string name)
        { return Regex.IsMatch(name, @"^(.*?)[\s_.-]*[vV]?(\d+(?:\.\d+){1,3})$"); }

        internal static string Child(string root, string relative)
        {
            if (String.IsNullOrWhiteSpace(relative) || System.IO.Path.IsPathRooted(relative) || relative.IndexOf(':') >= 0)
                throw new InvalidDataException("Invalid relative macro path: " + relative);
            string parent = System.IO.Path.GetFullPath(root).TrimEnd('\\', '/') + System.IO.Path.DirectorySeparatorChar;
            string result = System.IO.Path.GetFullPath(System.IO.Path.Combine(parent, relative));
            if (!result.StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Macro path leaves its install folder: " + relative);
            // Match the manager's no-junction policy; a manifest must not redirect execution elsewhere.
            for (string p = result; p != null; p = System.IO.Path.GetDirectoryName(p))
                if ((File.Exists(p) || Directory.Exists(p)) && (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Linked paths are not supported: " + p);
            return result;
        }

        internal static bool StableExecutable(string relative)
        {
            if (!System.IO.Path.GetExtension(relative).Equals(".exe", StringComparison.OrdinalIgnoreCase)) return false;
            var parts = relative.Split('\\', '/');
            if (parts.Any(p => p.StartsWith(".") || new[] { "exclude", "tests", "backups" }.Contains(p.ToLowerInvariant()))) return false;
            if (Versioned(System.IO.Path.GetFileNameWithoutExtension(relative))) return false;
            return !parts.Take(parts.Length - 1).Any(Versioned);
        }

        internal static string DisplayText(string text)
        {
            if (String.IsNullOrWhiteSpace(text) || text.Any(Char.IsControl) || text.Contains("\\"))
                throw new InvalidDataException("Invalid ribbon label: " + text);
            return text.Trim();
        }

        public static MacroCatalog Scan(string settingsPath, string documentsFolder)
        {
            var result = new MacroCatalog();
            try
            {
                result.Folder = ManagerFolder(settingsPath, documentsFolder);
                if (!Directory.Exists(result.Folder)) { result.Notices.Add("Macro Manager's local folder is unavailable: " + result.Folder); return result; }
                string control = Child(result.Folder, ".se-manager");
                if (!Directory.Exists(control)) { result.Notices.Add("No Macro Manager installation records were found. Install macros with the user manager first."); return result; }
                var overrides = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
                string configPath = Child(result.Folder, "HammillMacros.xml");
                if (File.Exists(configPath))
                {
                    var config = ReadXml(configPath).Root;
                    if (config == null || config.Name != "macros") throw new InvalidDataException("HammillMacros.xml must have a macros root.");
                    foreach (var item in config.Elements("macro"))
                    {
                        string key = (string)item.Attribute("package");
                        if (String.IsNullOrWhiteSpace(key) || overrides.ContainsKey(key)) throw new InvalidDataException("Missing or duplicate package in HammillMacros.xml.");
                        overrides.Add(key, item);
                    }
                }
                foreach (string file in Directory.GetFiles(control, "*.xml").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                {
                    try
                    {
                        Child(control, System.IO.Path.GetFileName(file));
                        var package = ReadXml(file).Root;
                        // stableCopies=1 is emitted only for the Solid Edge Macros category by v1.11+.
                        // Add-ins, Windows programs and transaction records are not launcher entries.
                        if (package == null || package.Name != "package" || (string)package.Attribute("stableCopies") != "1") continue;
                        string key = System.IO.Path.GetFileNameWithoutExtension(file);
                        XElement option; overrides.TryGetValue(key, out option);
                        if (option != null && option.Attribute("enabled") != null && !Boolean.Parse((string)option.Attribute("enabled"))) continue;
                        var owned = package.Elements("file").Select(x => (string)x.Attribute("path")).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                        var candidates = owned.Where(StableExecutable).ToList();
                        string requested = option == null ? null : (string)option.Attribute("path");
                        if (!String.IsNullOrWhiteSpace(requested)) candidates = candidates.Where(x => x.Equals(requested, StringComparison.OrdinalIgnoreCase)).ToList();
                        else candidates = candidates.Where(x => FamilyKey(System.IO.Path.GetFileNameWithoutExtension(x)).Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
                        // A package may include helper EXEs. Only its main family-matching executable,
                        // or an explicitly selected manifest-owned stable executable, gets a button.
                        if (candidates.Count != 1) { result.Notices.Add(key + ": no unique stable main EXE; select its path in HammillMacros.xml or repair it in Macro Manager."); continue; }
                        string path = Child(result.Folder, candidates[0]);
                        if (!File.Exists(path)) { result.Notices.Add(key + ": stable EXE is missing; repair it in Macro Manager."); continue; }
                        var entry = new MacroEntry { Key = key, Path = path, Name = DisplayText(option == null ? System.IO.Path.GetFileNameWithoutExtension(path) : ((string)option.Attribute("name") ?? System.IO.Path.GetFileNameWithoutExtension(path))) };
                        if (option != null)
                        {
                            if (option.Attribute("group") != null) entry.Group = DisplayText((string)option.Attribute("group"));
                            if (option.Attribute("environments") != null)
                            {
                                var env = ((string)option.Attribute("environments")).ToLowerInvariant().Split(',').Select(x => x.Trim()).Distinct().OrderBy(x => x).ToArray();
                                if (env.Length == 0 || env.Any(x => !new[] { "part", "assembly", "draft", "sheetmetal" }.Contains(x))) throw new InvalidDataException("Unknown macro environment for " + key);
                                entry.Environments = String.Join(",", env);
                            }
                        }
                        string customIcon = option == null ? null : (string)option.Attribute("icon");
                        if (!String.IsNullOrWhiteSpace(customIcon))
                        {
                            entry.IconPath = Child(result.Folder, customIcon);
                            if (!File.Exists(entry.IconPath)) throw new FileNotFoundException("Configured icon is missing.", entry.IconPath);
                        }
                        else
                        {
                            foreach (string extension in new[] { ".png", ".ico", ".bmp" })
                            {
                                string icon = System.IO.Path.ChangeExtension(candidates[0], extension);
                                if (owned.Contains(icon, StringComparer.OrdinalIgnoreCase) && File.Exists(Child(result.Folder, icon))) { entry.IconPath = Child(result.Folder, icon); break; }
                            }
                        }
                        result.Entries.Add(entry);
                    }
                    catch (Exception error) { result.Notices.Add(System.IO.Path.GetFileName(file) + ": " + error.Message); }
                }
                result.Entries.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name));
            }
            catch (Exception error) { result.Entries.Clear(); result.Notices.Add("Macro discovery stopped: " + error.Message); }
            return result;
        }

        internal static string Hash(byte[] bytes)
        { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }

        internal string RibbonSignature()
        {
            // EXE contents/version are intentionally absent: a normal macro update keeps its button.
            var lines = new List<string> { "hammill-launcher-ribbon-v1", Folder.ToLowerInvariant() };
            foreach (var entry in Entries.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                string icon = "default";
                try { if (entry.IconPath != null) icon = Hash(File.ReadAllBytes(entry.IconPath)); } catch { icon = "unavailable"; }
                lines.Add(entry.Key + "|" + entry.Path.ToLowerInvariant() + "|" + entry.Name + "|" + entry.Group + "|" + entry.Environments + "|" + icon);
            }
            return Hash(Encoding.UTF8.GetBytes(String.Join("\n", lines)));
        }
    }
}
