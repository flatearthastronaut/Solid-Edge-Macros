using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

[assembly:System.Reflection.AssemblyTitle("Turbo Hole Table Callouts")]
[assembly:System.Reflection.AssemblyProduct("Turbo Hole Table Callouts")]
[assembly:System.Reflection.AssemblyVersion("0.4.0.0")]

namespace HoleTableFormatter
{
    internal static class Program
    {
        internal const string ProductName="Turbo Hole Table Callouts";
        internal const string Version="v0.4";
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
        readonly Button openButton=new Button{Text="Show selected table",AutoSize=true,Enabled=false};
        readonly Label summary=new Label{Dock=DockStyle.Top,Height=68,Padding=new Padding(10),AutoEllipsis=true};
        readonly ListBox tableList=new ListBox{Dock=DockStyle.Fill,IntegralHeight=false,DisplayMember="Caption"};
        readonly Dictionary<int,TableForm> windows=new Dictionary<int,TableForm>();
        ScanResult result;
        internal MainForm()
        {
            Text=Program.ProductName+" "+Program.Version;Program.SetIcon(this);
            StartPosition=FormStartPosition.CenterScreen;Size=new Size(650,410);MinimumSize=new Size(500,300);Font=new Font("Segoe UI",9);
            var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(8)};bar.Controls.Add(scanButton);bar.Controls.Add(openButton);
            var footer=new Label{Dock=DockStyle.Bottom,Height=44,Padding=new Padding(10),Text="Each table opens in its own window. Double-click a title to return to that table."};
            Controls.Add(tableList);Controls.Add(summary);Controls.Add(bar);Controls.Add(footer);
            summary.Text="Scan a saved draft to review its hole tables.";
            scanButton.Click+=delegate{Scan();};openButton.Click+=delegate{ShowSelected();};tableList.DoubleClick+=delegate{ShowSelected();};
        }
        void Scan()
        {
            scanButton.Enabled=false;openButton.Enabled=false;UseWaitCursor=true;
            foreach(var window in windows.Values)window.Enabled=false;
            summary.Text="Reading hole tables and saved hole presets...";summary.Refresh();
            try{LoadScan(Automation.Scan(null,Path.Combine(Path.GetTempPath(),"TurboHoleTableCallouts")),true);}
            catch(Exception error){summary.Text="Scan failed. Previous table windows are still available.";MessageBox.Show(this,error.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);}
            finally
            {
                UseWaitCursor=false;scanButton.Enabled=true;openButton.Enabled=result!=null;
                foreach(var window in windows.Values)window.Enabled=true;
            }
        }
        internal void LoadScan(ScanResult scanned,bool showWindows)
        {
            foreach(var window in windows.Values.ToArray())window.Close();windows.Clear();tableList.Items.Clear();result=scanned;
            foreach(TableInfo table in result.Tables.OrderBy(t=>t.Index))tableList.Items.Add(new TableListItem(table));
            if(tableList.Items.Count>0)tableList.SelectedIndex=0;
            UpdateSummary();openButton.Enabled=tableList.Items.Count>0;
            if(showWindows)foreach(TableInfo table in result.Tables.OrderBy(t=>t.Index))ShowTable(table);
        }
        void UpdateSummary()
        {
            summary.Text=Path.GetFileName(result.Document)+" — "+result.TableCount+" tables, "+result.Rows.Count+" rows. "+
                result.Rows.Count(r=>r.Eligible)+" ready; "+result.Rows.Count(r=>r.Status.StartsWith("Review"))+" need review; "+
                result.Rows.Count(r=>r.Overridden)+" custom rows preserved."+(result.Notes.Count==0?"":" "+String.Join(" ",result.Notes));
        }
        void ShowSelected(){var item=tableList.SelectedItem as TableListItem;if(item!=null)ShowTable(item.Table);}
        void ShowTable(TableInfo table)
        {
            TableForm window;
            if(windows.TryGetValue(table.Index,out window)&&!window.IsDisposed)
            {if(window.WindowState==FormWindowState.Minimized)window.WindowState=FormWindowState.Normal;window.Activate();return;}
            window=new TableForm(result,table,ApplyTable);windows[table.Index]=window;
            window.FormClosed+=delegate{windows.Remove(table.Index);};
            Rectangle area=Screen.FromControl(this).WorkingArea;
            window.Size=new Size(Math.Min(window.Width,area.Width),Math.Min(window.Height,area.Height));
            int offset=((table.Index-1)%6)*28;window.StartPosition=FormStartPosition.Manual;
            window.Location=new Point(area.Left+Math.Min(36+offset,Math.Max(0,area.Width-window.Width)),area.Top+Math.Min(30+offset,Math.Max(0,area.Height-window.Height)));
            window.Show(this);
        }
        int ApplyTable(int index){int changed=Automation.Apply(result,index);UpdateSummary();return changed;}
        sealed class TableListItem
        {
            internal readonly TableInfo Table;
            internal TableListItem(TableInfo table){Table=table;}
            public string Caption{get{return Presentation.Caption(Table);}}
        }
    }

    internal sealed class TableForm:Form
    {
        readonly ScanResult scan;
        readonly TableInfo table;
        readonly Func<int,int> apply;
        readonly Button applyButton=new Button{Text="Apply selected formats",AutoSize=true};
        readonly Label summary=new Label{Dock=DockStyle.Fill,AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft};
        internal readonly DataGridView Grid=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,
            RowHeadersVisible=false,AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells,SelectionMode=DataGridViewSelectionMode.FullRowSelect};
        internal TableForm(ScanResult scan,TableInfo table,Func<int,int> apply)
        {
            this.scan=scan;this.table=table;this.apply=apply;Text=Presentation.Caption(table)+" — "+Program.ProductName;Program.SetIcon(this);
            Size=new Size(1240,740);MinimumSize=new Size(850,480);Font=new Font("Segoe UI",9);
            var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(8)};bar.Controls.Add(applyButton);
            bar.Controls.Add(new Label{Text="Already Set",AutoSize=true,BackColor=Presentation.SetColor,Padding=new Padding(8),Margin=new Padding(16,0,0,0)});
            bar.Controls.Add(new Label{Text="Ready",AutoSize=true,BackColor=Presentation.ReadyColor,Padding=new Padding(8),Margin=new Padding(8,0,0,0)});
            bar.Controls.Add(new Label{Text="Review / no match",AutoSize=true,BackColor=Presentation.ReviewColor,Padding=new Padding(8),Margin=new Padding(8,0,0,0)});
            var heading=new Label{Dock=DockStyle.Top,AutoSize=true,MaximumSize=new Size(1150,0),Padding=new Padding(10,8,10,4),
                Font=new Font(Font,FontStyle.Bold),UseMnemonic=false,Text=Presentation.Title(table)};
            var header=new Panel{Dock=DockStyle.Top,Height=48,Padding=new Padding(10,0,10,0)};header.Controls.Add(summary);
            var footer=new Label{Dock=DockStyle.Bottom,Height=40,Padding=new Padding(10),Text="Only this table is updated. Custom rows and oddballs stay unchanged. Review and save the draft in Solid Edge."};
            Grid.AutoGenerateColumns=false;Grid.Columns.Add(new DataGridViewCheckBoxColumn{Name="Apply",Width=48});
            AddText("Hole",68);AddText("Category",140);AddText("Format",95);AddText("Status",140);AddText("Current callout",220);AddText("Proposed callout",220);AddText("Reason",280);
            Grid.DefaultCellStyle.WrapMode=DataGridViewTriState.True;
            Controls.Add(Grid);Controls.Add(header);Controls.Add(heading);Controls.Add(bar);Controls.Add(footer);
            Grid.CurrentCellDirtyStateChanged+=delegate{if(Grid.IsCurrentCellDirty)Grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
            applyButton.Click+=delegate
            {
                applyButton.Enabled=false;UseWaitCursor=true;
                try{CaptureSelections();int changed=apply(table.Index);Fill();summary.Text=changed+" callouts updated in this table. Review and save the draft in Solid Edge.";}
                catch(Exception error){MessageBox.Show(this,error.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);}
                finally{UseWaitCursor=false;applyButton.Enabled=scan.Rows.Any(r=>r.TableIndex==table.Index&&r.Eligible);}
            };
            FormClosing+=delegate{CaptureSelections();};Fill();
        }
        void AddText(string name,int width){Grid.Columns.Add(new DataGridViewTextBoxColumn{Name=name,HeaderText=name,Width=width,ReadOnly=true,SortMode=DataGridViewColumnSortMode.NotSortable});}
        internal void CaptureSelections()
        {
            Grid.EndEdit();foreach(DataGridViewRow row in Grid.Rows){var plan=(RowPlan)row.Tag;plan.Selected=plan.Eligible&&Convert.ToBoolean(row.Cells[0].Value);}
        }
        internal void Fill()
        {
            Grid.Rows.Clear();
            foreach(RowPlan plan in Presentation.OrderRows(scan.Rows.Where(r=>r.TableIndex==table.Index)))
            {
                int i=Grid.Rows.Add(plan.Selected,plan.Label,plan.Category,Presentation.FormatLabel(plan),
                    plan.Status=="Already set"?"Already Set":plan.Status,Presentation.Display(plan.Before),Presentation.Display(plan.Eligible||plan.Status=="Applied"?plan.After:""),plan.Reason);
                DataGridViewRow row=Grid.Rows[i];row.Tag=plan;row.Cells[0].ReadOnly=!plan.Eligible;
                Color color=Presentation.RowColor(plan);
                // Selected and disabled cells retain their status color, rather
                // than letting keyboard focus obscure a Ready/Already Set row.
                row.DefaultCellStyle.BackColor=color;row.DefaultCellStyle.SelectionBackColor=color;
                row.DefaultCellStyle.ForeColor=Color.Black;row.DefaultCellStyle.SelectionForeColor=Color.Black;
            }
            Grid.ClearSelection();var rows=scan.Rows.Where(r=>r.TableIndex==table.Index).ToList();
            summary.Text=Path.GetFileName(scan.Document)+" — "+rows.Count+" rows; "+rows.Count(r=>r.Eligible)+" ready; "+
                rows.Count(r=>r.Status=="Already set"||r.Status=="Applied")+" already set; sorted by hole.";
            if(rows.Count==0)summary.Text="No editable rows in this table. "+String.Join(" ",scan.Notes.Where(n=>n.StartsWith("Table "+table.Index+" ")));
            applyButton.Enabled=rows.Any(r=>r.Eligible);
        }
    }
}
