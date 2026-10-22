using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CoopManager;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;

internal class FakeRegistrar : IRegistrar
{
    public int Registers, Unregisters;
    public bool Fail, Running;
    public void CheckClosed() { if (Running) throw new IOException("SE running"); }
    public void Run(string dir, bool unregister)
    {
        if (unregister) Unregisters++;
        else { Registers++; if (Fail && Registers == 1) throw new IOException("Registration failed"); }
    }
}
internal static partial class Tests
{
    static int count;
    static string root;
    static void Assert(bool value, string message) { count++; if (!value) throw new Exception(message); }
    static void Write(string p, string s) { Directory.CreateDirectory(Path.GetDirectoryName(p)); File.WriteAllText(p, s); }
    static void Throws(Action action, string message) { bool thrown = false; try { action(); } catch (IOException) { thrown = true; } Assert(thrown, message); }
    static void Case(string name, Action<string,string> test)
    {
        string s = Path.Combine(root, name, "source"), l = Path.Combine(root, name, "local");
        Directory.CreateDirectory(s); Directory.CreateDirectory(l); test(s, l); Console.WriteLine("PASS " + name);
    }
    static IEnumerable<Control> Descendants(Control parent)
    { foreach (Control child in parent.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
    [STAThread] static void Main()
    {
        root = Path.GetFullPath(Path.Combine("tests", "work", Guid.NewGuid().ToString("N"))); Directory.CreateDirectory(root);
        Diagnostics.Initialize(Path.Combine(root, "logs"));
        Assert(AppInfo.SourceSetting(null)==@"H:\Coop\internal\Macros and Add-ins", "New default source");
        Assert(AppInfo.SourceSetting(@"H:\solid_edge\Coop\internal\Macros and Add-ins\")==AppInfo.DefaultSource, "Old default migrated");
        Assert(AppInfo.SourceSetting(@"Z:\Custom Macros")==@"Z:\Custom Macros", "Custom source retained");
        Assert(ReleaseName.Compare(ReleaseName.Parse("Macro-v1.10.exe", true), ReleaseName.Parse("Macro v1.9.exe", true)) > 0, "Numeric versions");
        Assert(ReleaseName.Compare(ReleaseName.Parse("Macro V1.0.0", false), ReleaseName.Parse("Macro v1.0", false)) == 0, "Padded versions");
        Assert(ReleaseName.Parse("BoltCircleHoles-v0.20.exe", true).Name == "BoltCircleHoles", "Name parsing");
        Case("macro", (s,l) => {
            Write(Path.Combine(s,"Macro-v1.10.exe"), "new"); Write(Path.Combine(l,"Macro-v1.9.exe"), "old"); Write(Path.Combine(l,"Unrelated.exe"),"keep");
            Assert(Catalog.Compare(s,l).First(x=>x.Name=="Macro").Status=="Update available", "Upgrade status");
            Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar());
            Assert(File.ReadAllText(Path.Combine(l,"Macro-v1.10.exe"))=="new", "Installed content");
            Assert(!File.Exists(Path.Combine(l,"Macro-v1.9.exe")), "Older version removed");
            Assert(File.Exists(Path.Combine(l,"Unrelated.exe")), "Unrelated preserved");
            Assert(Catalog.Compare(s,l).First(x=>x.Name=="Macro").Status=="Up to date", "Current status");
            Write(Path.Combine(l,"Macro-v1.10.exe"),"changed");
            Assert(Catalog.Compare(s,l).First(x=>x.Name=="Macro").Status=="Repair available", "Repair status");
        });
        Case("rollback", (s,l) => {
            foreach (var d in new[]{s,l}) { Write(Path.Combine(d,"Register.cmd"),"exit /b 0"); Write(Path.Combine(d,"UNRegister.cmd"),"exit /b 0"); }
            Write(Path.Combine(s,"SEAddIn.dll"),"new"); Write(Path.Combine(l,"SEAddIn.dll"),"old");
            var reg = new FakeRegistrar { Fail=true };
            Throws(()=>Installer.Install(Catalog.Scan(s).Single(),s,l,reg),"Registration failure reported");
            Assert(File.ReadAllText(Path.Combine(l,"SEAddIn.dll"))=="old","Old DLL restored");
            Assert(reg.Registers==2 && reg.Unregisters==2,"Old registration restored");
            Assert(Directory.GetDirectories(Path.Combine(l,".se-manager"),"transaction-*").Length==0,"Rollback cleaned");
        });
        Case("suite", (s,l) => {
            foreach(var d in new[]{s,l}) { Write(Path.Combine(d,"Register.cmd"),"exit /b 0"); Write(Path.Combine(d,"UNRegister.cmd"),"exit /b 0"); Write(Path.Combine(d,"SEAddIn.dll"),"dll"); }
            Write(Path.Combine(s,"Callout V1.10","addin.dll"),"new"); Write(Path.Combine(l,"Callout V1.9","addin.dll"),"old");
            Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar());
            Assert(!Directory.Exists(Path.Combine(l,"Callout V1.9")),"Old suite component removed");
            Assert(File.Exists(Path.Combine(l,"Callout V1.10","addin.dll")),"New suite component installed");
            Assert(Catalog.Compare(s,l).Single().Status=="Up to date","Suite fingerprint");
            Write(Path.Combine(s,"SEAddIn.dll"),"updated"); Assert(Catalog.Compare(s,l).Single().Status=="Update available","Unversioned content change");
        });
        Case("zip", (s,l) => {
            string zip = Path.Combine(s,"Macro-v2.0.zip");
            using(var a=ZipFile.Open(zip,ZipArchiveMode.Create)) { var e=a.CreateEntry("Macro.exe"); using(var w=new StreamWriter(e.Open()))w.Write("exe"); var t=a.CreateEntry("template.txt"); using(var w=new StreamWriter(t.Open()))w.Write("template"); }
            Write(Path.Combine(l,"Macro-v1.0.exe"),"old"); Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar());
            Assert(File.Exists(Path.Combine(l,"Macro-v2.0","template.txt")),"ZIP dependencies copied");
            Assert(!File.Exists(Path.Combine(l,"Macro-v1.0.exe")),"Legacy loose EXE removed");
            Assert(Catalog.Compare(s,l).Single().Status=="Up to date","ZIP current status");
        });
        Case("zip-traversal", (s,l) => {
            using(var a=ZipFile.Open(Path.Combine(s,"Bad-v1.0.zip"),ZipArchiveMode.Create)) { var e=a.CreateEntry("../../outside.exe"); using(var w=new StreamWriter(e.Open()))w.Write("bad"); }
            Throws(()=>Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar()),"ZIP traversal blocked");
            Assert(!File.Exists(Path.Combine(l,"outside.exe")),"No escaped output");
        });
        Case("missing-script", (s,l) => {
            Write(Path.Combine(s,"SEAddIn.dll"),"new");
            Throws(()=>Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar()),"Missing registrar blocked");
            Assert(!File.Exists(Path.Combine(l,"SEAddIn.dll")),"No partial DLL install");
        });
        Case("se-open", (s,l) => {
            Write(Path.Combine(s,"SEAddIn.dll"),"dll"); Write(Path.Combine(s,"Register.cmd"),"register"); Write(Path.Combine(s,"UNRegister.cmd"),"unregister");
            Throws(()=>Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar{Running=true}),"Open SE blocked");
            Assert(!File.Exists(Path.Combine(l,"SEAddIn.dll")),"Open SE unchanged");
        });
        Case("ambiguous", (s,l) => {
            Write(Path.Combine(s,"Macro-v1.0.exe"),"exe"); Write(Path.Combine(s,"Macro-v1.0.zip"),"zip");
            Assert(!Catalog.Compare(s,l).Single().Actionable,"Ambiguous same-version source blocked");
        });
        Case("roots", (s,l) => { Throws(()=>Installer.ValidateRoots(s,s),"Same roots blocked"); Throws(()=>Installer.ValidateRoots(s,Path.Combine(s,"child")),"Nested roots blocked"); });
        Case("recovery", (s,l) => {
            Write(Path.Combine(s,"Macro-v1.0.exe"),"exe"); Directory.CreateDirectory(Path.Combine(l,".se-manager","transaction-aborted"));
            Throws(()=>Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar()),"Unfinished recovery blocked");
        });
        Case("downgrade", (s,l) => {
            Write(Path.Combine(s,"Macro-v1.0.exe"),"old"); Write(Path.Combine(l,"Macro-v2.0.exe"),"new");
            Assert(Catalog.Compare(s,l).Single().Status=="Local version newer","Newer local detected");
            Throws(()=>Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar()),"Downgrade blocked by installer");
            Assert(File.ReadAllText(Path.Combine(l,"Macro-v2.0.exe"))=="new","Newer local preserved");
        });
        Case("obsolete-files", (s,l) => {
            Write(Path.Combine(s,"Package-v1.0","Macro.exe"),"exe"); Write(Path.Combine(s,"Package-v1.0","old.txt"),"old"); Write(Path.Combine(s,"Package-v1.0","custom.txt"),"default");
            var p=Catalog.Scan(s).Single(); Installer.Install(p,s,l,new FakeRegistrar());
            Write(Path.Combine(l,"Package-v1.0","custom.txt"),"user edited");
            File.Delete(Path.Combine(s,"Package-v1.0","old.txt")); File.Delete(Path.Combine(s,"Package-v1.0","custom.txt"));
            Installer.Install(p,s,l,new FakeRegistrar());
            Assert(!File.Exists(Path.Combine(l,"Package-v1.0","old.txt")),"Obsolete managed file removed");
            Assert(File.ReadAllText(Path.Combine(l,"Package-v1.0","custom.txt"))=="user edited","Modified obsolete file retained");
        });
        Case("copy-failure", (s,l) => {
            Write(Path.Combine(s,"Macro-v1.1.exe"),"new"); Write(Path.Combine(l,"Macro-v1.0.exe"),"old");
            using(var locked=new FileStream(Path.Combine(l,"Macro-v1.0.exe"),FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                Throws(()=>Installer.Install(Catalog.Scan(s).Single(),s,l,new FakeRegistrar()),"Locked old file blocks update");
            Assert(File.ReadAllText(Path.Combine(l,"Macro-v1.0.exe"))=="old","Locked original preserved");
            Assert(!File.Exists(Path.Combine(l,"Macro-v1.1.exe")),"No partial copy after lock failure");
        });
        Case("mixed-suite-macros", (s,l) => {
            foreach(var d in new[]{s,l}) { Write(Path.Combine(d,"Register.cmd"),"register"); Write(Path.Combine(d,"UNRegister.cmd"),"unregister"); Write(Path.Combine(d,"SEAddIn.dll"),"addin"); }
            Write(Path.Combine(s,"Macro-v2.0","Macro.exe"),"new"); Write(Path.Combine(l,"Macro-v1.0","Macro.exe"),"old");
            Assert(Catalog.Scan(s).Count==2,"Macro folder separate from suite");
            Assert(Catalog.SourceFiles(Catalog.Scan(s).Single(x=>x.Bundle)).Count==3,"Suite excludes independent macro folder");
            Installer.Install(Catalog.Scan(s).Single(x=>!x.Bundle),s,l,new FakeRegistrar());
            Assert(!Directory.Exists(Path.Combine(l,"Macro-v1.0")),"Older macro folder removed beside suite");
            Assert(Catalog.Compare(s,l).Single(x=>x.Name=="Macro").Status=="Up to date","Mixed folder macro visible");
        });
        Case("grid-selection", (s,l) => {
            using (var form = new ManagerForm(Path.Combine(l,"settings.xml"), false))
            {
                // Create real WinForms handles without displaying a window or scanning user folders.
                form.ShowInTaskbar=false; form.Opacity=0; form.Show(); Application.DoEvents();
                using(var stream=typeof(ManagerForm).Assembly.GetManifestResourceStream("ManagerIcon.ico"))
                using(var reader=new BinaryReader(stream))
                {
                    Assert(reader.ReadUInt16()==0 && reader.ReadUInt16()==1 && reader.ReadUInt16()==7,"Seven icon resolutions embedded");
                    using(var bitmap=form.Icon.ToBitmap()) Assert(bitmap.Width>0,"Window uses valid embedded icon");
                }
                var grid = Descendants(form).OfType<DataGridView>().First();
                var entry = new Entry { Name="Macro", Status="Update available", Details="Package details", Actionable=true };
                int selectionEvents = 0; grid.SelectionChanged += delegate { selectionEvents++; };
                form.PopulateEntries(new[]{entry});
                grid.CurrentCell = grid.Rows.Cast<DataGridViewRow>().First(r=>r.Tag is Entry).Cells[1];
                Assert(grid.CurrentRow.Tag==entry, "Entry attached before selection");
                Assert(Descendants(form).OfType<TextBox>().Any(t=>t.Text=="Package details"), "Selection displays details");
                Assert(grid.Columns["Icon"] is DataGridViewImageColumn && grid.Rows.Cast<DataGridViewRow>().First(r=>r.Tag is Entry).Cells["Icon"].Value is System.Drawing.Bitmap,"Package icon displayed in grid");
                Assert(grid.Rows.Cast<DataGridViewRow>().First(r=>r.Tag is Entry).Height>=32,"List icons are not vertically clipped");
                int empty = grid.Rows.Add(false,null,"Transient row");
                grid.CurrentCell = grid.Rows[empty].Cells[1]; // v1.0 crashed here when Tag had not been assigned.
                Assert(grid.CurrentRow.Tag==null, "Null row metadata safely handled");
                form.PopulateEntries(new[]{entry}); grid.CurrentCell=grid.Rows.Cast<DataGridViewRow>().First(r=>r.Tag is Entry).Cells[1];
                form.PopulateEntries(new Entry[0]);
                Assert(Descendants(form).OfType<GroupBox>().Count()==3 && Descendants(form).OfType<DataGridView>().All(g=>g.Rows.Count==0), "Three separate empty frames remain after clearing packages");
                Assert(selectionEvents>0, "Real grid selection events exercised");
            }
        });
        Case("package-icons", (s,l) => {
            string exe=Path.Combine(s,"Macro-v1.2.exe"), png=Path.Combine(s,"Macro.png"); Write(exe,"fixture executable");
            using(var bitmap=new System.Drawing.Bitmap(32,32))
            { using(var g=System.Drawing.Graphics.FromImage(bitmap)) g.Clear(System.Drawing.Color.Red); bitmap.Save(png,System.Drawing.Imaging.ImageFormat.Png); }
            var entry=new Entry { Available=new Package { Path=exe, Release=ReleaseName.Parse("Macro-v1.2.exe",true),Kind="Macro" } };
            using(var cache=new PackageIcons())
            {
                var icon=cache.Get(entry);
                Assert(icon.Width==32 && icon.GetPixel(12,12).R>200 && icon.GetPixel(12,12).G<20,"Matching sidecar icon used");
                Assert(object.ReferenceEquals(icon,cache.Get(entry)),"Icons cached for repeated reads");
                using(var unlocked=File.Open(png,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) Assert(unlocked.Length>0,"Icon file not locked");
            }
            string zip=Path.Combine(s,"Archive-v1.0.zip");
            using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create)) archive.CreateEntryFromFile(png,"Assets/icon.png");
            using(var cache=new PackageIcons())
            {
                var zipped=cache.Get(new Entry{ Available=new Package { Path=zip,Kind="ZIP" } });
                Assert(zipped.GetPixel(12,12).R>200 && zipped.GetPixel(12,12).G<20,"ZIP artwork loaded without extraction");
                using(var unlocked=File.Open(zip,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) Assert(unlocked.Length>0,"ZIP not locked by icon");
            }
            Write(png,"invalid image");
            using(var cache=new PackageIcons())
            {
                Assert(cache.Get(entry)!=null,"Corrupt icon safely falls back");
                Assert(cache.Get(new Entry{Details=Path.Combine(l,"missing.exe")})!=null,"Missing package icon safely falls back");
            }
            string folder=Path.Combine(s,"Folder-v1.0"); Directory.CreateDirectory(folder);
            using(var stream=File.Create(Path.Combine(folder,"app.ico"))) System.Drawing.SystemIcons.Information.Save(stream);
            using(var cache=new PackageIcons()) Assert(cache.Get(new Entry{Available=new Package{Path=folder}}).Width==32,"Folder ICO loaded");
            using(var cache=new PackageIcons()) Assert(cache.Get(new Entry{Details=System.Reflection.Assembly.GetExecutingAssembly().Location}).Width==32,"Executable icon extraction");
        });
        Case("zip-ico-priority", (s,l) => {
            string zip=Path.Combine(s,"Macro-v1.0.zip"), png=Path.Combine(s,"Macro.png");
            using(var bitmap=new System.Drawing.Bitmap(32,32))
            { using(var g=System.Drawing.Graphics.FromImage(bitmap)) g.Clear(System.Drawing.Color.Red); bitmap.Save(png,System.Drawing.Imaging.ImageFormat.Png); }
            using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(png,"Macro.png");
                var entry=archive.CreateEntry("Nested/Assets/actual.ICO");
                using(var stream=entry.Open()) System.Drawing.SystemIcons.Information.Save(stream);
            }
            using(var cache=new PackageIcons())
            {
                var icon=cache.Get(new Entry{Available=new Package{Path=zip,Kind="ZIP"}});
                Assert(!(icon.GetPixel(12,12).R>200 && icon.GetPixel(12,12).G<20),"Nested ICO beats matching PNG and outside sidecar");
                Assert(File.ReadAllText(Diagnostics.CurrentPath).Contains("entry=Nested/Assets/actual.ICO"),"Exact selected ZIP icon logged");
            }
        });
        foreach(var relative in new[]{"../Bolt Circle Holes/Compiled Executables/BoltCircleHoles-v0.20.zip","../SketchToModels-v2.32/Compiled Executables/SketchToModels-v2.32.zip"})
        {
            string archivePath=Path.GetFullPath(relative);
            if(!File.Exists(archivePath)) continue; // Optional integration fixtures from the parent repository.
            using(var cache=new PackageIcons())
            {
                cache.Get(new Entry{Available=new Package{Path=archivePath,Kind="ZIP"}});
                Assert(File.ReadAllText(Diagnostics.CurrentPath).Split(new[]{Environment.NewLine},StringSplitOptions.None).Any(line=>line.Contains("ICON SELECTED archive="+archivePath+" entry=") && line.EndsWith(".ico",StringComparison.OrdinalIgnoreCase)),"Project ZIP ICO decoded: "+Path.GetFileName(archivePath));
            }
        }
        Case("program-groups", (s,l) => {
            Write(Path.Combine(s,"Utility-v1.10.exe"),"desktop program"); Write(Path.Combine(l,"Utility-v1.9.exe"),"older desktop program");
            var program=Catalog.Compare(s,l).Single();
            var preferences=new CategoryPreferences(); preferences.Set(program,Categories.Windows);
            var reloaded=new CategoryPreferences(); reloaded.Load(preferences.Save());
            Assert(reloaded.Get(program)==Categories.Windows,"Windows category saved and restored");
            var future=new Entry{Available=new Package{Release=ReleaseName.Parse("Utility-v2.0.exe",true)}};
            Assert(reloaded.Get(future)==Categories.Windows,"Category retained across version changes");
            var registrar=new FakeRegistrar(); Installer.Install(program.Available,s,l,registrar);
            Assert(registrar.Registers==0 && registrar.Unregisters==0,"Windows program updates do not register with SE");
            Assert(File.Exists(Path.Combine(l,"Utility-v1.10.exe")) && !File.Exists(Path.Combine(l,"Utility-v1.9.exe")),"Windows executable copied and old release removed");
            Write(Path.Combine(s,"Addin-v1.0","library.dll"),"DLL");
            Assert(Catalog.Compare(s,l).Single(e=>e.Name=="Addin").Category==Categories.Addins,"Add-in folder automatically categorized");
            string settings=Path.Combine(l,"ui-settings.xml");
            using(var form=new ManagerForm(settings,false))
            {
                form.ShowInTaskbar=false; form.Opacity=0; form.Show(); Application.DoEvents();
                form.PopulateEntries(new[]{program});
                var grid=Descendants(form).OfType<DataGridView>().Single(g=>g.Rows.Cast<DataGridViewRow>().Any(r=>r.Tag==program));
                Assert(Descendants(form).OfType<GroupBox>().Select(g=>g.Text).SequenceEqual(Categories.All),"All three separate frames in requested order");
                Assert(Descendants(form).OfType<DataGridView>().All(g=>g.Columns["Category"].ReadOnly && g.Columns["Category"] is DataGridViewTextBoxColumn),"User category is read-only text");
                Assert(!Descendants(form).OfType<Button>().Any(b=>b.Name=="CategoryLock"),"User category lock button removed");
                var mainButtons=Descendants(form).OfType<Button>().Where(b=>!b.Text.StartsWith("Browse")).ToArray();
                Assert(mainButtons.Length==3 && mainButtons.All(b=>b.Font.Bold && b.Height>=44),"Three action buttons are larger and bold");
                var host=mainButtons[0].Parent.Parent;
                int left=mainButtons.Min(b=>b.PointToScreen(System.Drawing.Point.Empty).X), right=mainButtons.Max(b=>b.PointToScreen(System.Drawing.Point.Empty).X+b.Width);
                Assert(Math.Abs((left+right)/2.0-(host.PointToScreen(System.Drawing.Point.Empty).X+host.Width/2.0))<=2,"Action buttons centered as a group");
                Assert(Descendants(form).OfType<Button>().Where(b=>b.Text.StartsWith("Browse")).All(b=>b.Height==b.Parent.Controls.OfType<TextBox>().Single().Height),"Browse buttons match path textbox height");
                WaitUi(form.ChangeCategory(program,Categories.Windows));
                Assert(((GroupBox)grid.Parent).Text==Categories.Macros,"User edition cannot change category even through handler");
                Assert(Descendants(form).OfType<DataGridView>().All(g=>g.Rows.Cast<DataGridViewRow>().All(r=>r.Tag is Entry)),"Frames contain only package rows");
                Assert(!Descendants(form).OfType<Button>().Any(b=>b.Text.Contains("logs")),"No user-facing log buttons");
                var pictures=Descendants(form).OfType<PictureBox>().ToArray();
                Assert(pictures.Length==2 && pictures.All(p=>p.Image!=null),"Network and local folder pictures displayed");
                Assert(pictures[0].PointToScreen(System.Drawing.Point.Empty).Y==pictures[1].PointToScreen(System.Drawing.Point.Empty).Y,"Folder controls are side by side");
            }
        });
        Case("compressed-ico-pixels",(s,l)=>{
            byte[] png;
            using(var bitmap=new System.Drawing.Bitmap(32,32))
            { using(var g=System.Drawing.Graphics.FromImage(bitmap))g.Clear(System.Drawing.Color.Cyan);using(var buffer=new MemoryStream()){bitmap.Save(buffer,System.Drawing.Imaging.ImageFormat.Png);png=buffer.ToArray();} }
            string ico=Path.Combine(s,"Macro.ico");
            using(var writer=new BinaryWriter(File.Create(ico)))
            {writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)1);writer.Write((byte)32);writer.Write((byte)32);writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(png.Length);writer.Write(22);writer.Write(png);}
            using(var cache=new PackageIcons())
            {
                var image=cache.Get(new Entry{Details=ico});
                Assert(image.GetPixel(16,16).ToArgb()==System.Drawing.Color.Cyan.ToArgb(),"PNG-compressed ICO pixels decoded correctly, not GDI noise");
            }
        });
        Case("exclude-source-folder",(s,l)=>{
            Write(Path.Combine(s,"Exclude","Hidden-v9.0.exe"),"hidden");
            Write(Path.Combine(s,"Visible-v1.0.exe"),"visible");
            Write(Path.Combine(s,"OnlyHidden-v1.0","eXcLuDe","Hidden.exe"),"hidden nested");
            Write(Path.Combine(s,"Folder-v1.0","Visible.exe"),"visible folder");
            Write(Path.Combine(s,"Folder-v1.0","Assets","EXCLUDE","hidden.dll"),"hidden dependency");
            var packages=Catalog.Scan(s);
            Assert(packages.Count==2 && !packages.Any(p=>p.Release.Name=="Exclude" || p.Release.Name=="OnlyHidden"),"Root and nested Exclude folders omitted from discovery");
            var folder=packages.Single(p=>p.Release.Name=="Folder");
            Assert(Catalog.SourceFiles(folder).Count==1,"Nested Exclude not copied with package");
            Installer.Install(folder,s,l,new FakeRegistrar());
            Assert(File.Exists(Path.Combine(l,"Folder-v1.0","Visible.exe")) && !Directory.Exists(Path.Combine(l,"Folder-v1.0","Assets")),"Install omits excluded source content");
            Assert(Files.Tree(s).Any(f=>f.Contains("Hidden-v9.0")),"Safety traversal remains unfiltered");
            Assert(Catalog.Scan(Path.Combine(s,"Exclude")).Count==0,"Selecting Exclude itself yields no available packages");
            Write(Path.Combine(s,"Register.cmd"),"register");Write(Path.Combine(s,"UNRegister.cmd"),"unregister");Write(Path.Combine(s,"SEAddIn.dll"),"dll");
            var suite=Catalog.Scan(s).Single(p=>p.Bundle);
            Assert(!Catalog.SourceFiles(suite).Keys.Any(Files.ExcludedRelativeFile),"Shared bundle ignores Exclude content");
            Assert(!Catalog.Compare(s,l).Single(e=>e.Available!=null && e.Available.Bundle).Details.Contains("Exclude"),"Excluded folder absent from suite details");
        });
        PublishingCases();
        string evidence = string.Join("\n", Directory.GetFiles(Diagnostics.LogDirectory,"*.log").Select(File.ReadAllText));
        Assert(evidence.Contains("INSTALL SUCCESS") && evidence.Contains("COPY VERIFIED"), "Successful operations persisted");
        Assert(evidence.Contains("INSTALL FAILED") && evidence.Contains("ROLLBACK SUCCESS") && evidence.Contains("System.IO.IOException"), "Failures and rollback details persisted");
        Case("logging", (s,l) => {
            Diagnostics.Initialize(Path.Combine(l,"logs"),1024);
            Parallel.For(0,12,i=>Diagnostics.Info("Concurrent marker "+i));
            string written=string.Join("\n",Directory.GetFiles(Diagnostics.LogDirectory,"*.log").Select(File.ReadAllText));
            Assert(Enumerable.Range(0,12).All(i=>written.Contains("Concurrent marker "+i)),"Concurrent log writes retained");
            for(int i=0;i<35;i++) Diagnostics.Info(new string('x',1100));
            Assert(Directory.GetFiles(Diagnostics.LogDirectory,"*.log").Length<=30,"Log retention bounded");
            Assert(Directory.GetFiles(Diagnostics.LogDirectory,"*.log").Length>1,"Log rotation exercised");
            try { throw new InvalidOperationException("Test exception with stack"); }
            catch(Exception ex) { Diagnostics.Error("Test exception",ex); }
            Assert(File.ReadAllText(Diagnostics.CurrentPath).Contains("InvalidOperationException"),"Full exception type logged");
            string exported=Path.Combine(l,"support.zip"); Diagnostics.Export(exported);
            using(var zip=ZipFile.OpenRead(exported)) Assert(zip.Entries.Count==Directory.GetFiles(Diagnostics.LogDirectory,"*.log").Length,"All retained logs exported");
            Write(Path.Combine(l,"blocked"),"file, not directory"); Diagnostics.Initialize(Path.Combine(l,"blocked","logs")); Diagnostics.Info("Should not throw");
            Assert(Diagnostics.LastError!=null,"Logging failure reported without throwing");
        });
        Console.WriteLine(count + " assertions passed.");
        Files.RemoveTree(Path.GetFullPath(Path.Combine("tests","work")),root);
    }
}
