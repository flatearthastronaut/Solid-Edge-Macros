using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace PopulateSHCS
{
    static class Program
    {
        [STAThread]static void Main()
        {
            bool first;using(var guard=new Mutex(true,"Local\\PopulateSHCS-v1",out first)){
                if(!first){MessageBox.Show("Populate SHCS is already running.");return;}
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                using(var filter=new BusyFilter())Application.Run(new MainWindow());
            }
        }
    }
    public class MainWindow:Form
    {
        readonly TextBox folder=new TextBox{Dock=DockStyle.Fill};
        readonly DataGridView grid=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,BackgroundColor=Color.White};
        readonly TextBox detail=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical};
        readonly Label status=new Label{AutoSize=true,Text="Select your Standard Parts folder, then scan the main assembly."};
        readonly FlowLayoutPanel actions=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true};
        readonly Button stop=new Button{Text="Cancel",Enabled=false,AutoSize=true};
        List<CatalogPart> catalog=new List<CatalogPart>();Scan scan;Engine engine;bool busy,cancel;DateTime lastPaint;
        string mappedFile{get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"RememberedParts.xml");}}
        public MainWindow()
        {
            Text="Populate SHCS v1.0 — Solid Edge";Size=new System.Drawing.Size(1180,760);MinimumSize=new System.Drawing.Size(850,560);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(14),ColumnCount=1,RowCount=6};
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,75));layout.RowStyles.Add(new RowStyle(SizeType.Percent,25));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(layout);
            layout.Controls.Add(new Label{AutoSize=true,MaximumSize=new System.Drawing.Size(1100,0),Text="Populate empty counterbores using Standard Parts SHCS. Length = 1.5 × nominal screw diameter + cylinder length below the shoulder, rounded UP to the next available part. Inch sizes win chart ties. All lengths below are inches."},0,0);
            var pathRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,AutoSize=true};pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));pathRow.Controls.Add(new Label{Text="Standard Parts folder",AutoSize=true,Padding=new Padding(0,6,8,0)},0,0);pathRow.Controls.Add(folder,1,0);var browse=new Button{Text="Browse…",AutoSize=true};pathRow.Controls.Add(browse,2,0);layout.Controls.Add(pathRow,0,1);
            browse.Click+=delegate{using(var dialog=new FolderBrowserDialog{SelectedPath=folder.Text,Description="Choose the Standard Parts folder containing generated SHCS parts"})if(dialog.ShowDialog(this)==DialogResult.OK){folder.Text=dialog.SelectedPath;catalog.Clear();scan=null;grid.Rows.Clear();}};
            folder.Text=StandardParts.DefaultFolder();folder.TextChanged+=delegate{if(!busy){catalog.Clear();scan=null;grid.Rows.Clear();}};
            AddButton("1  Scan assembly",ScanClick);AddButton("2  Choose missing Standard Part…",ChooseClick);AddButton("3  Insert checked screws",InsertClick);actions.Controls.Add(stop);stop.Click+=delegate{cancel=true;status.Text="Cancelling after the current Solid Edge operation…";};layout.Controls.Add(actions,0,2);
            grid.Columns.Add(new DataGridViewCheckBoxColumn{Name="Insert",HeaderText="Insert",FillWeight=35});
            foreach(string name in new[]{"Component","SHCS","Cylinder","Calculated","Chosen","Status"})grid.Columns.Add(new DataGridViewTextBoxColumn{Name=name,HeaderText=name,ReadOnly=true,FillWeight=name=="Component"?220:name=="Status"?200:70});
            grid.CurrentCellDirtyStateChanged+=delegate{if(grid.IsCurrentCellDirty)grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
            grid.CellValueChanged+=delegate(object sender,DataGridViewCellEventArgs e){if(e.RowIndex>=0 && e.ColumnIndex==0){var row=grid.Rows[e.RowIndex];var h=row.Tag as Hole;if(h!=null && h.Status!="Ready")row.Cells[0].Value=false;}};
            grid.SelectionChanged+=delegate{ShowDetails();};layout.Controls.Add(grid,0,3);layout.Controls.Add(detail,0,4);layout.Controls.Add(status,0,5);
            FormClosing+=delegate(object sender,FormClosingEventArgs e){if(busy){cancel=true;e.Cancel=true;}else if(engine!=null)engine.Dispose();};
        }
        void AddButton(string text,Action action){var b=new Button{Text=text,AutoSize=true,Margin=new Padding(0,8,10,8)};b.Click+=delegate{Run(action);};actions.Controls.Add(b);}
        void Run(Action action)
        {
            if(busy)return;busy=true;cancel=false;stop.Enabled=true;folder.Enabled=false;grid.Enabled=false;foreach(Control c in actions.Controls)if(c!=stop)c.Enabled=false;
            try{action();}catch(OperationCanceledException){status.Text="Cancelled.";}catch(Exception ex){Log(ex.ToString());status.Text="Stopped: "+ex.Message;MessageBox.Show(this,ex.Message,"Populate SHCS",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            finally{busy=false;stop.Enabled=false;folder.Enabled=true;grid.Enabled=true;foreach(Control c in actions.Controls)if(c!=stop)c.Enabled=true;}
        }
        void Progress(string text){status.Text=text;if((DateTime.UtcNow-lastPaint).TotalMilliseconds>100){lastPaint=DateTime.UtcNow;Application.DoEvents();}if(cancel)throw new OperationCanceledException();}
        void Connect(){if(engine!=null){engine.Dispose();engine=null;}engine=new Engine{Progress=Progress,Cancel=()=>cancel};}
        void ScanClick()
        {
            scan=null;grid.Rows.Clear();Connect();
            if(catalog.Count==0){catalog=StandardParts.Index(folder.Text,engine.Sizes,Progress,()=>cancel);ReadMappings();}
            scan=engine.Review(catalog);Display();Log("Scanned "+scan.Components.Count+" bodies, "+scan.Holes.Count+" recognized counterbores, "+scan.Issues.Count+" issues.");
        }
        void Display()
        {
            grid.Rows.Clear();foreach(var h in scan.Holes){int i=grid.Rows.Add(h.Status=="Ready",h.Owner,h.Size.Name,Inches(h.Grip),Inches(h.Required),h.Part==null?"—":Inches(h.Part.Length),h.Status);grid.Rows[i].Tag=h;if(h.Status!="Ready")grid.Rows[i].DefaultCellStyle.ForeColor=Color.DimGray;}
            status.Text=scan.Components.Count+" solid bodies • "+scan.Holes.Count+" chart-matched counterbores • "+scan.Holes.Count(h=>h.Status=="Ready")+" ready • "+scan.Issues.Count+" scan issues";ShowDetails();
        }
        void ShowDetails()
        {
            string issues=scan==null?"":String.Join(Environment.NewLine,scan.Issues);
            var h=grid.CurrentRow==null?null:grid.CurrentRow.Tag as Hole;
            detail.Text=(h==null?"":h.Owner+Environment.NewLine+"Counterbore Ø "+Inches(h.Bore)+", clearance Ø "+Inches(h.Drill)+", counterbore depth "+Inches(h.Depth)+Environment.NewLine+"Required under-head length: "+Inches(h.Required)+" in. "+(h.Part==null?"No selected library part.":h.Part.Path)+Environment.NewLine)+
                "Insertion seats each screw in the main assembly with a native Ground relationship. Screws stay at their placed position if source components move. The assembly is not saved automatically."+Environment.NewLine+issues;
        }
        void ChooseClick()
        {
            var h=grid.CurrentRow==null?null:grid.CurrentRow.Tag as Hole;if(h==null)throw new InvalidOperationException("Scan and select a counterbore row first.");
            MessageBox.Show(this,"In Standard Parts, select a socket-head cap screw of size "+h.Size.Name+" with under-head length at least "+Inches(h.Required)+" in. Choose the next available length above that value.","Required SHCS");
            var file=StandardParts.SelectAndGenerate(h.Part==null?null:h.Part.Path,engine.App);if(file==null)return;
            var part=Rules.ParsePart(file,engine.Sizes);
            if(part==null){using(var input=new LengthWindow(h.Size.Name,h.Required)){if(input.ShowDialog(this)!=DialogResult.OK)return;part=new CatalogPart{Path=file,Size=h.Size.Name,Diameter=h.Size.Diameter,Length=input.Length};}}
            if(part.Size!=h.Size.Name || part.Length+1e-9<h.Required)throw new InvalidOperationException("The selected screw has the wrong diameter or is shorter than the calculated requirement.");
            engine.ValidatePart(part);catalog.RemoveAll(p=>String.Equals(p.Path,file,StringComparison.OrdinalIgnoreCase));catalog.Add(part);WriteMapping(part);
            engine.Assembly.Activate();scan=engine.Review(catalog);Display();
        }
        void InsertClick()
        {
            if(scan==null || engine==null)throw new InvalidOperationException("Scan the assembly first.");
            var selected=grid.Rows.Cast<DataGridViewRow>().Where(r=>Convert.ToBoolean(r.Cells[0].Value)).Select(r=>(Hole)r.Tag).ToList();
            if(selected.Count==0)throw new InvalidOperationException("No Ready counterbores are checked.");
            int placed=engine.Populate(scan,selected,catalog);Log("Inserted "+placed+" SHCS in "+engine.Assembly.Name);
            scan=engine.Review(catalog);Display();MessageBox.Show(this,placed+" SHCS inserted and grounded in the main assembly. Review the assembly and save it when satisfied.","Populate SHCS");
        }
        static string Inches(double v){return (v/Rules.Inch).ToString("0.####",CultureInfo.InvariantCulture);}
        static void Log(string message){try{File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"PopulateSHCS.log"),DateTime.Now.ToString("s")+" "+message+Environment.NewLine);}catch(IOException){}}
        void ReadMappings()
        {
            if(!File.Exists(mappedFile))return;var doc=new XmlDocument{XmlResolver=null};using(var reader=XmlReader.Create(mappedFile,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))doc.Load(reader);
            foreach(XmlElement p in doc.SelectNodes("/Parts/Part")){var size=engine.Sizes.FirstOrDefault(s=>s.Name==p.GetAttribute("size"));double length;if(size!=null && File.Exists(p.GetAttribute("path")) && Double.TryParse(p.GetAttribute("lengthMeters"),NumberStyles.Float,CultureInfo.InvariantCulture,out length) && length>0 && !Double.IsInfinity(length))catalog.Add(new CatalogPart{Path=p.GetAttribute("path"),Size=size.Name,Diameter=size.Diameter,Length=length});}
        }
        void WriteMapping(CatalogPart part)
        {
            var doc=new XmlDocument{XmlResolver=null};var root=doc.CreateElement("Parts");doc.AppendChild(root);
            // Persist only explicitly selected parts, not thousands of library entries.
            if(File.Exists(mappedFile)){var old=new XmlDocument{XmlResolver=null};using(var r=XmlReader.Create(mappedFile,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))old.Load(r);foreach(XmlElement e in old.SelectNodes("/Parts/Part"))if(!String.Equals(e.GetAttribute("path"),part.Path,StringComparison.OrdinalIgnoreCase))root.AppendChild(doc.ImportNode(e,true));}
            var node=doc.CreateElement("Part");node.SetAttribute("path",part.Path);node.SetAttribute("size",part.Size);node.SetAttribute("lengthMeters",part.Length.ToString("R",CultureInfo.InvariantCulture));root.AppendChild(node);doc.Save(mappedFile);
        }
    }
    sealed class LengthWindow:Form
    {
        NumericUpDown input=new NumericUpDown{Minimum=.001M,Maximum=100,DecimalPlaces=4,Increment=.125M,Dock=DockStyle.Top};
        public double Length{get{return (double)input.Value*Rules.Inch;}}
        public LengthWindow(string size,double minimum){Text="Confirm selected SHCS length";Size=new System.Drawing.Size(460,210);StartPosition=FormStartPosition.CenterParent;Controls.Add(new Button{Text="Use this SHCS",Dock=DockStyle.Bottom,DialogResult=DialogResult.OK});Controls.Add(input);Controls.Add(new Label{Text="Selected size: "+size+". Enter its catalog under-head length in INCHES. Geometry will be checked before insertion.",Dock=DockStyle.Top,Height=65});input.Value=(decimal)(minimum/Rules.Inch);}
    }
}
