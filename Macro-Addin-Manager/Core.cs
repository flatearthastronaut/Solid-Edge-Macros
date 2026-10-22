using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CoopManager
{
    // Filename versions, not floating point: 1.10 > 1.9, and 1.0 == 1.0.0.
    internal sealed class ReleaseName
    {
        public string Name, Version;
        public int[] Parts;
        public string Key { get { return Regex.Replace(Name, @"[\s_.-]+", "").ToLowerInvariant(); } }
        public static ReleaseName Parse(string value, bool file)
        {
            string stem = file ? Path.GetFileNameWithoutExtension(value) : value;
            Match m = Regex.Match(stem, @"^(.*?)[\s_.-]*[vV]?(\d+(?:\.\d+){1,3})$");
            int[] parts = null;
            if (m.Success)
            {
                try { parts = m.Groups[2].Value.Split('.').Select(Int32.Parse).ToArray(); }
                catch (OverflowException) { m = Match.Empty; }
            }
            return new ReleaseName { Name = m.Success ? m.Groups[1].Value.TrimEnd(' ', '-', '_', '.') : stem,
                Version = m.Success ? m.Groups[2].Value : "", Parts = parts };
        }
        public static int Compare(ReleaseName a, ReleaseName b)
        {
            for (int i = 0; i < 4; i++)
            {
                int x = a.Parts != null && i < a.Parts.Length ? a.Parts[i] : 0;
                int y = b.Parts != null && i < b.Parts.Length ? b.Parts[i] : 0;
                if (x != y) return x.CompareTo(y);
            }
            return 0;
        }
    }
    internal sealed class Package
    {
        public ReleaseName Release;
        public string Path, Kind;
        public bool Bundle;
        public string Key { get { return Bundle ? "shared-addins" : Release.Key; } }
        public string Label { get { return Bundle ? "Shared SE add-ins" : Release.Name; } }
    }
    internal sealed class Entry
    {
        public Package Available;
        public string Name, AvailableVersion, InstalledVersion, Status, Details, PackageKey;
        public bool Actionable;
        public string Category = Categories.Macros;
    }
    internal static class Files
    {
        public static string Full(string path) { return System.IO.Path.GetFullPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar); }
        public static bool Inside(string parent, string child)
        { return Full(child).StartsWith(Full(parent) + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase); }
        public static void NoLinks(string path)
        {
            // Check ancestors too: an apparently ordinary destination can sit below a junction.
            for (string p = System.IO.Path.GetFullPath(path); p != null; p = System.IO.Path.GetDirectoryName(p))
                if ((File.Exists(p) || Directory.Exists(p)) && (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Links/junctions are not supported: " + p);
        }
        public static string Child(string root, string relative)
        {
            string p = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, relative));
            if (System.IO.Path.IsPathRooted(relative) || !Inside(root, p)) throw new IOException("Unsafe package path: " + relative);
            NoLinks(p); return p;
        }
        public static IEnumerable<string> Tree(string root)
        {
            NoLinks(root);
            foreach (string p in Directory.GetFiles(root)) { NoLinks(p); yield return p; }
            foreach (string d in Directory.GetDirectories(root))
                foreach (string p in Tree(d)) yield return p;
        }
        public static bool ExcludedFolder(string path)
        { return System.IO.Path.GetFileName(path.TrimEnd('\\','/')).Equals("Exclude",StringComparison.OrdinalIgnoreCase); }
        public static bool ExcludedRelativeFile(string relative)
        { return (System.IO.Path.GetDirectoryName(relative)??"").Split('\\','/').Any(s=>s.Equals("Exclude",StringComparison.OrdinalIgnoreCase)); }
        // Source discovery must prune the directory before opening it, rather than filtering files afterward.
        // Keep Tree unfiltered for transaction verification, backup cleanup, and local inventory.
        public static IEnumerable<string> SourceTree(string root)
        {
            if(ExcludedFolder(root))yield break;
            NoLinks(root);
            foreach(string file in Directory.GetFiles(root)){NoLinks(file);yield return file;}
            foreach(string directory in Directory.GetDirectories(root))
                if(!ExcludedFolder(directory))foreach(string file in SourceTree(directory))yield return file;
        }
        public static string Relative(string root, string file) { return file.Substring(Full(root).Length + 1); }
        public static string Hash(string path)
        { using (var sha = SHA256.Create()) using (var s = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(s)); }
        public static void Copy(string from, string to)
        {
            NoLinks(from); NoLinks(to); Directory.CreateDirectory(System.IO.Path.GetDirectoryName(to));
            File.Copy(from, to, true);
            if (Hash(from) != Hash(to)) throw new IOException("Copy verification failed: " + from);
            Diagnostics.Info("COPY VERIFIED from=" + from + " to=" + to);
        }
        public static void RemoveTree(string root, string target)
        {
            if (!Inside(root, target)) throw new IOException("Refusing deletion outside transaction directory.");
            NoLinks(target); Tree(target).ToArray(); Directory.Delete(target, true);
        }
    }
    internal static class Catalog
    {
        public static bool Script(string path, string name) { return File.Exists(System.IO.Path.Combine(path, name)); }
        public static bool RootAddins(string path)
        { return Script(path, "Register.cmd") || File.Exists(System.IO.Path.Combine(path, "SEAddIn.dll")); }
        static bool MacroFolder(string path, bool exclude = true)
        {
            var files = (exclude ? Files.SourceTree(path) : Files.Tree(path)).ToList();
            return files.Any(f => System.IO.Path.GetExtension(f).Equals(".exe", StringComparison.OrdinalIgnoreCase)) &&
                !files.Any(f => System.IO.Path.GetFileName(f).Equals("Register.cmd", StringComparison.OrdinalIgnoreCase));
        }
        public static List<Package> Scan(string root, bool exclude = true)
        {
            var list = new List<Package>();
            if ((exclude && Files.ExcludedFolder(root)) || !Directory.Exists(root)) return list;
            Files.NoLinks(root);
            bool bundle = RootAddins(root);
            if (bundle) list.Add(new Package { Path = root, Bundle = true, Kind = "Add-in bundle", Release = ReleaseName.Parse("Shared SE add-ins", false) });
            foreach (string p in Directory.GetFileSystemEntries(root))
            {
                if(exclude && Files.ExcludedFolder(p))continue;
                Files.NoLinks(p);
                string name = System.IO.Path.GetFileName(p);
                if (name.StartsWith(".")) continue;
                bool dir = Directory.Exists(p);
                string ext = System.IO.Path.GetExtension(p).ToLowerInvariant();
                if (dir && bundle && !MacroFolder(p, exclude)) continue;
                if (!dir && ext != ".exe" && ext != ".zip") continue;
                if (dir && !(exclude ? Files.SourceTree(p) : Files.Tree(p)).Any(f => new[] { ".exe", ".dll", ".zip" }.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()))) continue;
                list.Add(new Package { Path = p, Release = ReleaseName.Parse(name, !dir), Kind = dir ? "Folder" : ext == ".zip" ? "ZIP" : "Macro" });
            }
            return list;
        }
        public static Dictionary<string, string> SourceFiles(Package p)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (p.Bundle)
            {
                var macroDirs = Directory.GetDirectories(p.Path).Where(d => !Files.ExcludedFolder(d) && !System.IO.Path.GetFileName(d).StartsWith(".") && MacroFolder(d)).ToArray();
                foreach (string f in Files.SourceTree(p.Path))
                {
                    string r = Files.Relative(p.Path, f);
                    if (r.Split('\\', '/').Any(s => s.StartsWith("."))) continue;
                    if (macroDirs.Any(d => Files.Inside(d, f))) continue;
                    // Root macros/ZIPs are separate packages; everything else belongs to the shared add-in suite.
                    if (System.IO.Path.GetDirectoryName(r) == "" && new[] { ".exe", ".zip" }.Contains(System.IO.Path.GetExtension(r).ToLowerInvariant())) continue;
                    result.Add(r, f);
                }
            }
            else if (Directory.Exists(p.Path))
                foreach (string f in Files.SourceTree(p.Path)) result.Add(System.IO.Path.Combine(System.IO.Path.GetFileName(p.Path), Files.Relative(p.Path, f)), f);
            else result.Add(System.IO.Path.GetFileName(p.Path), p.Path);
            return result;
        }
        public static List<Entry> Compare(string source, string local)
        {
            if (!Directory.Exists(source)) throw new IOException("Available folder cannot be reached: " + source);
            var available = Scan(source); var installed = Scan(local, false); var rows = new List<Entry>();
            foreach (var group in available.GroupBy(p => p.Key))
            {
                var ordered = group.OrderByDescending(candidate => candidate.Release, Comparer<ReleaseName>.Create(ReleaseName.Compare)).ToList();
                var p = ordered[0];
                var old = installed.Where(x => x.Key == p.Key).OrderByDescending(x => x.Release, Comparer<ReleaseName>.Create(ReleaseName.Compare)).ToList();
                string state = old.Count == 0 ? "Not installed" : ReleaseName.Compare(old[0].Release, p.Release) < 0 ? "Update available" :
                    ReleaseName.Compare(old[0].Release, p.Release) > 0 ? "Local version newer" : "Up to date";
                string manifest = System.IO.Path.Combine(local, ".se-manager", p.Key + ".xml");
                if (File.Exists(manifest) && state != "Local version newer")
                {
                    var doc = XDocument.Load(manifest);
                    if ((string)doc.Root.Attribute("version") == p.Release.Version)
                    {
                        var files = doc.Root.Elements("file").Where(f=>p.Kind=="ZIP" || !Files.ExcludedRelativeFile((string)f.Attribute("path"))).ToList();
                        bool valid = files.All(f => File.Exists(Files.Child(local, (string)f.Attribute("path"))) && Files.Hash(Files.Child(local, (string)f.Attribute("path"))) == (string)f.Attribute("hash"));
                        state = valid ? "Up to date" : "Repair available";
                        if (valid && p.Kind != "ZIP")
                        {
                            var src = SourceFiles(p);
                            if (src.Count != files.Count || files.Any(f => !src.ContainsKey((string)f.Attribute("path")) || Files.Hash(src[(string)f.Attribute("path")]) != (string)f.Attribute("hash"))) state = "Update available";
                        }
                        if (valid && p.Kind == "ZIP" && Files.Hash(p.Path) != (string)doc.Root.Attribute("sourceHash")) state = "Update available";
                    }
                }
                else if (old.Count > 0 && state != "Local version newer" && (p.Bundle || p.Release.Version == "" || state == "Up to date"))
                {
                    if (p.Kind == "ZIP") state = "Compare / install";
                    else
                    {
                        var src = SourceFiles(p);
                        state = src.All(f => File.Exists(Files.Child(local, f.Key)) && Files.Hash(f.Value) == Files.Hash(Files.Child(local, f.Key))) ? "Up to date" : "Update available";
                    }
                }
                if (state == "Up to date" && old.Any(x => x.Release.Parts != null && p.Release.Parts != null && ReleaseName.Compare(x.Release, p.Release) < 0)) state = "Cleanup available";
                // Same-version ZIP and EXE can be alternate releases. Never pick silently.
                if (ordered.Count(x => ReleaseName.Compare(x.Release, p.Release) == 0) > 1) state = "Ambiguous source versions";
                if (p.Bundle && (!Script(p.Path, "Register.cmd") || !Script(p.Path, "UNRegister.cmd"))) state = "Registration scripts missing";
                var localDirs = Directory.Exists(local) ? Directory.GetDirectories(local) : new string[0];
                var details = p.Bundle ? "Shared add-in components update together. Requires Register.cmd and UNRegister.cmd.\r\n" + string.Join("\r\n", Directory.GetDirectories(p.Path).Where(d => !Files.ExcludedFolder(d) && !MacroFolder(d)).Select(d => System.IO.Path.GetFileName(d) + "  |  local: " +
                    (localDirs.Any(ld => ReleaseName.Parse(System.IO.Path.GetFileName(ld), false).Key == ReleaseName.Parse(System.IO.Path.GetFileName(d), false).Key) ?
                    string.Join(", ", localDirs.Where(ld => ReleaseName.Parse(System.IO.Path.GetFileName(ld), false).Key == ReleaseName.Parse(System.IO.Path.GetFileName(d), false).Key).Select(System.IO.Path.GetFileName)) : "not installed"))) : p.Path;
                rows.Add(new Entry { Available = p, Category = Categories.Detect(p), Name = p.Label, AvailableVersion = p.Release.Version == "" ? "Content comparison" : p.Release.Version,
                    InstalledVersion = old.Count == 0 && !File.Exists(manifest) ? "—" : old.Count == 0 ? "Managed copy" : string.Join(", ", old.Select(x => x.Release.Version == "" ? "Unversioned" : x.Release.Version)),
                    Status = state, Actionable = state != "Local version newer" && state != "Ambiguous source versions" && state != "Registration scripts missing", Details = details });
            }
            foreach (var p in installed.Where(x => !available.Any(a => a.Key == x.Key)))
                rows.Add(new Entry { PackageKey = p.Key, Category = Categories.Detect(p), Name = p.Label, InstalledVersion = p.Release.Version, AvailableVersion = "—", Status = "Local only", Details = p.Path });
            return rows.OrderBy(e => e.Name).ToList();
        }
    }
    internal interface IRegistrar { void Run(string directory, bool unregister); void CheckClosed(); }
    internal sealed class ScriptRegistrar : IRegistrar
    {
        public void CheckClosed()
        {
            var processes = Process.GetProcessesByName("Edge");
            try { if (processes.Length > 0) throw new IOException("Close Solid Edge before installing add-ins."); }
            finally { foreach (var p in processes) p.Dispose(); }
        }
        public void Run(string directory, bool unregister)
        {
            CheckClosed();
            string name = unregister ? "UNRegister.cmd" : "Register.cmd";
            string script = System.IO.Path.Combine(directory, name);
            Diagnostics.Info("REGISTRATION START script=" + script + " elevation=requested");
            if (!File.Exists(script)) throw new IOException("Required registration script missing: " + script);
            // Run the vendor's script in its own directory, elevated. No guessed RegAsm/CLSID settings.
            var info = new ProcessStartInfo(script) { WorkingDirectory = directory, UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Normal };
            using (var process = Process.Start(info))
            {
                process.WaitForExit();
                Diagnostics.Info("REGISTRATION EXIT script=" + script + " exitCode=" + process.ExitCode);
                if (process.ExitCode != 0) throw new IOException(name + " failed with exit code " + process.ExitCode + ".");
            }
        }
    }
    internal static class Installer
    {
        public static void ValidateRoots(string source, string local)
        {
            Files.NoLinks(source); Files.NoLinks(local);
            if (Files.Full(source).Equals(Files.Full(local), StringComparison.OrdinalIgnoreCase) || Files.Inside(source, local) || Files.Inside(local, source))
                throw new IOException("Source and local folders must be separate and must not contain each other.");
            if (Files.Full(local).Equals(Files.Full(System.IO.Path.GetPathRoot(local)), StringComparison.OrdinalIgnoreCase)) throw new IOException("Select a dedicated local Add-ins folder, not a drive root.");
        }
        static void Extract(string zip, string root)
        {
            using (var archive = ZipFile.OpenRead(zip))
            {
                long total = 0;
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in archive.Entries)
                {
                    string relative = e.FullName.Replace('/', '\\');
                    if (relative.Contains(":")) throw new IOException("Invalid ZIP entry.");
                    string target = Files.Child(root, relative);
                    if (!names.Add(target)) throw new IOException("Duplicate ZIP path: " + relative);
                    total = checked(total + e.Length);
                    if (total > 4L * 1024 * 1024 * 1024) throw new IOException("ZIP expands beyond the 4 GB package limit.");
                    if (relative.EndsWith("\\")) Directory.CreateDirectory(target);
                    else { Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)); e.ExtractToFile(target); }
                }
            }
        }
        public static string Install(Package p, string source, string local, IRegistrar registrar)
        {
            Diagnostics.Info("INSTALL START package=" + p.Label + " version=" + p.Release.Version + " source=" + source + " local=" + local);
            var timer = Stopwatch.StartNew();
            try
            {
                ValidateRoots(source, local);
                Directory.CreateDirectory(local);
                string control = Files.Child(local, ".se-manager"); Directory.CreateDirectory(control);
                // A file lock works across processes and users; release it deterministically on all exits.
                using (var gate = new FileStream(Files.Child(control, "install.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    string result = InstallLocked(p, source, local, registrar);
                    Diagnostics.Info("INSTALL SUCCESS package=" + p.Label + " elapsedMs=" + timer.ElapsedMilliseconds + " result=" + result);
                    return result;
                }
            }
            catch (Exception ex) { Diagnostics.Error("INSTALL FAILED package=" + p.Label + " elapsedMs=" + timer.ElapsedMilliseconds, ex); throw; }
        }
        static string InstallLocked(Package p, string source, string local, IRegistrar registrar)
        {
            ValidateRoots(source, local);
            if (!Files.Inside(source, p.Path) && !(p.Bundle && Files.Full(source).Equals(Files.Full(p.Path), StringComparison.OrdinalIgnoreCase)))
                throw new IOException("Package is no longer in the selected source folder.");
            if (Catalog.Scan(local, false).Any(x => x.Key == p.Key && x.Release.Parts != null && p.Release.Parts != null && ReleaseName.Compare(x.Release, p.Release) > 0))
                throw new IOException("A newer local version exists; downgrades are not allowed.");
            Directory.CreateDirectory(local);
            string control = Files.Child(local, ".se-manager"); Directory.CreateDirectory(control);
            // Persist failed recovery data and refuse a new transaction until it is resolved.
            if (Directory.GetDirectories(control, "transaction-*").Length > 0) throw new IOException("An unfinished transaction exists in " + control + ". Recover it before another install.");
            string txn = System.IO.Path.Combine(control, "transaction-" + Guid.NewGuid().ToString("N"));
            string stage = System.IO.Path.Combine(txn, "new"), backup = System.IO.Path.Combine(txn, "backup");
            Directory.CreateDirectory(stage); Directory.CreateDirectory(backup);
            Diagnostics.Info("TRANSACTION CREATED " + txn);
            var touched = new List<string>(); var moved = new List<string>(); var applied = new List<string>();
            string regDir = null; string oldRegDir = null; bool oldUnregistered = false, registrationAttempted = false, committed = false;
            var recovery = new XElement("recovery", new XAttribute("local", local));
            Action journal = () => new XDocument(recovery).Save(System.IO.Path.Combine(txn, "recovery.xml"));
            try
            {
                if (p.Kind == "ZIP") Extract(p.Path, Files.Child(stage, System.IO.Path.GetFileNameWithoutExtension(p.Path)));
                else foreach (var pair in Catalog.SourceFiles(p)) Files.Copy(pair.Value, Files.Child(stage, pair.Key));
                var payload = Files.Tree(stage).Select(f => Files.Relative(stage, f)).ToList();
                Diagnostics.Info("STAGING COMPLETE transaction=" + txn + " files=" + payload.Count);
                if (payload.Count == 0) throw new IOException("Package is empty.");
                var scripts = payload.Where(f => System.IO.Path.GetFileName(f).Equals("Register.cmd", StringComparison.OrdinalIgnoreCase)).ToList();
                bool addin = p.Bundle || scripts.Count > 0 || payload.Any(f => System.IO.Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase)) && !payload.Any(f => System.IO.Path.GetExtension(f).Equals(".exe", StringComparison.OrdinalIgnoreCase));
                if (addin)
                {
                    // A root script owns the whole suite; otherwise accept a single unambiguous registrar.
                    string selected = scripts.FirstOrDefault(f => System.IO.Path.GetDirectoryName(f) == "");
                    if (selected == null && scripts.Count == 1) selected = scripts[0];
                    if (selected == null) throw new IOException("Provide a package Register.cmd (one root script for a suite). No registration commands were guessed.");
                    regDir = System.IO.Path.GetDirectoryName(Files.Child(local, selected));
                    string stagedReg = System.IO.Path.GetDirectoryName(Files.Child(stage, selected));
                    if (!Catalog.Script(stagedReg, "UNRegister.cmd")) throw new IOException("Package must include UNRegister.cmd for recovery and future updates.");
                    registrar.CheckClosed();
                }
                var older = Catalog.Scan(local, false).Where(x => x.Key == p.Key && x.Release.Parts != null && p.Release.Parts != null && ReleaseName.Compare(x.Release, p.Release) < 0).ToList();
                if (p.Bundle)
                {
                    // Legacy shared suites have individually versioned component directories.
                    foreach (string next in Directory.GetDirectories(p.Path).Where(d=>!Files.ExcludedFolder(d)))
                    {
                        var nextName = ReleaseName.Parse(System.IO.Path.GetFileName(next), false);
                        if (nextName.Parts == null) continue;
                        foreach (string previous in Directory.GetDirectories(local))
                        {
                            var oldName = ReleaseName.Parse(System.IO.Path.GetFileName(previous), false);
                            if (oldName.Parts != null && oldName.Key == nextName.Key && ReleaseName.Compare(oldName, nextName) < 0 && !Directory.Exists(System.IO.Path.Combine(p.Path, System.IO.Path.GetFileName(previous))))
                                older.Add(new Package { Path = previous, Release = oldName });
                            if (oldName.Parts != null && oldName.Key == nextName.Key && ReleaseName.Compare(oldName, nextName) > 0)
                                throw new IOException("Local add-in component is newer: " + previous);
                        }
                    }
                }
                if (addin)
                {
                    var oldCandidates = p.Bundle ? new List<string>() : older.Where(x => Directory.Exists(x.Path)).SelectMany(x => Files.Tree(x.Path).Where(f => System.IO.Path.GetFileName(f).Equals("UNRegister.cmd", StringComparison.OrdinalIgnoreCase)).Select(System.IO.Path.GetDirectoryName)).ToList();
                    if (Directory.Exists(regDir) && Catalog.Script(regDir, "UNRegister.cmd")) oldCandidates.Add(regDir);
                    oldCandidates = oldCandidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    if (oldCandidates.Count > 1) throw new IOException("Multiple old add-in registrars found. Resolve registration before updating.");
                    oldRegDir = oldCandidates.FirstOrDefault();
                    if (oldRegDir == null && payload.Any(f => File.Exists(Files.Child(local, f)) && System.IO.Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase)))
                        throw new IOException("Existing add-in DLLs lack UNRegister.cmd. Restore the old script before updating.");
                }
                // Track each affected path before mutation. Existing data is moved into the recovery area.
                foreach (var old in older) touched.Add(Files.Relative(local, old.Path));
                touched.AddRange(payload.Where(f => !touched.Any(t => f.StartsWith(t + "\\", StringComparison.OrdinalIgnoreCase))));
                string manifestRel = System.IO.Path.Combine(".se-manager", p.Key + ".xml"); touched.Add(manifestRel);
                string priorManifest = Files.Child(local, manifestRel);
                if (File.Exists(priorManifest))
                {
                    foreach (var f in XDocument.Load(priorManifest).Root.Elements("file"))
                    {
                        string r = (string)f.Attribute("path"); Files.Child(local, r);
                        if(p.Kind!="ZIP" && Files.ExcludedRelativeFile(r))continue;
                        // Delete only unchanged files previously owned by this package. Preserve user edits.
                        if (!payload.Contains(r, StringComparer.OrdinalIgnoreCase) && !touched.Any(t => r.StartsWith(t + "\\", StringComparison.OrdinalIgnoreCase)) &&
                            File.Exists(Files.Child(local, r)) && Files.Hash(Files.Child(local, r)) == (string)f.Attribute("hash")) touched.Add(r);
                    }
                }
                foreach (string r in touched.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    string dest = Files.Child(local, r);
                    if (Directory.Exists(dest)) Files.Tree(dest).ToArray();
                    if (File.Exists(dest)) using (File.Open(dest, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                    recovery.Add(new XElement("path", new XAttribute("name", r), new XAttribute("existed", File.Exists(dest) || Directory.Exists(dest))));
                }
                journal();
                Diagnostics.Info("RECOVERY JOURNAL SAVED transaction=" + txn);
                if (oldRegDir != null) { oldUnregistered = true; registrar.Run(oldRegDir, true); }
                foreach (string r in touched.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    string dest = Files.Child(local, r), save = Files.Child(backup, r);
                    if (File.Exists(dest) || Directory.Exists(dest))
                    {
                        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(save));
                        if (Directory.Exists(dest)) Directory.Move(dest, save); else File.Move(dest, save);
                        moved.Add(r);
                        Diagnostics.Info("BACKUP MOVED path=" + dest + " backup=" + save);
                    }
                }
                foreach (string r in payload) { applied.Add(r); Files.Copy(Files.Child(stage, r), Files.Child(local, r)); }
                if (regDir != null) { registrationAttempted = true; registrar.Run(regDir, false); }
                var manifest = new XElement("package", new XAttribute("version", p.Release.Version), new XAttribute("sourceHash", p.Kind == "ZIP" ? Files.Hash(p.Path) : ""),
                    payload.Select(r => new XElement("file", new XAttribute("path", r), new XAttribute("hash", Files.Hash(Files.Child(local, r))))));
                applied.Add(manifestRel);
                new XDocument(manifest).Save(Files.Child(local, manifestRel));
                committed = true;
                Diagnostics.Info("TRANSACTION COMMITTED " + txn);
            }
            catch (Exception error)
            {
                Diagnostics.Error("TRANSACTION FAILED; starting rollback " + txn, error);
                try
                {
                    if (registrationAttempted) registrar.Run(regDir, true);
                    foreach (string r in applied) { string f = Files.Child(local, r); if (File.Exists(f)) File.Delete(f); }
                    foreach (string r in moved.AsEnumerable().Reverse())
                    {
                        string dest = Files.Child(local, r), save = Files.Child(backup, r);
                        if (File.Exists(dest)) File.Delete(dest);
                        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest));
                        if (Directory.Exists(save)) Directory.Move(save, dest); else File.Move(save, dest);
                        Diagnostics.Info("ROLLBACK RESTORED " + dest);
                    }
                    if (oldUnregistered) registrar.Run(oldRegDir, false);
                    Files.RemoveTree(control, txn);
                    Diagnostics.Info("ROLLBACK SUCCESS " + txn);
                }
                catch (Exception rollback) { Diagnostics.Error("ROLLBACK FAILED; preserved recovery files " + txn, rollback); throw new IOException(error.Message + "\r\nRecovery requires attention: " + rollback.Message + "\r\nPreserved files: " + txn, error); }
                throw;
            }
            if (committed)
            {
                try { Files.RemoveTree(control, txn); Diagnostics.Info("CLEANUP SUCCESS; obsolete backups removed " + txn); }
                catch (Exception ex) { Diagnostics.Error("CLEANUP FAILED " + txn, ex); return "Installed; recovery cleanup needed: " + txn + " (" + ex.Message + ")"; }
            }
            return regDir == null ? "Installed and verified." : "Installed; registration script completed. Confirm availability in SE Settings > Add-Ins.";
        }
    }
}
