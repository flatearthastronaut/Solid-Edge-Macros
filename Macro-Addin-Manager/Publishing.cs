using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace CoopManager
{
    internal static class BuildCatalog
    {
        static IEnumerable<string> OutputFolders(string root)
        {
            if(Files.ExcludedFolder(root))yield break;
            Files.NoLinks(root);
            if(Path.GetFileName(root).Equals("Compiled Executables",StringComparison.OrdinalIgnoreCase)) { yield return root;yield break; }
            foreach(string child in Directory.GetDirectories(root))
            {
                // Prune before opening. Never descend into source control, tests, or SDK examples.
                string name=Path.GetFileName(child);
                if(Files.ExcludedFolder(child)||name.StartsWith(".")||name.Equals("tests",StringComparison.OrdinalIgnoreCase)||name.StartsWith("SDK_",StringComparison.OrdinalIgnoreCase))continue;
                foreach(string folder in OutputFolders(child))yield return folder;
            }
        }
        public static List<Package> Scan(string root)
        {
            if(!Directory.Exists(root))throw new IOException("Local builds folder cannot be reached: "+root);
            if(Files.ExcludedFolder(root))return new List<Package>();
            var folders=OutputFolders(root).ToList();
            if(folders.Count==0)folders.Add(root); // Also accept an explicitly selected distribution folder.
            var result=new List<Package>();
            foreach(string folder in folders)
            {
                var packages=Catalog.Scan(folder);
                // A root registration suite is published as one ZIP; registration still happens only
                // when the user installs it. Keep a stable family name instead of "Compiled Executables".
                foreach(var suite in packages.Where(p=>p.Bundle).ToArray())
                {
                    suite.Release=ReleaseName.Parse(Path.GetFileName(Path.GetDirectoryName(folder)),false);
                }
                // Older projects keep distribution ZIPs beside Build.cmd rather than in the output
                // directory. Include only matching families, never unrelated documents/source ZIPs.
                if(Path.GetFileName(folder).Equals("Compiled Executables",StringComparison.OrdinalIgnoreCase))
                    foreach(string zip in Directory.GetFiles(Path.GetDirectoryName(folder),"*.zip"))
                    {
                        var release=ReleaseName.Parse(Path.GetFileName(zip),true);
                        if(packages.Any(p=>p.Release.Key==release.Key))packages.Add(new Package{Path=zip,Release=release,Kind="ZIP"});
                    }
                foreach(var family in packages.GroupBy(p=>p.Release.Key))
                {
                    var latest=family.OrderByDescending(p=>p.Release,Comparer<ReleaseName>.Create(ReleaseName.Compare)).First();
                    var candidates=family.Where(p=>ReleaseName.Compare(p.Release,latest.Release)==0).ToList();
                    if(candidates.Any(p=>p.Kind=="ZIP"))candidates=candidates.Where(p=>p.Kind=="ZIP").ToList();
                    // Identical copies of the same ZIP in project/output folders are harmless.
                    if(candidates.Count>1 && candidates.All(p=>p.Kind=="ZIP") && candidates.Select(p=>Files.Hash(p.Path)).Distinct().Count()==1)
                        candidates=candidates.Take(1).ToList();
                    result.AddRange(candidates);
                }
            }
            return result;
        }
        public static List<Entry> Compare(string builds,string shared)
        {
            Installer.ValidateRoots(builds,shared);
            if(!Directory.Exists(shared))throw new IOException("Shared publish folder cannot be reached: "+shared);
            var available=Scan(builds);var published=Catalog.Scan(shared);var rows=new List<Entry>();
            foreach(var family in available.GroupBy(p=>p.Release.Key))
            {
                var p=family.OrderByDescending(x=>x.Release,Comparer<ReleaseName>.Create(ReleaseName.Compare)).First();
                var same=family.Where(x=>ReleaseName.Compare(x.Release,p.Release)==0).ToList();
                var old=published.Where(x=>x.Key==p.Release.Key).OrderByDescending(x=>x.Release,Comparer<ReleaseName>.Create(ReleaseName.Compare)).ToList();
                string state=old.Count==0?"Not published":ReleaseName.Compare(old[0].Release,p.Release)>0?"Shared version newer":ReleaseName.Compare(old[0].Release,p.Release)<0?"New build available":"Published";
                if(state=="Published")
                {
                    string target=Files.Child(shared,Publisher.OutputName(p));
                    if(!File.Exists(target) || old.Any(x=>ReleaseName.Compare(x.Release,p.Release)==0 && !Files.Full(x.Path).Equals(Files.Full(target),StringComparison.OrdinalIgnoreCase)))state="Conflicting shared format";
                    else if(!Publisher.Matches(p,target))state="Published content differs";
                }
                if(same.Count>1)state="Ambiguous local builds";
                rows.Add(new Entry{Available=p,PackageKey=p.Release.Key,Name=p.Release.Name,Category=Categories.Detect(p),AvailableVersion=p.Release.Version==""?"Unversioned":p.Release.Version,
                    InstalledVersion=old.Count==0?"—":string.Join(", ",old.Select(x=>x.Release.Version==""?"Unversioned":x.Release.Version)),Status=state,
                    Actionable=state=="Not published"||state=="New build available"||state=="Published content differs",
                    Details=p.Path+"\r\nPublish to: "+Path.Combine(shared,Publisher.OutputName(p))+(same.Count>1?"\r\nResolve duplicate build sources:\r\n"+string.Join("\r\n",same.Select(x=>x.Path)):"")});
            }
            foreach(var family in published.Where(p=>!available.Any(a=>a.Release.Key==p.Key)).GroupBy(p=>p.Key))
            {
                var p=family.OrderByDescending(x=>x.Release,Comparer<ReleaseName>.Create(ReleaseName.Compare)).First();
                rows.Add(new Entry{PackageKey=p.Key,Name=p.Label,Category=Categories.Detect(p),AvailableVersion="—",InstalledVersion=p.Release.Version,Status="Shared only",Details=p.Path});
            }
            return rows;
        }
    }
    internal static class Publisher
    {
        const string ControlFolder=".se-publisher";
        public static IDisposable Lock(string shared)
        {
            if(!Directory.Exists(shared))throw new IOException("Shared publish folder cannot be reached: "+shared);
            string control=Files.Child(shared,ControlFolder);Directory.CreateDirectory(control);
            var gate=new FileStream(Files.Child(control,"publish.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            try
            {
                if(Directory.GetDirectories(control,"transaction-*").Length>0)throw new IOException("An interrupted publication needs recovery in "+control+". Review recovery.xml and backups before publishing again.");
                return gate;
            }
            catch{gate.Dispose();throw;}
        }
        public static string OutputName(Package p)
        {
            if(p.Bundle)return p.Release.Name+(p.Release.Version==""?"":"-v"+p.Release.Version)+".zip";
            return Path.GetFileName(p.Path)+(Directory.Exists(p.Path)?".zip":"");
        }
        static Dictionary<string,string> Payload(Package p)
        {
            if(p.Bundle)return Catalog.SourceFiles(p);
            return Files.SourceTree(p.Path).ToDictionary(f=>Files.Relative(p.Path,f),f=>f,StringComparer.OrdinalIgnoreCase);
        }
        public static bool Matches(Package p,string target)
        {
            if(!Directory.Exists(p.Path))return Files.Hash(p.Path)==Files.Hash(target);
            var files=Payload(p);
            using(var archive=ZipFile.OpenRead(target))
            {
                var entries=archive.Entries.Where(e=>!string.IsNullOrEmpty(e.Name)).ToList();
                if(entries.Count!=files.Count)return false;
                var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach(var entry in entries)
                {
                    string relative=entry.FullName.Replace('/', '\\'),file;
                    if(!seen.Add(relative)||!files.TryGetValue(relative,out file))return false;
                    using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=entry.Open())
                        if(BitConverter.ToString(sha.ComputeHash(stream))!=Files.Hash(file))return false;
                }
                return true;
            }
        }
        static void Stage(Package p,string target)
        {
            if(!Directory.Exists(p.Path)){Files.Copy(p.Path,target);return;}
            var files=Payload(p);
            if(files.Count==0)throw new IOException("Build contains no publishable files.");
            using(var archive=ZipFile.Open(target,ZipArchiveMode.Create))
                foreach(var pair in files.OrderBy(x=>x.Key,StringComparer.OrdinalIgnoreCase))
                    archive.CreateEntryFromFile(pair.Value,pair.Key.Replace('\\','/'),CompressionLevel.Optimal);
            if(!Matches(p,target))throw new IOException("Build changed during ZIP creation. Build again and refresh.");
        }
        public static void Publish(Package p,string builds,string shared,string category)
        {
            Installer.ValidateRoots(builds,shared);
            if(!Files.Inside(builds,p.Path) && !Files.Full(builds).Equals(Files.Full(p.Path),StringComparison.OrdinalIgnoreCase))throw new IOException("Build is outside the selected local folder.");
            // Revalidate stale rows at execution time, including Exclude and duplicate build sources.
            var current=BuildCatalog.Scan(builds).Where(x=>x.Release.Key==p.Release.Key).ToList();
            if(current.Any(x=>ReleaseName.Compare(x.Release,p.Release)>0) || current.Count(x=>ReleaseName.Compare(x.Release,p.Release)==0)!=1 ||
                !current.Any(x=>Files.Full(x.Path).Equals(Files.Full(p.Path),StringComparison.OrdinalIgnoreCase)))throw new IOException("Build selection changed. Refresh before publishing.");
            using(Lock(shared))
            {
                string name=OutputName(p),destination=Files.Child(shared,name);
                var old=Catalog.Scan(shared).Where(x=>x.Key==p.Release.Key).ToList();
                if(old.Any(x=>ReleaseName.Compare(x.Release,p.Release)>0))throw new IOException("A newer shared version already exists.");
                if(old.Any(x=>ReleaseName.Compare(x.Release,p.Release)==0 && !Files.Full(x.Path).Equals(Files.Full(destination),StringComparison.OrdinalIgnoreCase)))
                    throw new IOException("Another distribution format exists for this shared version. Resolve it before publishing.");
                var previous=CategoryPreferences.ReadShared(shared);var next=CategoryPreferences.ReadShared(shared);
                next.Set(new Entry{PackageKey=p.Release.Key},category);
                string control=Files.Child(shared,ControlFolder),txn=Files.Child(control,"transaction-"+Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(txn);
                string staged=Path.Combine(txn,"payload"),backup=Path.Combine(txn,"previous-build"),categoryPath=Files.Child(shared,CategoryPreferences.FileName);
                bool existed=File.Exists(destination),hadCategories=File.Exists(categoryPath),categoryWritten=false,published=false;
                try
                {
                    Stage(p,staged);
                    if(hadCategories)Files.Copy(categoryPath,Path.Combine(txn,"previous-categories.xml"));
                    new XDocument(new XElement("publication",new XAttribute("destination",destination),new XAttribute("previousBuild",existed),
                        new XAttribute("previousCategories",hadCategories),new XAttribute("categoryFile",categoryPath))).Save(Path.Combine(txn,"recovery.xml"));
                    // Publish a complete file by rename/replace on the destination filesystem. A user
                    // can see the previous or new complete build, never a partially copied ZIP/EXE.
                    next.WriteShared(shared);categoryWritten=true;
                    if(existed)File.Replace(staged,destination,backup);else File.Move(staged,destination);
                    published=true;
                    Diagnostics.Info("PUBLISH SUCCESS package="+p.Release.Name+" destination="+destination+" category="+category);
                }
                catch(Exception error)
                {
                    try
                    {
                        // File.Replace can report a metadata/IO failure after creating its backup.
                        // Never delete that only good copy during cleanup; restore it first, or keep
                        // the journal and backup intact when recovery itself cannot complete.
                        if(File.Exists(backup))
                        {
                            if(File.Exists(destination))File.Replace(backup,destination,null);else File.Move(backup,destination);
                        }
                        if(categoryWritten){if(hadCategories)previous.WriteShared(shared);else if(File.Exists(categoryPath))File.Delete(categoryPath);}
                        Files.RemoveTree(control,txn);
                    }
                    catch(Exception recovery){throw new IOException(error.Message+" Recovery required at "+txn+": "+recovery.Message,error);}
                    Diagnostics.Error("PUBLISH FAILED package="+p.Release.Name,error);throw;
                }
                if(published)
                {
                    // An interrupted cleanup keeps its journal and blocks subsequent writes. Earlier
                    // numbered builds elsewhere on the share are intentionally retained for rollback.
                    try{Files.RemoveTree(control,txn);}
                    catch(Exception error){throw new IOException("Build published, but publication cleanup needs attention: "+txn,error);}
                }
            }
        }
    }
}
