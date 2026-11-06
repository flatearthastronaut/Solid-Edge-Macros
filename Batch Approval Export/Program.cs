using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Hammill.Automation;
[assembly: AssemblyTitle("Hammill Batch Approval Export")]
[assembly: AssemblyCompany("Hammill")]
[assembly: AssemblyVersion("1.0.0.0")]
namespace Hammill.BatchApprovalExport
{
    internal static class Program
    {
        [STAThread]static void Main(){MacroHost.Run("Hammill Batch Approval Export",new[]{2},c=>new ExportForm(c.Application,c.Document));}
    }
    internal sealed class AssemblyChoice
    {
        public object Document;public string Path;
        public override string ToString(){return Path;}
    }
    internal sealed class ExportForm:Form
    {
        private readonly ComScope owned=new ComScope();
        internal ExportForm(object application,object draft)
        {
            MacroHost.Style(this,"Batch Approval Export",new Size(850,560));
            var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=2,RowCount=7};
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,145));root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            foreach(int height in new[]{56,40,40,40,45,230,50})root.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
            var heading=MacroHost.Heading("Batch Approval / Export");root.Controls.Add(heading,0,0);root.SetColumnSpan(heading,2);
            var assemblies=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};
            root.Controls.Add(new Label{Text="Linked assembly",AutoSize=true},0,1);root.Controls.Add(assemblies,1,1);
            var date=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="yyyy-MM-dd",Dock=DockStyle.Left};
            root.Controls.Add(new Label{Text="Filename date",AutoSize=true},0,2);root.Controls.Add(date,1,2);
            var folder=new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Text="Use each source document's folder"};
            var choose=new Button{Text="Output folder…",AutoSize=true};root.Controls.Add(choose,0,3);root.Controls.Add(folder,1,3);string output=null;
            choose.Click+=(s,e)=>{using(var dialog=new FolderBrowserDialog())if(dialog.ShowDialog(this)==DialogResult.OK){output=dialog.SelectedPath;folder.Text=output;}};
            var options=new FlowLayoutPanel{Dock=DockStyle.Fill};
            var formats=new[]{new CheckBox{Text="2D DWG",Checked=true,AutoSize=true},new CheckBox{Text="2D PDF",Checked=true,AutoSize=true},new CheckBox{Text="3D STEP",Checked=true,AutoSize=true},new CheckBox{Text="3D PDF",Checked=true,AutoSize=true}};
            options.Controls.AddRange(formats);root.Controls.Add(options,0,4);root.SetColumnSpan(options,2);
            var log=new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Both,WordWrap=false};root.Controls.Add(log,0,5);root.SetColumnSpan(log,2);
            var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var export=new Button{Text="Export selected",AutoSize=true};var preview=new Button{Text="Preview files",AutoSize=true};buttons.Controls.Add(export);buttons.Controls.Add(preview);root.Controls.Add(buttons,0,6);root.SetColumnSpan(buttons,2);Controls.Add(root);
            Func<List<ExportJob>> plan=()=>{var choice=assemblies.SelectedItem as AssemblyChoice;return ExportEngine.Plan(draft,(string)((dynamic)draft).FullName,choice==null?null:choice.Document,choice==null?null:choice.Path,date.Value,output,formats[0].Checked,formats[1].Checked,formats[2].Checked,formats[3].Checked);};
            Action refresh=()=>{try{log.Text=String.Join("\r\n",plan().Select(x=>x.ToString()))+"\r\n\r\nExisting files are skipped. Source documents are not saved or renamed.";}catch(Exception error){log.Text=error.Message;}};
            preview.Click+=(s,e)=>refresh();
            export.Click+=(s,e)=>{
                buttons.Enabled=false;ControlBox=false;
                try{var jobs=plan();log.Text=String.Join("\r\n",ExportEngine.Run(jobs,new SolidEdgeExporter(application),message=>{log.Text=message;log.Refresh();}));}
                catch(Exception error){log.Text=error.Message;}
                finally{try{((dynamic)draft).Activate();}catch(Exception error){log.AppendText("\r\nCould not reactivate draft: "+error.Message);}buttons.Enabled=true;ControlBox=true;}
            };
            Shown+=(s,e)=>{
                try
                {
                    using(var scope=new ComScope())
                    {
                        dynamic links=scope.Own(((dynamic)draft).ModelLinks);int count=(int)links.Count;var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        for(int i=1;i<=count;i++)using(var item=new ComScope())
                        {
                            try{dynamic link=item.Own(links.Item(i));dynamic model=owned.Own(link.ModelDocument);int type=(int)model.Type;
                                if(type==3 || type==10){string path=(string)model.FullName;if(seen.Add(path))assemblies.Items.Add(new AssemblyChoice{Document=model,Path=path});}}
                            catch(Exception error){log.AppendText("Model link "+i+": "+error.Message+"\r\n");}
                        }
                    }
                    if(assemblies.Items.Count>0)assemblies.SelectedIndex=0;
                    else{formats[2].Checked=false;formats[3].Checked=false;formats[2].Enabled=false;formats[3].Enabled=false;}
                    refresh();
                }
                catch(Exception error){log.Text=error.Message;}
            };
        }
        protected override void Dispose(bool disposing){if(disposing)owned.Dispose();base.Dispose(disposing);}
    }
}
