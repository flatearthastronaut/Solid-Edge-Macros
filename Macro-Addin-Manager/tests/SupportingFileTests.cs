using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using CoopManager;

internal static partial class Tests
{
    static void LauncherZip(string path, string seed = "[Inch]\n1 2 place\n1 3 place\n")
    {
        Zip(path, "Register.cmd", "UNRegister.cmd", "Hammill.MacroLauncher.dll");
        using(var zip=ZipFile.Open(path,ZipArchiveMode.Update))
        {
            if(seed!=null)using(var writer=new StreamWriter(zip.CreateEntry("Shared Configuration/"+SupportingFiles.Styles).Open()))writer.Write(seed);
            // A second, excluded copy must never become a rollout source.
            using(var writer=new StreamWriter(zip.CreateEntry("Exclude/Shared Configuration/"+SupportingFiles.Styles).Open()))writer.Write("excluded");
        }
    }
    static void SupportingFileCases()
    {
        Case("style-publish-install-refresh",(s,l)=>{
            string build=Path.Combine(s,"Hammill-Macro-Launcher-v0.9.zip"), styles=Path.Combine(l,SupportingFiles.Styles);
            LauncherZip(build);
            var p=BuildCatalog.Scan(s).Single(); Publisher.Publish(p,s,l,Categories.Addins);
            Assert(File.ReadAllText(styles).StartsWith("[Inch]"),"Admin seeds style list at shared root and ignores Exclude copy");
            Assert(Catalog.Scan(l).Count==1,"Supporting TXT does not appear as a separate program");
            Assert(BuildCatalog.Compare(s,l).Single().Status=="Published","Complete publication is current");
            string installed=Path.Combine(Path.GetDirectoryName(s),"i");
            var user=Catalog.Scan(l).Single(); Installer.Install(user,l,installed,new FakeRegistrar());
            Assert(Files.Hash(styles)==Files.Hash(Path.Combine(installed,SupportingFiles.Styles)),"User receives style list at configured Local root");
            Assert(Catalog.Compare(l,installed).Single().Status=="Up to date","Style snapshot included in manifest comparison");
            Write(styles,"[Inch]\nCustom A\nCustom B\n");
            Publisher.Publish(p,s,l,Categories.Addins);
            Assert(File.ReadAllText(styles).Contains("Custom A"),"Republish preserves administrator edits to shared list");
            Assert(Catalog.Compare(l,installed).Single().Status=="Update available","Shared-only text edits trigger same-version local update");
            Installer.Install(user,l,installed,new FakeRegistrar());
            Assert(Files.Hash(styles)==Files.Hash(Path.Combine(installed,SupportingFiles.Styles)),"Local snapshot refreshed from live shared list rather than packaged default");
            File.Delete(Path.Combine(installed,SupportingFiles.Styles));
            Assert(Catalog.Compare(l,installed).Single().Status=="Repair available","Missing local style snapshot offers repair");
            File.Delete(styles);
            Assert(BuildCatalog.Compare(s,l).Single().Status=="Supporting files missing","Already published launcher offers missing-file publication");
            Assert(!Catalog.Compare(l,installed).Single().Actionable,"Missing shared list blocks install with diagnostic status");
            Publisher.Publish(p,s,l,Categories.Addins);
            Installer.Install(user,l,installed,new FakeRegistrar());
            Assert(Catalog.Compare(l,installed).Single().Status=="Up to date","Repair recreates shared and local style lists without version bump");
            string before=File.ReadAllText(Path.Combine(installed,SupportingFiles.Styles));
            Write(styles,"[Inch]\nChanged A\nChanged B\n");
            Throws(()=>Installer.Install(user,l,installed,new FakeRegistrar{Fail=true}),"Registration failure during support refresh reported");
            Assert(File.ReadAllText(Path.Combine(installed,SupportingFiles.Styles))==before,"Install rollback restores previous local supporting file");
        });
        Case("style-publication-rollback",(s,l)=>{
            string build=Path.Combine(s,"Hammill-Macro-Launcher-v0.9.zip");LauncherZip(build);
            string destination=Path.Combine(l,Path.GetFileName(build));File.Copy(build,destination);
            using(var held=new FileStream(destination,FileMode.Open,FileAccess.Read,FileShare.Read))
                Throws(()=>Publisher.Publish(BuildCatalog.Scan(s).Single(),s,l,Categories.Addins),"Locked package causes publication failure after support staging");
            Assert(!File.Exists(Path.Combine(l,SupportingFiles.Styles)),"Failed publication rolls back newly seeded shared file");
            Assert(Files.Hash(destination)==Files.Hash(build),"Failed publication preserves old package");
            Assert(!Directory.GetDirectories(Path.Combine(l,".se-publisher"),"transaction-*").Any(),"Supporting-file rollback cleans transaction");
        });
        Case("style-seed-validation",(s,l)=>{
            string build=Path.Combine(s,"Hammill-Macro-Launcher-v0.9.zip");LauncherZip(build,null);
            Throws(()=>Publisher.Publish(BuildCatalog.Scan(s).Single(),s,l,Categories.Addins),"Excluded seed alone cannot be published");
            Assert(!File.Exists(Path.Combine(l,Path.GetFileName(build))),"Missing required support file blocks entire publication");
            File.Delete(build); LauncherZip(build,new string('x',65537));
            Throws(()=>Publisher.Publish(BuildCatalog.Scan(s).Single(),s,l,Categories.Addins),"Oversized style seed rejected before mutation");
        });
        Case("style-committed-recovery",(s,l)=>{
            string txn=CompletedPublication(l), journal=Path.Combine(txn,"recovery.xml"), styles=Path.Combine(l,SupportingFiles.Styles);
            Write(styles,"original");var document=XDocument.Load(journal);
            document.Root.Add(new XElement("support",new XAttribute("path",styles),new XAttribute("hash",Files.Hash(styles))));document.Save(journal);
            Write(styles,"edited");Throws(()=>{using(Publisher.Lock(l)){}},"Changed shared support file blocks automatic recovery cleanup");
            Assert(File.Exists(Path.Combine(txn,"obsolete-0")),"Changed support file preserves recovery backup");
            File.Delete(styles);Throws(()=>{using(Publisher.Lock(l)){}},"Missing shared support file blocks cleanup");
            document.Root.Element("support").SetAttributeValue("path",Path.Combine(s,SupportingFiles.Styles));document.Save(journal);
            Throws(()=>{using(Publisher.Lock(l)){}},"Support recovery rejects paths outside share");
            document.Root.Element("support").SetAttributeValue("path",styles);document.Save(journal);Write(styles,"original");
            using(Publisher.Lock(l)){};
            Assert(!Directory.Exists(txn) && File.Exists(styles),"Verified supporting file permits committed cleanup without deleting live configuration");
        });
        Case("style-folder-publication",(s,l)=>{
            string folder=Path.Combine(s,"Hammill-Macro-Launcher-v0.9");
            Write(Path.Combine(folder,"Register.cmd"),"register");Write(Path.Combine(folder,"UNRegister.cmd"),"unregister");
            Write(Path.Combine(folder,"Hammill.MacroLauncher.dll"),"library");
            Write(Path.Combine(folder,"Shared Configuration",SupportingFiles.Styles),"[Inch]\nA\nB\n");
            Write(Path.Combine(folder,"Exclude","Shared Configuration",SupportingFiles.Styles),"excluded");
            Publisher.Publish(BuildCatalog.Scan(s).Single(),s,l,Categories.Addins);
            Assert(File.ReadAllText(Path.Combine(l,SupportingFiles.Styles)).Contains("[Inch]"),"Folder distribution seeds shared configuration");
            // Exercise the user installer's folder format as well as its ZIP path.
            Write(Path.Combine(s,SupportingFiles.Styles),"[Inch]\nA\nB\n");
            string local=Path.Combine(l,"i");Installer.Install(Catalog.Scan(s).Single(),s,local,new FakeRegistrar());
            Assert(Catalog.Compare(s,local).Single().Status=="Up to date","Folder support snapshot is not mistaken for an extra package source file");
        });
        Case("real-distribution-support-files",(s,l)=>{
            string repository=Path.GetFullPath("..");
            string launcher=Path.Combine(repository,"Add-ins","Hammill-Macro-Launcher","Compiled Executables","Hammill-Macro-Launcher-v0.9.zip");
            string ring=Path.Combine(repository,"O-Ring Finder","Compiled Executables","O-Ring Finder v1.1.zip");
            File.Copy(launcher,Path.Combine(s,Path.GetFileName(launcher)));File.Copy(ring,Path.Combine(s,Path.GetFileName(ring)));
            foreach(var p in BuildCatalog.Scan(s))Publisher.Publish(p,s,l,p.Key=="oringfinder"?Categories.Macros:Categories.Addins);
            string installed=Path.Combine(Path.GetDirectoryName(s),"i");
            foreach(var p in Catalog.Scan(l))Installer.Install(p,l,installed,new FakeRegistrar());
            Assert(Files.Hash(Path.Combine(l,SupportingFiles.Styles))==Files.Hash(Path.Combine(installed,SupportingFiles.Styles)),"Actual launcher ZIP publishes and installs shared style list");
            string stable=Path.Combine(installed,"O-Ring Finder"), numbered=Path.Combine(installed,"O-Ring Finder v1.1");
            string table=Path.Combine(repository,"O-Ring Finder","Compiled Executables","Oring_table.txt");
            Assert(File.Exists(Path.Combine(stable,"O-Ring Finder.exe")),"Actual O-ring ZIP retains stable ribbon EXE");
            Assert(Files.Hash(table)==Files.Hash(Path.Combine(stable,"Oring_table.txt")) && Files.Hash(table)==Files.Hash(Path.Combine(numbered,"Oring_table.txt")),"Actual O-ring TXT arrives beside stable and versioned executables unchanged");
            Assert(Catalog.Compare(l,installed).All(e=>e.Status=="Up to date"),"Both actual distributions compare current after installation");
            File.Delete(Path.Combine(stable,"Oring_table.txt"));
            Assert(Catalog.Compare(l,installed).Single(e=>e.Available.Key=="oringfinder").Status=="Repair available","Missing stable O-ring table offers repair");
            Installer.Install(Catalog.Scan(l).Single(p=>p.Key=="oringfinder"),l,installed,new FakeRegistrar());
            Assert(Files.Hash(table)==Files.Hash(Path.Combine(stable,"Oring_table.txt")),"Repair restores actual O-ring table");
        });
    }
}
