using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CoopManager
{
    // Stable copies are ordinary files, not launchers or links. They are staged, hashed, backed up,
    // and repaired by the same transaction as the numbered release, including all dependencies.
    internal static class StableMacros
    {
        public static bool Enabled(Package package,string category)
        { return !package.Bundle && category==Categories.Macros; }

        public static Dictionary<string,string> Stage(Package package,string stage,IList<string> originals)
        {
            var copies=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            bool folder=package.Kind=="ZIP" || Directory.Exists(package.Path);
            // Only a wrapper named for this package is normalized. Dependency directories may encode
            // their own versions and must retain those names for the program to find them.
            Func<string,string> stablePath=relative=>{
                if(!folder)return relative;
                var parts=relative.Split('\\','/');parts[0]=package.Release.Name;
                for(int i=1;i<parts.Length-1;i++)
                {var name=ReleaseName.Parse(parts[i],false);if(name.Key==package.Key && name.Parts!=null)parts[i]=name.Name;}
                return Path.Combine(parts);
            };
            Action<string,string> add=(target,source)=>{
                Files.Child(stage,target); // Reject invalid family names before adding any copies.
                if(target.Equals(source,StringComparison.OrdinalIgnoreCase))return;
                string existing;
                if(originals.Contains(target,StringComparer.OrdinalIgnoreCase) && Files.Hash(Files.Child(stage,target))!=Files.Hash(Files.Child(stage,source)))
                    throw new IOException("The package contains conflicting stable and versioned files: "+target);
                if(copies.TryGetValue(target,out existing) && !existing.Equals(source,StringComparison.OrdinalIgnoreCase) && Files.Hash(Files.Child(stage,existing))!=Files.Hash(Files.Child(stage,source)))
                    throw new IOException("Multiple package files map to the same stable path: "+target);
                if(!originals.Contains(target,StringComparer.OrdinalIgnoreCase))copies[target]=source;
            };
            if(folder)foreach(string file in originals)add(stablePath(file),file);
            var executables=originals.Where(f=>Path.GetExtension(f).Equals(".exe",StringComparison.OrdinalIgnoreCase) && !Files.ExcludedRelativeFile(f));
            foreach(var family in executables.GroupBy(f=>Path.Combine(Path.GetDirectoryName(stablePath(f))??"",ReleaseName.Parse(Path.GetFileName(f),true).Key),StringComparer.OrdinalIgnoreCase))
            {
                var ordered=family.OrderByDescending(f=>ReleaseName.Parse(Path.GetFileName(f),true),Comparer<ReleaseName>.Create(ReleaseName.Compare)).ToList();
                string executable=ordered[0];var release=ReleaseName.Parse(Path.GetFileName(executable),true);
                if(release.Parts==null)continue;
                if(ordered.Where(f=>ReleaseName.Compare(ReleaseName.Parse(Path.GetFileName(f),true),release)==0).Select(f=>Files.Hash(Files.Child(stage,f))).Distinct().Count()>1)
                    throw new IOException("Conflicting executables have the same latest version: "+release.Name);
                // A package may ship an outdated unversioned convenience EXE. The stable mirror
                // must always use the newest numbered EXE, not that stale convenience copy.
                string alias=Path.Combine(Path.GetDirectoryName(stablePath(executable))??"",release.Name+".exe");
                copies.Remove(alias);add(alias,executable);
                foreach(string suffix in new[]{".config",".manifest"})
                {
                    string companion=executable+suffix;
                    if(originals.Contains(companion,StringComparer.OrdinalIgnoreCase)){copies.Remove(alias+suffix);add(alias+suffix,companion);}
                }
                foreach(string extension in new[]{".ico",".png"})
                {
                    string companion=Path.ChangeExtension(executable,extension),target=Path.ChangeExtension(alias,extension);
                    if(originals.Contains(companion,StringComparer.OrdinalIgnoreCase)){copies.Remove(target);add(target,companion);}
                }
            }
            foreach(var copy in copies)Files.Copy(Files.Child(stage,copy.Value),Files.Child(stage,copy.Key));
            return copies;
        }
        public static void ValidateTargets(Package package,string local,string stage,Dictionary<string,string> copies,XElement previous)
        {
            var owned=new HashSet<string>(previous==null?new string[0]:previous.Elements("file").Select(f=>(string)f.Attribute("path")),StringComparer.OrdinalIgnoreCase);
            string control=Files.Child(local,".se-manager");
            foreach(string manifest in Directory.GetFiles(control,"*.xml"))
            {
                if(Path.GetFileNameWithoutExtension(manifest).Equals(package.Key,StringComparison.OrdinalIgnoreCase))continue;
                Files.NoLinks(manifest);
                if(XDocument.Load(manifest).Root.Elements("file").Any(f=>copies.ContainsKey((string)f.Attribute("path"))))
                    throw new IOException("A stable macro path belongs to another installed package: "+manifest);
            }
            foreach(var copy in copies)
            {
                string target=Files.Child(local,copy.Key);
                if(Directory.Exists(target))throw new IOException("A folder occupies the stable macro file path: "+target);
                if(!File.Exists(target)||owned.Contains(copy.Key)||Files.Hash(target)==Files.Hash(Files.Child(stage,copy.Key)))continue;
                // Adopt an existing manually-created loose alias only if it matches a numbered
                // version of the same installed macro. Do not overwrite unrelated untracked data.
                bool legacy=Path.GetDirectoryName(copy.Key)=="" && Catalog.Scan(local,false).Any(p=>p.Key==package.Key && p.Release.Parts!=null && File.Exists(p.Path) &&
                    Path.GetExtension(p.Path).Equals(".exe",StringComparison.OrdinalIgnoreCase) && Files.Hash(p.Path)==Files.Hash(target));
                if(!legacy)throw new IOException("An unmanaged file occupies the stable macro path. Move it aside before installing: "+target);
            }
        }
        public static HashSet<string> GeneratedRoots(string local)
        {
            var result=new HashSet<string>(StringComparer.OrdinalIgnoreCase);string control=Files.Child(local,".se-manager");
            if(!Directory.Exists(control))return result;
            foreach(string path in Directory.GetFiles(control,"*.xml"))
            {
                Files.NoLinks(path);var files=XDocument.Load(path).Root.Elements("file").ToList();
                var originals=new HashSet<string>(files.Where(f=>f.Attribute("derivedFrom")==null).Select(f=>((string)f.Attribute("path")).Split('\\','/')[0]),StringComparer.OrdinalIgnoreCase);
                foreach(var file in files.Where(f=>f.Attribute("derivedFrom")!=null))
                {
                    string name=((string)file.Attribute("path")).Split('\\','/')[0];
                    if(!originals.Contains(name))result.Add(Files.Child(local,name));
                }
            }
            return result;
        }
        public static string Details(XElement manifest,string local)
        {
            if(manifest==null || (string)manifest.Attribute("stableCopies")!="1")return "";
            var paths=manifest.Elements("file").Where(f=>Path.GetExtension((string)f.Attribute("path")).Equals(".exe",StringComparison.OrdinalIgnoreCase) &&
                ReleaseName.Parse(Path.GetFileName((string)f.Attribute("path")),true).Parts==null &&
                !(Path.GetDirectoryName((string)f.Attribute("path"))??"").Split('\\','/').Any(v=>ReleaseName.Parse(v,false).Parts!=null))
                .Select(f=>Files.Child(local,(string)f.Attribute("path"))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return paths.Length==0?"":"\r\nStable ribbon target (use the unversioned EXE):\r\n"+string.Join("\r\n",paths);
        }
    }
}
