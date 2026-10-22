using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using CoopManager;

internal static partial class Tests
{
    static void StableMacroCases()
    {
        Case("stable-loose-updates",(s,l)=>{
            Write(Path.Combine(s,"My Macro-v1.0.exe"),"first");var first=Catalog.Scan(s).Single();
            Installer.Install(first,s,l,new FakeRegistrar());
            string alias=Path.Combine(l,"My Macro.exe");
            Assert(File.ReadAllText(alias)=="first" && File.Exists(Path.Combine(l,"My Macro-v1.0.exe")),"Loose macro keeps numbered file and unversioned copy");
            Write(Path.Combine(s,"My Macro-v1.10.exe"),"latest");var next=Catalog.Compare(s,l).Single().Available;
            Installer.Install(next,s,l,new FakeRegistrar());
            Assert(File.ReadAllText(alias)=="latest" && !File.Exists(Path.Combine(l,"My Macro-v1.0.exe")),"Same ribbon path updated while old numbered file removed");
            var entry=Catalog.Compare(s,l).Single();
            Assert(entry.Status=="Up to date" && entry.InstalledVersion=="1.10","Stable copy does not create duplicate version or perpetual update");
            Assert(entry.Details.Contains(alias),"Catalog shows the stable ribbon target");
            Write(alias,"damaged");Assert(Catalog.Compare(s,l).Single().Status=="Repair available","Corrupt alias detected");
            Installer.Install(next,s,l,new FakeRegistrar());Assert(File.ReadAllText(alias)=="latest","Alias repaired from numbered release");
            File.Delete(alias);Assert(Catalog.Compare(s,l).Single().Status=="Repair available","Missing alias detected");
            Installer.Install(next,s,l,new FakeRegistrar());
            using(var locked=new FileStream(alias,FileMode.Open,FileAccess.Read,FileShare.None))
            {
                Write(Path.Combine(s,"My Macro-v2.0.exe"),"next");
                Throws(()=>Installer.Install(Catalog.Scan(s).Single(p=>p.Release.Version=="2.0"),s,l,new FakeRegistrar()),"Running stable EXE blocks replacement");
            }
            Assert(File.ReadAllText(alias)=="latest" && File.Exists(Path.Combine(l,"My Macro-v1.10.exe")) && !File.Exists(Path.Combine(l,"My Macro-v2.0.exe")),"Failed update preserves stable and versioned copies");
        });
        Case("stable-folder-dependencies",(s,l)=>{
            string folder=Path.Combine(s,"Macro-v1.0");
            Write(Path.Combine(folder,"Macro-v1.0.exe"),"first");Write(Path.Combine(folder,"Macro-v1.0.exe.config"),"config1");Write(Path.Combine(folder,"helper.dll"),"dll1");Write(Path.Combine(folder,"old.txt"),"obsolete");
            Write(Path.Combine(folder,"Exclude","hidden.exe"),"hidden");Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar());
            string stable=Path.Combine(l,"Macro");
            Assert(File.ReadAllText(Path.Combine(stable,"Macro.exe"))=="first" && File.ReadAllText(Path.Combine(stable,"Macro.exe.config"))=="config1","Stable folder has renamed executable and matching configuration");
            Assert(!Directory.Exists(Path.Combine(stable,"Exclude")),"Stable folder respects source Exclude pruning");
            string folder2=Path.Combine(s,"Macro-v2.0");Write(Path.Combine(folder2,"Macro-v2.0.exe"),"second");Write(Path.Combine(folder2,"Macro-v2.0.exe.config"),"config2");Write(Path.Combine(folder2,"helper.dll"),"dll2");
            Installer.Install(Catalog.Compare(s,l).Single().Available,s,l,new FakeRegistrar());
            Assert(File.ReadAllText(Path.Combine(stable,"Macro.exe"))=="second" && File.ReadAllText(Path.Combine(stable,"helper.dll"))=="dll2","Stable executable and dependencies update together");
            Assert(!Directory.Exists(Path.Combine(l,"Macro-v1.0")) && !File.Exists(Path.Combine(stable,"Macro-v1.0.exe")) && !File.Exists(Path.Combine(stable,"old.txt")),"Old numbered folder and unchanged obsolete stable files removed");
            Assert(File.Exists(Path.Combine(l,"Macro-v2.0","Macro-v2.0.exe")) && Catalog.Compare(s,l).Single().Status=="Up to date","Original numbered folder retained for reference without repeated updates");
        });
        Case("stable-zip-nesting",(s,l)=>{
            Zip(Path.Combine(s,"Macro-v1.0.zip"),"Macro-v1.0/Compiled Executables/Macro-v1.0.exe","Macro-v1.0/Compiled Executables/Macro-v1.0.exe.config","Macro-v1.0/Compiled Executables/template.txt");
            Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar());
            string target=Path.Combine(l,"Macro","Macro","Compiled Executables","Macro.exe");
            Assert(File.Exists(target),"Versioned ZIP wrapper and EXE converted to stable path");
            Zip(Path.Combine(s,"Macro-v2.0.zip"),"Macro-v2.0/Compiled Executables/Macro-v2.0.exe","Macro-v2.0/Compiled Executables/Macro-v2.0.exe.config","Macro-v2.0/Compiled Executables/template.txt");
            Installer.Install(Catalog.Compare(s,l).Single().Available,s,l,new FakeRegistrar());
            Assert(File.ReadAllText(target).Contains("v2.0") && File.ReadAllText(target+".config").Contains("v2.0"),"Nested stable ribbon path and configuration survive ZIP version changes");
            Assert(!Directory.Exists(Path.Combine(l,"Macro-v1.0")) && Catalog.Compare(s,l).Single().Details.Contains(target),"Nested original release cleaned and stable target shown");
        });
        Case("stable-migration",(s,l)=>{
            Write(Path.Combine(s,"Macro-v1.0.exe"),"version");var p=Catalog.Scan(s).Single();Installer.Install(p,s,l,new FakeRegistrar());
            string manifest=Path.Combine(l,".se-manager",p.Key+".xml");var doc=XDocument.Load(manifest);
            doc.Root.Attribute("stableCopies").Remove();doc.Root.Elements("file").Where(f=>f.Attribute("derivedFrom")!=null).Remove();doc.Save(manifest);File.Delete(Path.Combine(l,"Macro.exe"));
            Assert(Catalog.Compare(s,l).Single().Status=="Repair available","Existing 1.10 installation offers stable-copy migration via Update all installed");
            Installer.Install(p,s,l,new FakeRegistrar());Assert(File.Exists(Path.Combine(l,"Macro.exe")) && Catalog.Compare(s,l).Single().Status=="Up to date","One-time migration completes without changing version");
        });
        Case("stable-collisions",(s,l)=>{
            Write(Path.Combine(s,"Macro-v1.0.exe"),"build");Write(Path.Combine(l,"Macro.exe"),"unrelated");var p=Catalog.Scan(s).Single();
            Throws(()=>Installer.Install(p,s,l,new FakeRegistrar()),"Unmanaged conflicting stable file rejected");
            Assert(File.ReadAllText(Path.Combine(l,"Macro.exe"))=="unrelated" && !File.Exists(Path.Combine(l,"Macro-v1.0.exe")),"Unmanaged file and install state preserved");
            Write(Path.Combine(l,"Macro-v0.9.exe"),"unrelated");Installer.Install(p,s,l,new FakeRegistrar());
            Assert(File.ReadAllText(Path.Combine(l,"Macro.exe"))=="build","Existing loose convenience alias adopted when it matches an old numbered macro");
        });
        Case("stable-categories",(s,l)=>{
            Write(Path.Combine(s,"Tool-v1.0.exe"),"program");var p=Catalog.Scan(s).Single();
            CategoryPreferences.AssignShared(s,new Entry{Available=p},Categories.Windows);Installer.Install(p,s,l,new FakeRegistrar());
            Assert(!File.Exists(Path.Combine(l,"Tool.exe")) && Catalog.Compare(s,l).Single().Status=="Up to date","Windows category does not create macro aliases");
            string addin=Path.Combine(s,"Addin-v1.0");Write(Path.Combine(addin,"Register.cmd"),"register");Write(Path.Combine(addin,"UNRegister.cmd"),"unregister");Write(Path.Combine(addin,"Addin-v1.0.exe"),"helper");
            var a=Catalog.Scan(s).Single(x=>x.Release.Name=="Addin");CategoryPreferences.AssignShared(s,new Entry{Available=a},Categories.Macros);
            Installer.Install(a,s,l,new FakeRegistrar());Assert(!Directory.Exists(Path.Combine(l,"Addin")),"Registration packages never receive macro mirrors even when miscategorized");
            Assert(Catalog.Compare(s,l).Single(e=>e.Name=="Addin").Status=="Up to date","Mislabeled add-in does not enter stable-copy repair loop");
        });
        Case("stable-shipped-alias",(s,l)=>{
            string folder=Path.Combine(s,"Macro-v2.0");Write(Path.Combine(folder,"Macro-v2.0.exe"),"latest");Write(Path.Combine(folder,"Macro.exe"),"stale packaged alias");
            Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar());
            Assert(File.ReadAllText(Path.Combine(l,"Macro","Macro.exe"))=="latest","Stable copy always comes from latest numbered executable");
            Assert(File.ReadAllText(Path.Combine(l,"Macro-v2.0","Macro.exe"))=="stale packaged alias","Original release payload stays intact");
            Assert(Catalog.Compare(s,l).Single().Status=="Up to date","Shipped convenience alias does not confuse source comparison");
        });
        Case("stable-other-owner",(s,l)=>{
            Write(Path.Combine(s,"Macro-v1.0.exe"),"build");Write(Path.Combine(l,"Macro.exe"),"build");
            string metadata=Path.Combine(l,".se-manager");Directory.CreateDirectory(metadata);
            new XDocument(new XElement("package",new XElement("file",new XAttribute("path","Macro.exe")))).Save(Path.Combine(metadata,"other.xml"));
            Throws(()=>Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar()),"Stable target owned by another package cannot be claimed");
            Assert(File.ReadAllText(Path.Combine(l,"Macro.exe"))=="build","Other package file preserved");
        });
        Case("stable-ambiguous-exes",(s,l)=>{
            string folder=Path.Combine(s,"Macro-v1.0");Write(Path.Combine(folder,"Macro-v1.0.exe"),"one");Write(Path.Combine(folder,"Macro-v1.0.0.exe"),"two");
            Throws(()=>Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar()),"Conflicting equally-versioned executables cannot supply stable copy");
            Assert(!Directory.Exists(Path.Combine(l,"Macro")),"Ambiguous package does not partially install stable copy");
        });
        foreach(string path in new[]{@"..\Bolt Circle Holes\Compiled Executables\BoltCircleHoles-v0.20.zip",@"..\SketchToModels-v2.32\Compiled Executables\SketchToModels-v2.32.zip"})
        {
            if(!File.Exists(path))continue;
            Case("stable-real-"+Path.GetFileNameWithoutExtension(path),(s,l)=>{
                File.Copy(path,Path.Combine(s,Path.GetFileName(path)));var p=Catalog.Scan(s).Single();Installer.Install(p,s,l,new FakeRegistrar());
                var stableExes=Files.Tree(Path.Combine(l,p.Release.Name)).Where(f=>Path.GetFileName(f).Equals(p.Release.Name+".exe",StringComparison.OrdinalIgnoreCase)).ToList();
                Assert(stableExes.Count>0 && Catalog.Compare(s,l).Single().Status=="Up to date","Real macro ZIP receives stable runnable copy");
                Assert(Catalog.Compare(s,l).Single().Details.Contains(stableExes[0]),"Real macro stable path shown in details");
            });
        }
    }
}
