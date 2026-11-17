using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Hammill.Automation;
using Hammill.MacroLauncher;
[assembly:AssemblyTitle("Update Dimension Styles")]
[assembly:AssemblyVersion("1.0.0.0")]
namespace Hammill.StyleRepair
{
    internal static class Program
    {
        [STAThread]static void Main(){MacroHost.Run("Update Dimension Styles",new[]{2},c=>new RepairForm(c.Application,c.Document));}
    }
    internal sealed class RepairForm:Form
    {
        readonly object application,target;
        readonly string configurationPath;
        bool running;
        readonly TextBox master=new TextBox{Dock=DockStyle.Fill};
        readonly TextBox report=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical};
        readonly Button update=new Button{Text="Add missing styles",AutoSize=true,Enabled=false};
        internal RepairForm(object app,object document,string listPath=null,string masterPath=null)
        {
            application=app;target=document;
            configurationPath=listPath;
            MacroHost.Style(this,"Update Dimension Styles",new Size(580,310));Text="Update Dimension Styles";
            master.Text=masterPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"normal (new design).dft");
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12),ColumnCount=2,RowCount=4};
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,25));root.RowStyles.Add(new RowStyle(SizeType.Absolute,36));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,38));
            root.Controls.Add(new Label{Text="Master draft — target: "+(string)((dynamic)target).Name,AutoSize=true},0,0);root.SetColumnSpan(root.GetControlFromPosition(0,0),2);
            var browse=new Button{Text="Browse…",AutoSize=true};root.Controls.Add(master,0,1);root.Controls.Add(browse,1,1);
            root.Controls.Add(report,0,2);root.SetColumnSpan(report,2);
            var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var check=new Button{Text="Check",AutoSize=true};
            actions.Controls.Add(update);actions.Controls.Add(check);root.Controls.Add(actions,0,3);root.SetColumnSpan(actions,2);Controls.Add(root);
            browse.Click+=(s,e)=>{using(var dialog=new OpenFileDialog{Filter="Solid Edge draft (*.dft)|*.dft",FileName=master.Text})if(dialog.ShowDialog(this)==DialogResult.OK)master.Text=dialog.FileName;};
            master.TextChanged+=(s,e)=>update.Enabled=false;
            check.Click+=(s,e)=>Run(false);update.Click+=(s,e)=>Run(true);Shown+=(s,e)=>Run(false);
        }
        private void Run(bool apply)
        {
            if(running)return;running=true;
            update.Enabled=false;
            try
            {
                string settings=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Coop","SEMacroManager","settings.xml");
                string list=configurationPath ?? Path.Combine(MacroCatalog.ManagerSourceFolder(settings),"Hammill.DimensionStyles.txt");
                var required=Repair.Required(list);var missing=Repair.Missing(target,required);
                report.Text="Cycle list: "+list+"\r\n\r\n";
                if(missing.Length==0){report.AppendText("All "+required.Length+" cycle styles are present. No changes needed.");return;}
                using(var source=new MasterDocument(application,target,master.Text))
                {
                    var masterNames=Repair.Names(source.Document);
                    var absent=missing.Where(n=>!masterNames.ContainsKey(n)).ToArray();
                    if(absent.Length>0)throw new InvalidOperationException("Master is missing these required styles:\r\n"+String.Join("\r\n",absent));
                    if(!apply){report.AppendText("Missing from this drawing:\r\n"+String.Join("\r\n",missing)+"\r\n\r\nExisting styles will be preserved. The drawing will not be saved automatically.");update.Enabled=true;return;}
                    var added=Repair.AddMissing(target,source.Document,required);
                    report.AppendText("Added "+added.Length+" styles:\r\n"+String.Join("\r\n",added)+"\r\n\r\nSave the drawing when satisfied.");
                    if(Repair.Retained.Count>0)report.AppendText("\r\nSolid Edge would not remove these additional imported styles: "+String.Join(", ",Repair.Retained));
                }
            }
            catch(Exception error){report.Text=error.Message;}
            finally{running=false;}
        }
    }
}
