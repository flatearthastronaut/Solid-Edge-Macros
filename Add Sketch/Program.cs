using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Hammill.Automation;
[assembly: AssemblyTitle("Add Sketch")]
[assembly: AssemblyCompany("Hammill")]
[assembly: AssemblyVersion("1.2.0.0")]
namespace Hammill.AddSketch
{
    internal static class Program
    {
        [STAThread]static void Main(){MacroHost.Run("Add Sketch",new[]{3,10},c=>new SketchForm(c.Application,c.Document));}
    }
    internal sealed class SketchForm : Form
    {
        private readonly object app,document;
        private readonly ComboBox planes=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
        private readonly NumericUpDown quantity=new NumericUpDown{Minimum=1,Maximum=100,Value=1,Width=80,Anchor=AnchorStyles.Left};
        private readonly ProgressBar progress=new ProgressBar{Dock=DockStyle.Fill};
        private readonly Label status=new Label{Dock=DockStyle.Fill,AutoEllipsis=true,ForeColor=Color.DimGray};
        private readonly ToolTip details=new ToolTip{AutoPopDelay=30000};
        private readonly Button add=new Button{Text="Add sketches",AutoSize=true,Enabled=false};
        internal SketchForm(object application,object assembly)
        {
            app=application;document=assembly;MacroHost.Style(this,"Add Sketch",new Size(390,204));
            // Use the native title bar as the only heading. Keep the shared host's
            // icon and DPI handling without changing the other macro windows.
            Text="Add Sketch";MaximizeBox=false;
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12),ColumnCount=2,RowCount=5};
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,145));root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            foreach(int height in new[]{32,32,14})root.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
            root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,36));
            root.Controls.Add(new Label{Text="Reference plane",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);root.Controls.Add(planes,1,0);
            root.Controls.Add(new Label{Text="Number of sketches",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,1);root.Controls.Add(quantity,1,1);
            root.Controls.Add(progress,0,2);root.SetColumnSpan(progress,2);root.Controls.Add(status,0,3);root.SetColumnSpan(status,2);
            var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,Margin=Padding.Empty,WrapContents=false};
            var reload=new Button{Text="Reload planes",AutoSize=true};
            buttons.Controls.Add(add);buttons.Controls.Add(reload);root.Controls.Add(buttons,0,4);root.SetColumnSpan(buttons,2);Controls.Add(root);
            reload.Click+=(s,e)=>LoadPlanes();Shown+=(s,e)=>LoadPlanes();
            add.Click+=(s,e)=>{add.Enabled=false;try{progress.Value=0;progress.Maximum=(int)quantity.Value;int n=SketchEngine.Add(app,document,planes.SelectedItem as PlaneChoice,(int)quantity.Value,i=>{progress.Value=i;progress.Refresh();});SetStatus("Added "+n+" sketch(es). Save the assembly when finished.");}
                catch(Exception error){SetStatus(error.Message);}finally{add.Enabled=planes.SelectedItem is PlaneChoice;}};
            Disposed+=(s,e)=>details.Dispose();
        }
        private void SetStatus(string message)
        {
            // Long COM diagnostics remain available on hover; the user can also
            // enlarge the window to read them without sacrificing the compact default.
            status.Text=message;details.SetToolTip(status,message);
        }
        private void LoadPlanes()
        {
            // Clear stale choices even if the collection itself cannot be read.
            // A failed reload must never leave the old Add action enabled.
            add.Enabled=false;planes.Items.Clear();
            try{var scan=SketchEngine.Planes(document);var choices=scan.Choices;planes.Items.AddRange(choices.Cast<object>().ToArray());
                planes.SelectedIndex=choices.FindIndex(p=>p.Name.Equals("Right (yz)",StringComparison.OrdinalIgnoreCase));if(planes.SelectedIndex<0 && choices.Count>0)planes.SelectedIndex=0;
                add.Enabled=planes.SelectedItem is PlaneChoice;
                string message=choices.Count==0?"No usable named reference planes found.":choices.Count+" reference plane(s) loaded.";
                if(scan.Notices.Count>0)message+="\r\nSkipped "+scan.Notices.Count+" unnamed or unreadable entries.";
                SetStatus(message);}
            catch(Exception error){SetStatus("Cannot load planes. Finish the active Solid Edge command, then click Reload planes.\r\n"+error.Message);}
        }
    }
}
