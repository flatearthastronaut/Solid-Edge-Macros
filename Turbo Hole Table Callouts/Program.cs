using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

[assembly:System.Reflection.AssemblyTitle("Turbo Hole Table Callouts")]
[assembly:System.Reflection.AssemblyProduct("Turbo Hole Table Callouts")]
[assembly:System.Reflection.AssemblyVersion("0.7.0.0")]

namespace HoleTableFormatter
{
    internal static class Program
    {
        internal const string ProductName="Turbo Hole Table Callouts";
        internal const string Version="v0.7";
        [STAThread] static int Main(string[] args)
        {
            try
            {
                using(var filter=new BusyFilter())
                {
                    if(args.Length==3 && args[0]=="--scan")
                    {
                        string output=Path.GetFullPath(args[2]);Directory.CreateDirectory(Path.GetDirectoryName(output));
                        Automation.Scan(args[1],Path.GetDirectoryName(output)).Save(output);return 0;
                    }
                    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new MainForm());return 0;
                }
            }
            catch(Exception error)
            {
                if(args.Length==3 && args[0]=="--scan")File.WriteAllText(Path.GetFullPath(args[2])+".error.txt",error.ToString());
                else MessageBox.Show(error.Message,ProductName,MessageBoxButtons.OK,MessageBoxIcon.Error);
                return 1;
            }
        }
        internal static void SetIcon(Form form)
        {
            using(var stream=typeof(Program).Assembly.GetManifestResourceStream("TurboHoleTableCallouts.Icon"))
                if(stream!=null)using(var icon=new Icon(stream))
                {
                    Icon owned=(Icon)icon.Clone();form.Icon=owned;
                    form.Disposed+=delegate{owned.Dispose();};
                }
        }
    }

    // One controller owns the scan and window lifetimes. All COM calls and UI
    // remain on one STA, following the Solid Edge SDK Automation pattern.
    internal sealed class MainForm:Form
    {
        readonly Button scanButton=new Button{Text="Scan active draft",AutoSize=true};
        internal readonly Button RefreshButton=new Button{Text="Refresh",AutoSize=true,Enabled=false};
        readonly Button openButton=new Button{Text="Show selected table",AutoSize=true,Enabled=false};
        readonly Label summary=new Label{Dock=DockStyle.Top,Height=68,Padding=new Padding(10),AutoEllipsis=true};
        readonly ListBox tableList=new ListBox{Dock=DockStyle.Fill,IntegralHeight=false,DisplayMember="Caption"};
        readonly Dictionary<int,TableForm> windows=new Dictionary<int,TableForm>();
        readonly Func<string,string,ScanResult> scanDraft;
        readonly Action<Exception> reportError;
        bool busy;
        ScanResult result;
        internal MainForm():this(Automation.Scan,null){}
        internal MainForm(Func<string,string,ScanResult> scanDraft,Action<Exception> reportError)
        {
            this.scanDraft=scanDraft;
            this.reportError=reportError??(error=>MessageBox.Show(this,error.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error));
            Text=Program.ProductName+" "+Program.Version;Program.SetIcon(this);
            StartPosition=FormStartPosition.CenterScreen;Size=new Size(650,410);MinimumSize=new Size(500,300);Font=new Font("Segoe UI",9);
            var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(8)};bar.Controls.Add(scanButton);bar.Controls.Add(RefreshButton);bar.Controls.Add(openButton);
            var footer=new Label{Dock=DockStyle.Bottom,Height=44,Padding=new Padding(10),Text="Each table opens in its own window. Double-click a title to return to that table."};
            Controls.Add(tableList);Controls.Add(summary);Controls.Add(bar);Controls.Add(footer);
            summary.Text="Scan a saved draft to review its hole tables.";
            scanButton.Click+=delegate{Scan(false);};RefreshButton.Click+=delegate{Scan(true);};
            openButton.Click+=delegate{ShowSelected();};tableList.DoubleClick+=delegate{ShowSelected();};
        }
        void Scan(bool refresh)
        {
            if(busy||(refresh&&result==null))return;
            Form requester=Form.ActiveForm;SetBusy(true);
            summary.Text="Reading hole tables and saved hole presets...";summary.Refresh();
            // Refresh uses the source draft, even if another document is now active.
            // Scan reads a SaveCopyAs of its current in-memory state, including edits
            // to hole groups that have not been saved. Replace every open preview
            // only after a complete scan so they share one current set of row IDs.
            try{LoadScan(scanDraft(refresh?result.Document:null,Path.Combine(Path.GetTempPath(),"TurboHoleTableCallouts")),!refresh);}
            catch(Exception error){summary.Text="Scan failed. Previous table windows are still available.";reportError(error);}
            finally
            {
                SetBusy(false);
                // Solid Edge activates the source when its temporary copy closes.
                // Return focus to the requesting preview after its rows are fresh.
                if(refresh){if(requester!=null&&!requester.IsDisposed)requester.Activate();else Activate();}
            }
        }
        void SetBusy(bool value)
        {
            busy=value;UseWaitCursor=value;scanButton.Enabled=!value;
            RefreshButton.Enabled=!value&&result!=null;openButton.Enabled=!value&&tableList.Items.Count>0;
            foreach(var window in windows.Values)window.Enabled=!value;
        }
        internal void LoadScan(ScanResult scanned,bool showWindows)
        {
            var selected=tableList.SelectedItem as TableListItem;
            bool sameDocument=result!=null&&String.Equals(result.Document,scanned.Document,StringComparison.OrdinalIgnoreCase);
            // Retain window locations/sizes on refresh. Removed tables and a newly
            // scanned document discard their old windows; never reuse stale plans.
            foreach(var entry in windows.ToArray())
                if(!sameDocument||!scanned.Tables.Any(t=>t.Index==entry.Key))entry.Value.Close();
            tableList.Items.Clear();result=scanned;
            foreach(TableInfo table in result.Tables.OrderBy(t=>t.Index))tableList.Items.Add(new TableListItem(table));
            if(tableList.Items.Count>0)tableList.SelectedIndex=0;
            if(sameDocument&&selected!=null)
                foreach(TableListItem item in tableList.Items)if(item.Table.Index==selected.Table.Index){tableList.SelectedItem=item;break;}
            foreach(var entry in windows)entry.Value.LoadScan(result,result.Tables.Single(t=>t.Index==entry.Key));
            UpdateSummary();openButton.Enabled=tableList.Items.Count>0;RefreshButton.Enabled=true;
            if(showWindows)foreach(TableInfo table in result.Tables.OrderBy(t=>t.Index))ShowTable(table);
        }
        void UpdateSummary()
        {
            summary.Text=Path.GetFileName(result.Document)+" — "+result.TableCount+" tables, "+result.Rows.Count+" rows. "+
                result.Rows.Count(r=>r.Eligible)+" hole types found; "+result.Rows.Count(r=>r.Status.StartsWith("Review"))+" need review; "+
                result.Rows.Count(r=>r.Status=="Custom row preserved")+" custom rows preserved."+(result.Notes.Count==0?"":" "+String.Join(" ",result.Notes));
        }
        void ShowSelected(){var item=tableList.SelectedItem as TableListItem;if(item!=null)ShowTable(item.Table);}
        void ShowTable(TableInfo table)
        {
            TableForm window=GetTableWindow(table);
            if(window.WindowState==FormWindowState.Minimized)window.WindowState=FormWindowState.Normal;
            if(!window.Visible)window.Show(this);else window.Activate();
        }
        internal TableForm GetTableWindow(TableInfo table)
        {
            TableForm window;
            if(windows.TryGetValue(table.Index,out window)&&!window.IsDisposed)return window;
            window=new TableForm(result,table,ApplyTable,()=>Scan(true));windows[table.Index]=window;
            window.FormClosed+=delegate{windows.Remove(table.Index);};
            Rectangle area=Screen.FromControl(this).WorkingArea;
            window.Size=new Size(Math.Min(window.Width,area.Width),Math.Min(window.Height,area.Height));
            int offset=((table.Index-1)%6)*28;window.StartPosition=FormStartPosition.Manual;
            window.Location=new Point(area.Left+Math.Min(36+offset,Math.Max(0,area.Width-window.Width)),area.Top+Math.Min(30+offset,Math.Max(0,area.Height-window.Height)));
            return window;
        }
        int ApplyTable(int index)
        {
            if(busy)throw new InvalidOperationException("Wait for the current scan or apply to finish.");
            // COM may pump messages during a write. Disable Refresh everywhere
            // so it cannot replace the shared plans while Apply is using them.
            SetBusy(true);
            try{int changed=Automation.Apply(result,index);UpdateSummary();return changed;}
            finally{SetBusy(false);}
        }
        sealed class TableListItem
        {
            internal readonly TableInfo Table;
            internal TableListItem(TableInfo table){Table=table;}
            public string Caption{get{return Presentation.Caption(Table);}}
        }
    }

    internal sealed class TableForm:Form
    {
        ScanResult scan;
        TableInfo table;
        readonly Func<int,int> apply;
        readonly Button applyButton=new Button{Text="Apply selected formats",AutoSize=true};
        internal readonly Button RefreshButton=new Button{Text="Refresh",AutoSize=true};
        readonly Label heading=new Label{Dock=DockStyle.Top,AutoSize=true,MaximumSize=new Size(1150,0),Padding=new Padding(10,8,10,4),UseMnemonic=false};
        readonly Label summary=new Label{Dock=DockStyle.Fill,AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft};
        readonly Dictionary<string,int[]> mixedGroups=new Dictionary<string,int[]>();
        internal int MixedGroupCount {get{return mixedGroups.Count;}}
        internal readonly Label MixedWarning=new Label{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(10,4,10,6),
            ForeColor=Color.Red,UseMnemonic=false,Text=Presentation.MixedTypeWarning,Visible=false};
        internal readonly DataGridView Grid=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,
            RowHeadersVisible=false,AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells,SelectionMode=DataGridViewSelectionMode.FullRowSelect};
        internal TableForm(ScanResult scan,TableInfo table,Func<int,int> apply,Action refresh)
        {
            this.apply=apply;Program.SetIcon(this);
            Size=new Size(1120,740);MinimumSize=new Size(850,480);Font=new Font("Segoe UI",9);
            MixedWarning.Font=new Font(Font,FontStyle.Bold);
            var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(8)};bar.Controls.Add(applyButton);bar.Controls.Add(RefreshButton);
            bar.Controls.Add(new Label{Text="Already Set",AutoSize=true,BackColor=Presentation.SetColor,Padding=new Padding(8),Margin=new Padding(16,0,0,0)});
            bar.Controls.Add(new Label{Text="Hole type found",AutoSize=true,BackColor=Presentation.FoundColor,Padding=new Padding(8),Margin=new Padding(8,0,0,0)});
            bar.Controls.Add(new Label{Text="Review / no match",AutoSize=true,BackColor=Presentation.ReviewColor,Padding=new Padding(8),Margin=new Padding(8,0,0,0)});
            heading.Font=new Font(Font,FontStyle.Bold);
            var header=new Panel{Dock=DockStyle.Top,Height=48,Padding=new Padding(10,0,10,0)};header.Controls.Add(summary);
            var footer=new Label{Dock=DockStyle.Bottom,Height=40,Padding=new Padding(10),Text="Only this table is updated. Custom rows and oddballs stay unchanged. Review and save the draft in Solid Edge."};
            Grid.AutoGenerateColumns=false;Grid.Columns.Add(new DataGridViewCheckBoxColumn{Name="Apply",Width=48});
            AddText("Hole",44);AddText("Category",140);AddText("Format",108);AddText("Status",98);AddText("Current callout",184);AddText("Proposed callout",196);AddText("Reason",280);
            Grid.DefaultCellStyle.WrapMode=DataGridViewTriState.True;
            Controls.Add(Grid);Controls.Add(header);Controls.Add(MixedWarning);Controls.Add(heading);Controls.Add(bar);Controls.Add(footer);
            Grid.RowPostPaint+=DrawGroupBorder;
            Grid.CurrentCellDirtyStateChanged+=delegate{if(Grid.IsCurrentCellDirty)Grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
            RefreshButton.Click+=delegate{refresh();};
            applyButton.Click+=delegate
            {
                applyButton.Enabled=false;UseWaitCursor=true;
                try{CaptureSelections();int changed=apply(this.table.Index);Fill();summary.Text=changed+" callouts updated in this table. Review and save the draft in Solid Edge.";}
                catch(Exception error){MessageBox.Show(this,error.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);}
                finally{UseWaitCursor=false;applyButton.Enabled=this.scan.Rows.Any(r=>r.TableIndex==this.table.Index&&r.Eligible);}
            };
            FormClosing+=delegate{CaptureSelections();};LoadScan(scan,table);
        }
        internal void LoadScan(ScanResult scanned,TableInfo refreshedTable)
        {
            scan=scanned;table=refreshedTable;
            Text=Presentation.Caption(table)+" — "+Program.ProductName;heading.Text=Presentation.Title(table);
            // A regroup can rename/reorder rows. Fresh plans replace the old grid
            // together; old checkbox selections must not follow reused row labels.
            Fill();
        }
        void AddText(string name,int width){Grid.Columns.Add(new DataGridViewTextBoxColumn{Name=name,HeaderText=name,Width=width,ReadOnly=true,SortMode=DataGridViewColumnSortMode.NotSortable});}
        internal void CaptureSelections()
        {
            Grid.EndEdit();foreach(DataGridViewRow row in Grid.Rows){var plan=(RowPlan)row.Tag;plan.Selected=plan.Eligible&&Convert.ToBoolean(row.Cells[0].Value);}
        }
        internal void Fill()
        {
            mixedGroups.Clear();Grid.Rows.Clear();
            foreach(RowPlan plan in Presentation.OrderRows(scan.Rows.Where(r=>r.TableIndex==table.Index)))
            {
                int i=Grid.Rows.Add(plan.Selected,plan.Label,plan.Category,Presentation.FormatLabel(plan),
                    plan.Status=="Already set"?"Already Set":plan.Status,Presentation.Display(plan.Before),Presentation.Display(plan.Eligible||plan.Status=="Applied"?plan.After:""),plan.Reason);
                DataGridViewRow row=Grid.Rows[i];row.Tag=plan;row.Cells[0].ReadOnly=!plan.Eligible;
                Color color=Presentation.RowColor(plan);
                // Selected and disabled cells retain their status color, rather
                // than letting keyboard focus obscure a found/already-set row.
                row.DefaultCellStyle.BackColor=color;row.DefaultCellStyle.SelectionBackColor=color;
                row.DefaultCellStyle.ForeColor=Color.Black;row.DefaultCellStyle.SelectionForeColor=Color.Black;
            }
            Grid.ClearSelection();var rows=scan.Rows.Where(r=>r.TableIndex==table.Index).ToList();
            var mixed=Presentation.MixedHoleGroups(rows);
            for(int i=0;i<Grid.Rows.Count;i++)
            {
                string letter=Presentation.HoleLetter(((RowPlan)Grid.Rows[i].Tag).Label);
                if(!mixed.Contains(letter))continue;
                int[] ends;if(!mixedGroups.TryGetValue(letter,out ends)){ends=new[]{i,i};mixedGroups.Add(letter,ends);}else ends[1]=i;
            }
            MixedWarning.Visible=mixedGroups.Count>0;Grid.Invalidate();
            summary.Text=Path.GetFileName(scan.Document)+" — "+rows.Count+" rows; "+rows.Count(r=>r.Eligible)+" hole types found; "+
                rows.Count(r=>r.Status=="Already set"||r.Status=="Applied")+" already set; sorted by hole.";
            if(rows.Count==0)summary.Text="No editable rows in this table. "+String.Join(" ",scan.Notes.Where(n=>n.StartsWith("Table "+table.Index+" ")));
            applyButton.Enabled=rows.Any(r=>r.Eligible);
        }
        void DrawGroupBorder(object sender,DataGridViewRowPostPaintEventArgs e)
        {
            var plan=Grid.Rows[e.RowIndex].Tag as RowPlan;if(plan==null)return;
            int[] ends;if(!mixedGroups.TryGetValue(Presentation.HoleLetter(plan.Label),out ends))return;
            // Draw a continuous four-pixel outline around the whole group, not
            // boxes around individual rows. RowPostPaint keeps it visible above
            // the existing green/yellow/red fills while scrolling or selecting.
            float left=e.RowBounds.Left+2,right=Math.Min(e.RowBounds.Right,Grid.ClientRectangle.Right)-3;
            float top=e.RowBounds.Top,bottom=e.RowBounds.Bottom;
            using(var pen=new Pen(Color.Red,4))
            {
                e.Graphics.DrawLine(pen,left,top,left,bottom);e.Graphics.DrawLine(pen,right,top,right,bottom);
                if(e.RowIndex==ends[0])e.Graphics.DrawLine(pen,left,top+2,right,top+2);
                if(e.RowIndex==ends[1])e.Graphics.DrawLine(pen,left,bottom-2,right,bottom-2);
            }
        }
    }
}
