using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using CoopManager;

internal static partial class Tests
{
    static void WaitUi(Task task)
    {
        var timeout=System.Diagnostics.Stopwatch.StartNew();
        while(!task.IsCompleted){Application.DoEvents();System.Threading.Thread.Sleep(10);if(timeout.Elapsed.TotalSeconds>30)throw new Exception("UI operation timed out");}
        task.GetAwaiter().GetResult();Application.DoEvents();
    }
    static void Configure(ManagerForm form,string source,string destination)
    {
        Descendants(form).OfType<TextBox>().Single(t=>t.Name=="SourcePath").Text=source;
        Descendants(form).OfType<TextBox>().Single(t=>t.Name=="DestinationPath").Text=destination;
        form.ShowInTaskbar=false;form.Opacity=0;form.Show();Application.DoEvents();
        WaitUi(form.RefreshList());
    }
    static DataGridViewRow Row(ManagerForm form,string name)
    { return Descendants(form).OfType<DataGridView>().SelectMany(g=>g.Rows.Cast<DataGridViewRow>()).Single(r=>((Entry)r.Tag).Name==name); }
    static void Zip(string path,params string[] names)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using(var zip=ZipFile.Open(path,ZipArchiveMode.Create))foreach(string name in names)
            using(var writer=new StreamWriter(zip.CreateEntry(name).Open()))writer.Write("content "+name);
    }
    static void PublishingCases()
    {
        Case("publish-discovery",(s,l)=>{
            string output=Path.Combine(s,"Project","Compiled Executables");
            Write(Path.Combine(output,"Macro-v1.9.exe"),"old");Write(Path.Combine(output,"Macro-v1.10.exe"),"new");Write(Path.Combine(output,"Macro.exe"),"alias");
            Zip(Path.Combine(output,"Macro-v1.10.zip"),"Macro.exe","template.txt");
            File.Copy(Path.Combine(output,"Macro-v1.10.zip"),Path.Combine(s,"Project","Macro-v1.10.zip"));
            Write(Path.Combine(s,"eXcLuDe","Project","Compiled Executables","Hidden-v9.0.exe"),"excluded");
            Write(Path.Combine(output,"EXCLUDE","Hidden.exe"),"excluded");
            Write(Path.Combine(s,"SDK_examples","Compiled Executables","Example.exe"),"sdk");
            var entry=BuildCatalog.Compare(s,l).Single();
            Assert(entry.Available.Kind=="ZIP" && entry.AvailableVersion=="1.10","Latest numeric release, ZIP preference, identical duplicate ZIPs deduplicated");
            Assert(entry.Status=="Not published" && entry.Actionable,"Unpublished release selectable");
            Publisher.Publish(entry.Available,s,l,Categories.Windows);
            Assert(File.Exists(Path.Combine(l,"Macro-v1.10.zip")) && !File.Exists(Path.Combine(l,"Macro-v1.10.exe")),"Publication preserves ZIP format without ambiguous EXE");
            Assert(Files.Hash(entry.Available.Path)==Files.Hash(Path.Combine(l,"Macro-v1.10.zip")),"Published ZIP verified unchanged");
            Assert(BuildCatalog.Compare(s,l).Single().Status=="Published","Publication content comparison");
            Assert(CategoryPreferences.ReadShared(l).Get(entry)==Categories.Windows,"Published category stored on share");
            var second=Catalog.Compare(l,Path.Combine(s,"installed")).Single();
            Installer.Install(second.Available,l,Path.Combine(s,"installed"),new FakeRegistrar());
            Assert(File.Exists(Path.Combine(s,"installed","Macro-v1.10","template.txt")),"Published ZIP installs with dependencies");
            Assert(BuildCatalog.Scan(Path.Combine(s,"eXcLuDe")).Count==0,"Admin ignores Exclude when selected as root");
        });
        Case("publish-folders-and-suite",(s,l)=>{
            Write(Path.Combine(s,"Utility-v1.0","Utility.exe"),"program");Write(Path.Combine(s,"Utility-v1.0","helper.dll"),"helper");
            Write(Path.Combine(s,"Utility-v1.0","Assets","EXCLUDE","hidden.exe"),"excluded");
            var package=BuildCatalog.Scan(s).Single();Publisher.Publish(package,s,l,Categories.Windows);
            using(var zip=ZipFile.OpenRead(Path.Combine(l,"Utility-v1.0.zip")))Assert(zip.Entries.Count==2 && zip.Entries.Any(e=>e.FullName=="helper.dll"),"Folder publication retains dependencies and prunes Exclude");
            Assert(BuildCatalog.Compare(s,l).Single().Status=="Published","Folder compares with its published ZIP");
            Write(Path.Combine(s,"Utility-v1.0","helper.dll"),"changed");
            Assert(BuildCatalog.Compare(s,l).Single().Status=="Published content differs","Folder changes detected");
            Publisher.Publish(package,s,l,Categories.Windows);
            Assert(BuildCatalog.Compare(s,l).Single().Status=="Published","Changed folder republished atomically");
            string suite=Path.Combine(s,"AddinSuite-v2.0","Compiled Executables");
            Write(Path.Combine(suite,"Register.cmd"),"not executed");Write(Path.Combine(suite,"UNRegister.cmd"),"not executed");Write(Path.Combine(suite,"SEAddIn.dll"),"dll");
            Write(Path.Combine(suite,"Exclude","old.dll"),"excluded");
            var addin=BuildCatalog.Scan(s).Single();Publisher.Publish(addin,s,l,Categories.Addins);
            using(var zip=ZipFile.OpenRead(Path.Combine(l,"AddinSuite-v2.0.zip")))Assert(zip.Entries.Count==3 && zip.Entries.Any(e=>e.Name=="Register.cmd"),"Suite published together without running registration");
            Assert(BuildCatalog.Compare(s,l).Single(e=>e.Name=="AddinSuite").Status=="Published","Suite identity remains stable across ZIP publication");
        });
        Case("publish-protection",(s,l)=>{
            Write(Path.Combine(s,"Macro-v2.0.exe"),"new");Write(Path.Combine(l,"Macro-v1.0.exe"),"old");Write(Path.Combine(l,"Unrelated.exe"),"keep");
            var p=BuildCatalog.Scan(s).Single();Publisher.Publish(p,s,l,Categories.Macros);
            Assert(!File.Exists(Path.Combine(l,"Macro-v1.0.exe")) && File.Exists(Path.Combine(l,"Unrelated.exe")),"Publication removes older releases and preserves unrelated files");
            Write(p.Path,"changed");
            using(var held=new FileStream(Path.Combine(l,"Macro-v2.0.exe"),FileMode.Open,FileAccess.Read,FileShare.None))
                Throws(()=>Publisher.Publish(p,s,l,Categories.Windows),"Locked shared build rejects replacement");
            Assert(File.ReadAllText(Path.Combine(l,"Macro-v2.0.exe"))=="new","Failed publication preserves old build");
            Assert(CategoryPreferences.ReadShared(l).Get(new Entry{Available=p})==Categories.Macros,"Failed replacement restores category catalog");
            Assert(Directory.GetDirectories(Path.Combine(l,".se-publisher"),"transaction-*").Length==0,"Ordinary publication failure cleaned");
            using(Publisher.Lock(l))Throws(()=>Publisher.Publish(p,s,l,Categories.Windows),"Concurrent publishers serialized");
            Publisher.Publish(p,s,l,Categories.Windows);
            Assert(File.ReadAllText(Path.Combine(l,"Macro-v2.0.exe"))=="changed","Lock released after failure");
            Write(Path.Combine(l,"Macro-v3.0.exe"),"newer");
            Throws(()=>Publisher.Publish(p,s,l,Categories.Macros),"Publisher blocks downgrade");
            Assert(!BuildCatalog.Compare(s,l).Single(e=>e.Name=="Macro").Actionable,"Newer shared build not selectable");
            Throws(()=>Publisher.Publish(p,s,s,Categories.Macros),"Publisher blocks overlapping paths");
            Directory.CreateDirectory(Path.Combine(l,".se-publisher","transaction-interrupted"));
            Throws(()=>CategoryPreferences.AssignShared(l,new Entry{Available=p},Categories.Macros),"Interrupted transaction blocks category writes");
        });
        Case("publish-ambiguity",(s,l)=>{
            Write(Path.Combine(s,"A","Compiled Executables","Macro-v1.0.exe"),"a");Write(Path.Combine(s,"B","Compiled Executables","Macro-v1.0.exe"),"b");
            var entry=BuildCatalog.Compare(s,l).Single();
            Assert(entry.Status=="Ambiguous local builds" && !entry.Actionable,"Two projects cannot silently publish same family/version");
            Throws(()=>Publisher.Publish(entry.Available,s,l,Categories.Macros),"Stale ambiguous selection rejected");
        });
        Case("publish-latest-only",(s,l)=>{
            Zip(Path.Combine(s,"Macro-v1.10.zip"),"Macro.exe");
            Write(Path.Combine(l,"Macro-v1.9.exe"),"old exe");Zip(Path.Combine(l,"Macro-v1.8.zip"),"Macro.exe");
            Write(Path.Combine(l,"Macro-v1.7","Macro.exe"),"old folder");Write(Path.Combine(l,"Macro.exe"),"unversioned alias");
            Write(Path.Combine(l,"Exclude","Macro-v1.0.exe"),"archived");Write(Path.Combine(l,"Other-v1.0.exe"),"unrelated");
            var package=BuildCatalog.Scan(s).Single();Publisher.Publish(package,s,l,Categories.Macros);
            var remaining=Catalog.Scan(l).Where(p=>p.Key==package.Key).ToList();
            Assert(remaining.Count==1 && remaining[0].Release.Version=="1.10","Only latest release remains across ZIP, EXE, folder, and alias formats");
            Assert(File.Exists(Path.Combine(l,"Exclude","Macro-v1.0.exe")) && File.Exists(Path.Combine(l,"Other-v1.0.exe")),"Excluded archives and unrelated packages preserved");
            Assert(Directory.GetDirectories(Path.Combine(l,".se-publisher"),"transaction-*").Length==0,"Obsolete backup files deleted after success");
            Write(Path.Combine(l,"Macro-v1.1.exe"),"legacy leftover");
            var cleanup=BuildCatalog.Compare(s,l).Single(e=>e.Name=="Macro");
            Assert(cleanup.Status=="Cleanup available" && cleanup.Actionable,"Already-published latest release can clean old shared copies");
            Publisher.Publish(package,s,l,Categories.Macros);
            Assert(Catalog.Scan(l).Count(p=>p.Key==package.Key)==1,"Cleanup-only publication removes old copies");
        });
        Case("publish-cleanup-failure",(s,l)=>{
            Write(Path.Combine(s,"Macro-v3.0.exe"),"new");Write(Path.Combine(l,"Macro-v1.0.exe"),"old");Write(Path.Combine(l,"Macro-v2.0","Macro.exe"),"older folder");
            var package=BuildCatalog.Scan(s).Single();var entry=new Entry{Available=package};CategoryPreferences.AssignShared(l,entry,Categories.Macros);
            using(var locked=new FileStream(Path.Combine(l,"Macro-v2.0","Macro.exe"),FileMode.Open,FileAccess.Read,FileShare.None))
                Throws(()=>Publisher.Publish(package,s,l,Categories.Windows),"Locked older folder blocks destructive cleanup");
            Assert(File.Exists(Path.Combine(l,"Macro-v1.0.exe")) && File.Exists(Path.Combine(l,"Macro-v2.0","Macro.exe")) && !File.Exists(Path.Combine(l,"Macro-v3.0.exe")),"Cleanup failure retains every old version and does not publish incomplete update");
            Assert(CategoryPreferences.ReadShared(l).Get(entry)==Categories.Macros,"Cleanup failure preserves category");
            Write(Path.Combine(l,"Macro-v2.0","Exclude","archive.txt"),"keep");
            Throws(()=>Publisher.Publish(package,s,l,Categories.Windows),"Exclude nested within an obsolete package is never deleted");
            Assert(File.Exists(Path.Combine(l,"Macro-v2.0","Exclude","archive.txt")),"Nested excluded archive preserved");
        });
        Case("publish-format-conflict",(s,l)=>{
            Zip(Path.Combine(s,"Macro-v1.0.zip"),"Macro.exe");Write(Path.Combine(l,"Macro-v1.0.exe"),"other distribution");
            var entry=BuildCatalog.Compare(s,l).Single();
            Assert(!entry.Actionable && entry.Status=="Conflicting shared format","Same-version format conflict shown");
            Throws(()=>Publisher.Publish(entry.Available,s,l,Categories.Macros),"Same-version format conflict blocked");
            File.Copy(entry.Available.Path,Path.Combine(l,"Macro-v1.0.zip"));
            Assert(!BuildCatalog.Compare(s,l).Single().Actionable,"Existing ZIP plus EXE ambiguity also blocks publication");
        });
        Case("publish-stale-selection",(s,l)=>{
            Write(Path.Combine(s,"Macro-v1.0.exe"),"old");var p=BuildCatalog.Scan(s).Single();
            Write(Path.Combine(s,"Macro-v2.0.exe"),"new");
            Throws(()=>Publisher.Publish(p,s,l,Categories.Macros),"Publishing revalidates newest build after refresh");
            Assert(!File.Exists(Path.Combine(l,"Macro-v1.0.exe")),"Stale build never appears on share");
            Throws(()=>Publisher.Publish(p,l,s,Categories.Macros),"Build outside selected local root rejected");
        });
        Case("shared-categories-ui",(s,l)=>{
            Write(Path.Combine(s,"Utility-v1.0.exe"),"program");var entry=Catalog.Compare(s,l).Single();
            CategoryPreferences.AssignShared(s,entry,Categories.Windows);
            var other=new Entry{Name="Other"};CategoryPreferences.AssignShared(s,other,Categories.Addins);
            Assert(CategoryPreferences.ReadShared(s).Get(entry)==Categories.Windows,"Sequential admins merge assignments");
            string settings=Path.Combine(l,"user-settings.xml");
            var legacy=new CategoryPreferences();legacy.Set(entry,Categories.Addins);
            new XDocument(new XElement("settings",new XElement("source",s),new XElement("local",l),legacy.Save())).Save(settings);
            using(var user=new ManagerForm(settings,false))
            {
                Configure(user,s,l);var row=Row(user,"Utility");
                Assert((string)row.Cells["Category"].Value==Categories.Windows,"User ignores old per-user category in favor of admin catalog");
                Assert(!XDocument.Load(settings).Root.Elements("categories").Any(),"Private settings no longer store categories");
                using(var admin=new ManagerForm(Path.Combine(l,"admin-settings.xml"),false,true))
                {
                    // A shared-only row can be categorized even without the original build on this PC.
                    string empty=Path.Combine(l,"builds");Directory.CreateDirectory(empty);Configure(admin,empty,s);
                    var adminRow=Row(admin,"Utility");adminRow.Cells[0].Value=true;
                    Assert(!adminRow.Cells["Category"].ReadOnly && adminRow.Cells["Category"] is DataGridViewComboBoxCell,"Admin has editable category dropdown");
                    Assert(Descendants(admin).OfType<Button>().Any(b=>b.Text=="Publish selected"),"Admin exposes publishing actions in shared form");
                    WaitUi(admin.ChangeCategory((Entry)adminRow.Tag,Categories.Addins));
                    Assert((string)Row(admin,"Utility").Cells["Category"].Value==Categories.Addins,"Admin row moves to saved category");
                    Assert(Convert.ToBoolean(Row(admin,"Utility").Cells[0].Value),"Category move preserves checkbox selection");
                    Row(admin,"Utility").Cells["Category"].Value=Categories.Windows;
                    var timeout=System.Diagnostics.Stopwatch.StartNew();
                    while((string)Row(admin,"Utility").Cells["Category"].Value!=Categories.Windows || !Descendants(admin).OfType<Button>().Single(b=>b.Text=="Refresh").Enabled ||
                        CategoryPreferences.ReadShared(s).Get(entry)!=Categories.Windows)
                    {Application.DoEvents();System.Threading.Thread.Sleep(10);if(timeout.Elapsed.TotalSeconds>10)throw new Exception("Category dropdown event timed out");}
                    Application.DoEvents();
                    Assert(((GroupBox)Row(admin,"Utility").DataGridView.Parent).Text==Categories.Windows,"Real dropdown callback moves and saves row");
                    WaitUi(admin.ChangeCategory((Entry)Row(admin,"Utility").Tag,Categories.Addins));
                    using(var bitmap=new System.Drawing.Bitmap(admin.Width,admin.Height))
                    {admin.DrawToBitmap(bitmap,new System.Drawing.Rectangle(0,0,bitmap.Width,bitmap.Height));bitmap.Save(Path.Combine("tests","bin","admin-preview.png"));}
                }
                WaitUi(user.RefreshList());
                Assert((string)Row(user,"Utility").Cells["Category"].Value==Categories.Addins,"User refresh sees another admin's assignment");
                Assert(CategoryPreferences.ReadShared(s).Get(other)==Categories.Addins,"Category edit retains unrelated assignments");
                Assert(!Descendants(user).OfType<Button>().Any(b=>b.Text.Contains("Publish")||b.Name=="CategoryLock"),"User has neither publish actions nor lock control");
                Write(Path.Combine(s,"Utility-v2.0.exe"),"next");WaitUi(user.RefreshList());
                Assert((string)Row(user,"Utility").Cells["Category"].Value==Categories.Addins,"Admin assignment follows new versions");
                using(var bitmap=new System.Drawing.Bitmap(user.Width,user.Height))
                {user.DrawToBitmap(bitmap,new System.Drawing.Rectangle(0,0,bitmap.Width,bitmap.Height));bitmap.Save(Path.Combine("tests","bin","user-preview.png"));}
            }
            Write(Path.Combine(s,CategoryPreferences.FileName),"<invalid />");
            Throws(()=>CategoryPreferences.AssignShared(s,entry,Categories.Macros),"Malformed category catalog is not overwritten");
        });
    }
}
