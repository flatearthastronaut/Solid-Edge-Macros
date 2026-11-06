using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Hammill.Automation;
[assembly: AssemblyTitle("Hammill Occurrence Viewer")]
[assembly: AssemblyCompany("Hammill")]
[assembly: AssemblyVersion("1.0.0.0")]
namespace Hammill.OccurrenceViewer
{
    internal static class Program
    {
        [STAThread]static void Main(){MacroHost.Run("Hammill Occurrence Viewer",new[]{3,10},c=>new ViewerForm(c.Application,c.Document));}
    }
    internal sealed class ViewerForm:Form
    {
        internal ViewerForm(object application,object assembly)
        {
            MacroHost.Style(this,"Occurrence Viewer",new Size(640,410));
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=2,RowCount=6};
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,145));root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            foreach(int height in new[]{56,42,36,36,150,44})root.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
            var heading=MacroHost.Heading("Occurrence Viewer");root.Controls.Add(heading,0,0);root.SetColumnSpan(heading,2);
            var filter=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};filter.Items.AddRange(VisibilityEngine.Filters);filter.SelectedIndex=0;
            root.Controls.Add(new Label{Text="Occurrence property",AutoSize=true},0,1);root.Controls.Add(filter,1,1);
            var condition=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};condition.Items.AddRange(new object[]{"Yes / enabled","No / disabled"});condition.SelectedIndex=0;
            root.Controls.Add(new Label{Text="Match value",AutoSize=true},0,2);root.Controls.Add(condition,1,2);
            var nested=new CheckBox{Text="Include subassemblies",AutoSize=true};root.Controls.Add(nested,1,3);
            var result=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Text="Preview matching occurrences, then show or hide them.\r\nVisibility changes are not automatically saved."};root.Controls.Add(result,0,4);root.SetColumnSpan(result,2);
            var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};
            var hide=new Button{Text="Hide matching",AutoSize=true};var show=new Button{Text="Show matching",AutoSize=true};var preview=new Button{Text="Preview",AutoSize=true};
            buttons.Controls.Add(hide);buttons.Controls.Add(show);buttons.Controls.Add(preview);root.Controls.Add(buttons,0,5);root.SetColumnSpan(buttons,2);Controls.Add(root);
            Action<bool?> execute=value=>{buttons.Enabled=false;try{result.Text=VisibilityEngine.Run(application,assembly,filter.SelectedIndex,condition.SelectedIndex==0,nested.Checked,value).ToString();}
                catch(Exception error){result.Text=error.Message;}finally{buttons.Enabled=true;}};
            preview.Click+=(s,e)=>execute(null);show.Click+=(s,e)=>execute(true);hide.Click+=(s,e)=>execute(false);
            filter.SelectedIndexChanged+=(s,e)=>condition.Enabled=filter.SelectedIndex!=0;condition.Enabled=false;
        }
    }
}
