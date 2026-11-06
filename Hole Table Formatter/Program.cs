using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HoleTableFormatter
{
    internal static class Program
    {
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
                else MessageBox.Show(error.Message,"Hole Table Formatter",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return 1;
            }
        }
    }
    internal sealed class MainForm:Form
    {
        readonly Button scanButton=new Button{Text="Scan active draft",AutoSize=true};
        readonly Button applyButton=new Button{Text="Apply selected formats",AutoSize=true,Enabled=false};
        readonly Label summary=new Label{Dock=DockStyle.Fill,AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft};
        readonly DataGridView grid=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,
            RowHeadersVisible=false,AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells,SelectionMode=DataGridViewSelectionMode.FullRowSelect};
        ScanResult result;
        internal MainForm()
        {
            Text="Hole Table Formatter v0.1";StartPosition=FormStartPosition.CenterScreen;Size=new Size(1180,720);MinimumSize=new Size(880,500);
            Font=new Font("Segoe UI",9);
            var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=44,Padding=new Padding(8)};
            bar.Controls.Add(scanButton);bar.Controls.Add(applyButton);
            var footer=new Label{Dock=DockStyle.Bottom,Height=38,Padding=new Padding(10),Text="Custom rows and unmatched holes stay unchanged. Review the drawing and save it in Solid Edge after applying."};
            var header=new Panel{Dock=DockStyle.Top,Height=54,Padding=new Padding(10)};header.Controls.Add(summary);
            grid.AutoGenerateColumns=false;
            grid.Columns.Add(new DataGridViewCheckBoxColumn{Name="Apply",Width=48});
            AddText("Table",48);AddText("Hole",60);AddText("Category",140);AddText("Format",55);AddText("Status",135);
            AddText("Current callout",210);AddText("Proposed callout",210);AddText("Reason",260);
            grid.DefaultCellStyle.WrapMode=DataGridViewTriState.True;
            Controls.Add(grid);Controls.Add(header);Controls.Add(bar);Controls.Add(footer);
            summary.Text="Scan a saved draft to match hole dimensions against your saved hole settings.";
            scanButton.Click+=delegate{Run(Scan);};applyButton.Click+=delegate{Run(Apply);};
            grid.CurrentCellDirtyStateChanged+=delegate{if(grid.IsCurrentCellDirty)grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
        }
        void AddText(string name,int width){grid.Columns.Add(new DataGridViewTextBoxColumn{Name=name,HeaderText=name,Width=width,ReadOnly=true,SortMode=DataGridViewColumnSortMode.NotSortable});}
        void Run(Action action)
        {
            scanButton.Enabled=false;applyButton.Enabled=false;UseWaitCursor=true;
            try{action();}catch(Exception error){MessageBox.Show(this,error.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);}
            finally{UseWaitCursor=false;scanButton.Enabled=true;applyButton.Enabled=result!=null&&result.Rows.Any(r=>r.Eligible);}
        }
        void Scan()
        {
            summary.Text="Reading hole tables and saved hole presets...";summary.Refresh();
            result=null;grid.Rows.Clear();
            result=Automation.Scan(null,Path.Combine(Path.GetTempPath(),"HammillHoleTableFormatter"));Fill();
            summary.Text=Path.GetFileName(result.Document)+" — "+result.TableCount+" tables, "+result.Rows.Count+" rows. "+
                result.Rows.Count(r=>r.Eligible)+" ready; "+result.Rows.Count(r=>r.Status.StartsWith("Review"))+" need review; "+
                result.Rows.Count(r=>r.Overridden)+" custom rows preserved."+
                (result.Notes.Count==0?"":" "+String.Join(" ",result.Notes));
        }
        // Display common Solid Edge symbols in the preview only. The stored
        // callout and native %F assignment retain their original property codes.
        static string Display(string text){return (text??"").Replace("%RT",Environment.NewLine).Replace("%DI","\u00D8").Replace("%PM","\u00B1").Replace("%DG","\u00B0");}
        void Fill()
        {
            grid.Rows.Clear();
            foreach(RowPlan plan in result.Rows.OrderBy(r=>r.Eligible||r.Status=="Applied"?0:r.Status.StartsWith("Review")?1:r.Overridden?2:3).ThenBy(r=>r.TableIndex).ThenBy(r=>r.RowIndex))
            {
                int i=grid.Rows.Add(plan.Selected,plan.TableIndex,plan.Label,plan.Category,
                    plan.Decision==null||plan.Decision.Slot==0?"":"%F"+plan.Decision.Slot,plan.Status,Display(plan.Before),
                    Display(plan.Eligible||plan.Status=="Applied"?plan.After:""),plan.Reason);
                grid.Rows[i].Tag=plan;grid.Rows[i].Cells[0].ReadOnly=!plan.Eligible;
                if(!plan.Eligible)grid.Rows[i].Cells[0].Style.BackColor=SystemColors.Control;
            }
            grid.ClearSelection();
        }
        void Apply()
        {
            grid.EndEdit();
            foreach(DataGridViewRow row in grid.Rows){var plan=(RowPlan)row.Tag;plan.Selected=plan.Eligible&&Convert.ToBoolean(row.Cells[0].Value);}
            int changed=Automation.Apply(result);Fill();summary.Text=changed+" callouts updated. The draft is open and unsaved; review it in Solid Edge.";
        }
    }
}
