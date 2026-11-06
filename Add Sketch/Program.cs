using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Hammill.Automation;
[assembly: AssemblyTitle("Hammill Add Sketch")]
[assembly: AssemblyCompany("Hammill")]
[assembly: AssemblyVersion("1.0.0.0")]
namespace Hammill.AddSketch
{
    internal static class Program
    {
        [STAThread]static void Main(){MacroHost.Run("Hammill Add Sketch",new[]{3,10},c=>new SketchForm(c.Application,c.Document));}
    }
    internal sealed class SketchForm : Form
    {
        private readonly object app,document;
        private readonly ComboBox planes=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
        private readonly NumericUpDown quantity=new NumericUpDown{Minimum=1,Maximum=100,Value=1,Dock=DockStyle.Fill};
        private readonly ProgressBar progress=new ProgressBar{Dock=DockStyle.Fill};
        private readonly Label status=new Label{Dock=DockStyle.Fill};
        internal SketchForm(object application,object assembly)
        {
            app=application;document=assembly;MacroHost.Style(this,"Add Sketch",new Size(550,330));
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=2,RowCount=6};
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,155));root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            foreach(int height in new[]{56,40,40,30,75,40})root.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
            var heading=MacroHost.Heading("Add Sketch");root.Controls.Add(heading,0,0);root.SetColumnSpan(heading,2);
            root.Controls.Add(new Label{Text="Reference plane",AutoSize=true},0,1);root.Controls.Add(planes,1,1);
            root.Controls.Add(new Label{Text="Number of sketches",AutoSize=true},0,2);root.Controls.Add(quantity,1,2);
            root.Controls.Add(progress,0,3);root.SetColumnSpan(progress,2);root.Controls.Add(status,0,4);root.SetColumnSpan(status,2);
            var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};
            var add=new Button{Text="Add sketches",AutoSize=true};var reload=new Button{Text="Reload planes",AutoSize=true};
            buttons.Controls.Add(add);buttons.Controls.Add(reload);root.Controls.Add(buttons,0,5);root.SetColumnSpan(buttons,2);Controls.Add(root);
            reload.Click+=(s,e)=>LoadPlanes();Shown+=(s,e)=>LoadPlanes();
            add.Click+=(s,e)=>{add.Enabled=false;try{progress.Value=0;progress.Maximum=(int)quantity.Value;int n=SketchEngine.Add(app,document,planes.SelectedItem as PlaneChoice,(int)quantity.Value,i=>{progress.Value=i;progress.Refresh();});status.Text="Added "+n+" sketch(es). The assembly has not been saved.";}
                catch(Exception error){status.Text=error.Message;}finally{add.Enabled=true;}};
        }
        private void LoadPlanes()
        {
            try{var choices=SketchEngine.Planes(document);planes.Items.Clear();planes.Items.AddRange(choices.Cast<object>().ToArray());
                planes.SelectedIndex=choices.FindIndex(p=>p.Name.Equals("Right (yz)",StringComparison.OrdinalIgnoreCase));if(planes.SelectedIndex<0 && choices.Count>0)planes.SelectedIndex=0;
                status.Text=choices.Count==0?"No assembly reference planes found.":"Add empty assembly sketches on the selected plane. Save the assembly when finished.";}
            catch(Exception error){status.Text=error.Message;}
        }
    }
}
