using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CoopManager
{
    internal sealed class ManagerForm : Form
    {
        readonly TextBox source=new TextBox(), local=new TextBox(), details=new TextBox();
        readonly Label status=new Label();
        readonly Dictionary<string,DataGridView> grids=new Dictionary<string,DataGridView>();
        readonly PackageIcons icons=new PackageIcons();
        CategoryPreferences categories=new CategoryPreferences();
        readonly List<Control> actions=new List<Control>();
        readonly List<Image> artwork=new List<Image>();
        readonly Font actionFont=new Font("Segoe UI",10.5f,FontStyle.Bold);
        readonly TableLayoutPanel frames=new TableLayoutPanel();
        readonly string settings;
        readonly Icon windowIcon;
        readonly bool admin;
        List<Entry> catalog=new List<Entry>();
        bool busy, rendering, categoryEditPending;
        public ManagerForm():this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Coop",AppInfo.SettingsFolder,"settings.xml"),true,AppInfo.Admin){}
        internal ManagerForm(string settingsPath,bool autoRefresh,bool adminEdition=false)
        {
            settings=settingsPath;admin=adminEdition;
            using(var stream=typeof(ManagerForm).Assembly.GetManifestResourceStream("ManagerIcon.ico"))
                if(stream!=null)using(var embedded=new Icon(stream)){windowIcon=(Icon)embedded.Clone();Icon=windowIcon;}
            Text="Solid Edge Macro & Add-in Manager"+(admin?" Admin":"")+" — v"+AppInfo.Version;
            Size=new Size(1140,850); MinimumSize=new Size(1080,740); StartPosition=FormStartPosition.CenterScreen;
            Font=new Font("Segoe UI",9.5f);
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(12)};
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,82));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,62));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,28)); Controls.Add(layout);
            source.Name="SourcePath";local.Name="DestinationPath";
            source.Text=admin?AppInfo.DefaultBuilds:AppInfo.DefaultSource; local.Text=admin?AppInfo.DefaultSource:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Add-ins");
            var paths=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};
            paths.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); paths.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            paths.Controls.Add(PathPanel(admin?"Local builds folder":"Available folder",source,!admin),0,0); paths.Controls.Add(PathPanel(admin?"Shared publish folder":"Local folder",local,admin),1,0); layout.Controls.Add(paths,0,0);
            frames.Dock=DockStyle.Fill; frames.ColumnCount=1; frames.RowCount=3;
            for(int i=0;i<Categories.All.Length;i++)
            {
                frames.RowStyles.Add(new RowStyle(SizeType.Percent,i==0?55:22.5f));
                var group=new GroupBox{Text=Categories.All[i],Dock=DockStyle.Fill,Padding=new Padding(9,6,9,8),Margin=new Padding(0,3,0,6)};
                var grid=CreateGrid(); grids.Add(Categories.All[i],grid); group.Controls.Add(grid);
                if(Categories.All[i]!=Categories.Addins)
                {
                    string helpCategory=Categories.All[i];
                    var helpBar=new Panel{Dock=DockStyle.Top,Height=32};
                    var help=new Button{Name="ImplementationHelp",Text="How to Implement",Tag=helpCategory,Dock=DockStyle.Right,Width=165};
                    help.Click+=delegate{using(var dialog=new ImplementationHelp(helpCategory,admin?"The Local folder chosen in the user manager on each user's PC.":local.Text.Trim()))dialog.ShowDialog(this);};
                    helpBar.Controls.Add(help);group.Controls.Add(helpBar);
                }
                frames.Controls.Add(group,0,i);
            }
            layout.Controls.Add(frames,0,1);
            details.Multiline=true; details.ReadOnly=true; details.Dock=DockStyle.Fill; details.ScrollBars=ScrollBars.Vertical; layout.Controls.Add(details,0,2);
            var buttonHost=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=1};
            var buttons=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=false,Anchor=AnchorStyles.None,Margin=Padding.Empty};
            buttonHost.Controls.Add(buttons,0,0);
            AddButton(buttons,"Refresh",async delegate{await RefreshList();});
            AddButton(buttons,admin?"Publish selected":"Install / update selected",async delegate{if(admin)await Publish(false);else await Apply(false);});
            AddButton(buttons,admin?"Publish / clean all":"Update all installed",async delegate{if(admin)await Publish(true);else await Apply(true);});
            layout.Controls.Add(buttonHost,0,3);
            status.Dock=DockStyle.Fill; status.AutoEllipsis=true; status.Text=admin?"Assign categories in the table; publish selected builds to the shared folder.":"Close Solid Edge before updating add-ins."; layout.Controls.Add(status,0,4);
            try{if(File.Exists(settings)){var x=XDocument.Load(settings);source.Text=admin?((string)x.Root.Element("source")??source.Text):AppInfo.SourceSetting((string)x.Root.Element("source"));local.Text=(string)x.Root.Element("local")??local.Text;}}
            catch(Exception ex){ReportError("Could not load remembered paths",ex);}
            if(autoRefresh)Shown+=async delegate{await RefreshList();};
            FormClosing+=delegate(object sender,FormClosingEventArgs e){if(busy){e.Cancel=true;ShowStatus("Wait for the current operation to finish.");}};
        }
        DataGridView CreateGrid()
        {
            var grid=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoGenerateColumns=false,RowHeadersVisible=false,
                SelectionMode=DataGridViewSelectionMode.FullRowSelect,BackgroundColor=Color.White,BorderStyle=BorderStyle.None,AllowUserToResizeRows=false};
            grid.RowTemplate.Height=40;
            grid.Columns.Add(new DataGridViewCheckBoxColumn{Name="Pick",HeaderText="Select",Width=48});
            grid.Columns.Add(new DataGridViewImageColumn{Name="Icon",HeaderText="",ReadOnly=true,Width=44,ImageLayout=DataGridViewImageCellLayout.Normal});
            var names=new[]{"Package","Available","Installed","Status"}; var widths=new[]{205,105,135,170};
            for(int i=0;i<names.Length;i++)grid.Columns.Add(new DataGridViewTextBoxColumn{Name=names[i],HeaderText=admin?(names[i]=="Available"?"Local build":names[i]=="Installed"?"Published":names[i]):names[i],ReadOnly=true,Width=widths[i]});
            if(admin){var category=new DataGridViewComboBoxColumn{Name="Category",HeaderText="Category",Width=190,FlatStyle=FlatStyle.Flat};category.Items.AddRange(Categories.All);grid.Columns.Add(category);}
            else grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Category",HeaderText="Category",Width=190,ReadOnly=true});
            foreach(DataGridViewColumn column in grid.Columns)column.SortMode=DataGridViewColumnSortMode.NotSortable;
            grid.CurrentCellDirtyStateChanged+=delegate{if(grid.IsCurrentCellDirty)grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
            grid.CellValueChanged+=delegate(object sender,DataGridViewCellEventArgs e)
            {
                if(rendering||!admin||busy||e.RowIndex<0||e.ColumnIndex!=grid.Columns["Category"].Index)return;
                var entry=grid.Rows[e.RowIndex].Tag as Entry;string value=Convert.ToString(grid.Rows[e.RowIndex].Cells["Category"].Value);
                if(entry==null||!Categories.All.Contains(value)||categories.Get(entry)==value)return;
                // Leave the grid's edit callback before moving the row to another category frame.
                categoryEditPending=true;
                BeginInvoke(new Action(async delegate{try{await ChangeCategory(entry,value);}finally{categoryEditPending=false;}}));
            };
            grid.SelectionChanged+=delegate{UpdateDetails(grid);};grid.CurrentCellChanged+=delegate{UpdateDetails(grid);};
            return grid;
        }
        Control PathPanel(string title,TextBox box,bool network)
        {
            var panel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=2,Padding=new Padding(0,0,10,0)};
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,36));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,80));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute,32));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,32));
            var image=UiArtwork.Folder(network);artwork.Add(image);
            panel.Controls.Add(new PictureBox{Image=image,SizeMode=PictureBoxSizeMode.CenterImage,Dock=DockStyle.Fill},0,0);
            var label=new Label{Text=title,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};panel.Controls.Add(label,1,0);panel.SetColumnSpan(label,2);
            box.Dock=DockStyle.Fill;panel.Controls.Add(box,0,1);panel.SetColumnSpan(box,2);actions.Add(box);
            var browse=new Button{Text="Browse…",AutoSize=false,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,Height=box.PreferredHeight,Margin=box.Margin};
            // TextBox height is font/DPI controlled; follow its actual height rather than filling the taller row.
            box.SizeChanged+=delegate{browse.Height=box.Height;};actions.Add(browse);panel.Controls.Add(browse,2,1);
            browse.Click+=async delegate{using(var dialog=new FolderBrowserDialog{SelectedPath=box.Text,Description=title})if(dialog.ShowDialog(this)==DialogResult.OK){box.Text=dialog.SelectedPath;await RefreshList();}};
            box.TextChanged+=delegate{ClearCatalog();};return panel;
        }
        void AddButton(Control parent,string text,EventHandler action){var b=new Button{Text=text,Width=text=="Refresh"?130:250,Height=46,Font=actionFont,Margin=new Padding(6,4,6,4)};b.Click+=action;actions.Add(b);parent.Controls.Add(b);}
        void UpdateDetails(DataGridView grid){var entry=grid.CurrentRow==null?null:grid.CurrentRow.Tag as Entry;details.Text=entry==null?"":entry.Details??"";}
        void ShowStatus(string text){status.Text=text+(Diagnostics.LastError==null?"":" (Diagnostic logging unavailable)");}
        void Log(string text){Diagnostics.Info(text);ShowStatus(text);}
        void ReportError(string context,Exception ex){Diagnostics.Error(context,ex);ShowStatus(context+": "+ex.Message);}
        void Busy(bool value){busy=value;foreach(var c in actions)c.Enabled=!value;foreach(var grid in grids.Values)grid.Enabled=!value;UseWaitCursor=value;}
        void SavePaths()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settings));string tmp=settings+".tmp";
            new XDocument(new XElement("settings",new XElement("source",source.Text.Trim()),new XElement("local",local.Text.Trim()))).Save(tmp);
            if(File.Exists(settings))File.Replace(tmp,settings,null);else File.Move(tmp,settings);
        }
        internal async Task RefreshList()
        {
            if(busy||categoryEditPending)return;Busy(true);
            try
            {
                SavePaths();string s=source.Text.Trim(),l=local.Text.Trim();Diagnostics.Info("SCAN START source="+s+" local="+l);ShowStatus("Checking available and installed programs…");Installer.ValidateRoots(s,l);ClearCatalog();
                CategoryPreferences sharedCategories=null;
                var entries=await Task.Run(()=>{sharedCategories=CategoryPreferences.ReadShared(admin?l:s);var result=admin?BuildCatalog.Compare(s,l):Catalog.Compare(s,l);foreach(var entry in result)icons.Get(entry);return result;});
                categories=sharedCategories;PopulateEntries(entries);Log("SCAN SUCCESS: "+entries.Count+" packages found.");
            }
            catch(Exception ex){ClearCatalog();ReportError("SCAN FAILED",ex);}finally{Busy(false);}
        }
        IEnumerable<DataGridViewRow> Rows(){return grids.Values.SelectMany(g=>g.Rows.Cast<DataGridViewRow>());}
        internal void PopulateEntries(IEnumerable<Entry> entries)
        {
            var selected=new HashSet<Entry>(Rows().Where(r=>r.Tag is Entry&&Convert.ToBoolean(r.Cells[0].Value)).Select(r=>(Entry)r.Tag));catalog=entries.ToList();rendering=true;
            try
            {
                foreach(var grid in grids.Values)grid.Rows.Clear();details.Clear();
                foreach(var entry in catalog.OrderBy(e=>e.Name,StringComparer.OrdinalIgnoreCase))
                {
                    string category=categories.Get(entry);var grid=grids[category];var row=new DataGridViewRow{Tag=entry,Height=40};
                    row.CreateCells(grid,selected.Contains(entry),icons.Get(entry),entry.Name,entry.AvailableVersion,entry.InstalledVersion,entry.Status,category);row.Cells[0].ReadOnly=!entry.Actionable;
                    if(!entry.Actionable)row.DefaultCellStyle.ForeColor=Color.DimGray;else if((entry.Status??"").Contains("available")||entry.Status=="Not installed")row.DefaultCellStyle.BackColor=Color.FromArgb(255,247,220);
                    grid.Rows.Add(row);Diagnostics.Info("CATALOG package="+entry.Name+" category="+category+" available="+entry.AvailableVersion+" installed="+entry.InstalledVersion+" status="+entry.Status);
                }
                foreach(var grid in grids.Values){grid.ClearSelection();grid.CurrentCell=null;}
                for(int i=0;i<Categories.All.Length;i++)frames.RowStyles[i].Height=Math.Max(2,grids[Categories.All[i]].Rows.Count)+1.5f;
            }
            finally{rendering=false;}
        }
        internal async Task ChangeCategory(Entry entry,string category)
        {
            if(!admin||busy||!catalog.Contains(entry))return;
            Busy(true);
            try
            {
                string shared=local.Text.Trim();
                categories=await Task.Run(()=>CategoryPreferences.AssignShared(shared,entry,category));
                Log("Shared category updated: "+entry.Name);
            }
            catch(Exception ex){ReportError("Could not save shared category",ex);}
            finally{PopulateEntries(catalog);Busy(false);}
        }
        void ClearCatalog(){foreach(var grid in grids.Values)grid.Rows.Clear();details.Clear();catalog.Clear();icons.Dispose();}
        protected override void Dispose(bool disposing)
        {
            if(disposing){foreach(var grid in grids.Values)if(!grid.IsDisposed)grid.Rows.Clear();icons.Dispose();}base.Dispose(disposing);
            if(disposing){if(windowIcon!=null)windowIcon.Dispose();foreach(var image in artwork)image.Dispose();actionFont.Dispose();}
        }
        async Task Publish(bool all)
        {
            if(!admin||busy)return;
            foreach(var grid in grids.Values)grid.EndEdit();
            if(categoryEditPending){ShowStatus("Wait for the category change to finish, then publish.");return;}
            var chosen=Rows().Where(r=>r.Tag is Entry && (all||Convert.ToBoolean(r.Cells[0].Value))).Select(r=>(Entry)r.Tag).Where(e=>e.Actionable).ToList();
            if(chosen.Count==0){ShowStatus("Select a new or changed build, or refresh the catalog.");return;}
            if(MessageBox.Show(this,"Publish these builds to "+local.Text.Trim()+"?\r\n\r\n"+string.Join("\r\n",chosen.Select(e=>e.Name+" "+e.AvailableVersion+" — "+categories.Get(e)))+
                "\r\n\r\nOlder releases and unversioned aliases of these programs will be removed from the share after the new builds are verified.","Review publication",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)!=DialogResult.OK)return;
            Busy(true);
            try
            {
                SavePaths();string builds=source.Text.Trim(),shared=local.Text.Trim();
                foreach(var entry in chosen){string category=categories.Get(entry);Log("Publishing "+entry.Name+"…");await Task.Run(()=>Publisher.Publish(entry.Available,builds,shared,category));Log("Published "+entry.Name);}
            }
            catch(Exception ex){ReportError("PUBLICATION STOPPED",ex);MessageBox.Show(this,ex.Message,"Publication stopped",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            finally{Busy(false);}
            await RefreshList();
        }
        async Task Apply(bool all)
        {
            foreach(var grid in grids.Values)grid.EndEdit();
            var chosen=Rows().Where(r=>r.Tag is Entry).Where(r=>all?new[]{"Update available","Repair available","Cleanup available"}.Contains(((Entry)r.Tag).Status):Convert.ToBoolean(r.Cells[0].Value)).Select(r=>(Entry)r.Tag).Where(e=>e.Actionable).ToList();
            if(chosen.Count==0){ShowStatus("Select a package, or refresh to check for updates.");return;}
            if(MessageBox.Show(this,"Install/update these packages?\r\n\r\n"+string.Join("\r\n",chosen.Select(e=>e.Name))+"\r\n\r\nOlder matching versions will be removed after success. Close SE before updating add-ins.","Review installation",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)!=DialogResult.OK)return;
            Busy(true);
            try{SavePaths();string s=source.Text.Trim(),l=local.Text.Trim();foreach(var e in chosen){Log("Installing "+e.Name+"…");string result=await Task.Run(()=>Installer.Install(e.Available,s,l,new ScriptRegistrar()));Log(e.Name+": "+result);}}
            catch(Exception ex){ReportError("INSTALL STOPPED",ex);MessageBox.Show(this,ex.Message,"Installation stopped",MessageBoxButtons.OK,MessageBoxIcon.Warning);}finally{Busy(false);}await RefreshList();
        }
    }
    internal static class Program
    {
        [STAThread] static void Main()
        {
            Diagnostics.Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Coop", AppInfo.SettingsFolder, "Logs"));
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                Diagnostics.Error("UNHANDLED UI EXCEPTION", e.Exception);
                MessageBox.Show("An unexpected error was recorded. The manager will close.\r\nLogs: " + Diagnostics.LogDirectory +
                    (Diagnostics.LastError == null ? "" : "\r\nLogging failed: " + Diagnostics.LastError), "SE Macro Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Do not continue in an uncertain UI/transaction state. Recovery journals remain on disk.
                Environment.Exit(1);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { Diagnostics.Error("UNHANDLED PROCESS EXCEPTION", e.ExceptionObject as Exception); };
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            try { Application.Run(new ManagerForm()); }
            catch (Exception ex) { Diagnostics.Error("STARTUP/MAIN FAILURE", ex); MessageBox.Show(ex.Message + "\r\nLogs: " + Diagnostics.LogDirectory); }
            finally { Diagnostics.Info("SESSION END"); }
        }
    }
}
