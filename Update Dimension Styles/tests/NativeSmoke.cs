using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Hammill.Automation;
using Hammill.StyleRepair;
internal static class NativeSmoke
{
    static object Find(object doc,string name)
    {
        using(var scope=new ComScope())
        {
            dynamic styles=scope.Own(((dynamic)doc).DimensionStyles);
            for(int i=1;i<=(int)styles.Count;i++)
            {dynamic item=styles.Item(i);if(((string)item.Name).Trim()==name)return item;ComScope.Release(item);}
        }
        throw new Exception("Style not found: "+name);
    }
    [STAThread]static int Main()
    {
        Application.EnableVisualStyles();
        using(new OleMessageFilter())using(var scope=new ComScope())
        {
            object fixture=null,previous=null;
            try
            {
                string project=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..")),root=Path.GetDirectoryName(project);
                string master=Path.Combine(project,"normal (new design).dft");
                string list=Path.Combine(root,"Add-ins","Hammill-Macro-Launcher","DimensionStyleCycle","Hammill.DimensionStyles.txt");
                string work=Path.Combine(project,"tests",".work");Directory.CreateDirectory(work);
                string path=Path.Combine(work,"repair-fixture-"+Guid.NewGuid().ToString("N")+".dft");File.Copy(master,path);
                dynamic app=scope.Own(Marshal.GetActiveObject("SolidEdge.Application"));dynamic docs=scope.Own(app.Documents);
                if((int)docs.Count>0)previous=scope.Own(app.ActiveDocument);
                fixture=docs.Open(path);
                using(var edit=new ComScope())
                {
                    dynamic styles=edit.Own(((dynamic)fixture).DimensionStyles);
                    foreach(string name in new[]{"3 2 place (vert)","3 4 place (vert)","4 2 place m[i] (vert)"})styles.Remove(name);
                    dynamic existing=edit.Own(Find(fixture,"1 3 place"));existing.Height=0.0042;
                }
                var required=Repair.Required(list);
                if(Repair.Missing(fixture,required).Length!=3)throw new Exception("Fixture did not lose the three test styles.");
                using(var form=new RepairForm((object)app,fixture,list,master))
                {
                    form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);form.Show();Application.DoEvents();
                    var actions=form.Controls[0].Controls.OfType<FlowLayoutPanel>().Single();var add=actions.Controls.OfType<Button>().Single(b=>b.Text=="Add missing styles");
                    if(!add.Enabled)throw new Exception("Native preflight did not enable repair: "+form.Controls[0].Controls.OfType<TextBox>().Last().Text);
                    add.PerformClick();Application.DoEvents();
                    using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(Path.Combine(work,"preview.png"));}
                    form.Hide();
                }
                if(Repair.Missing(fixture,required).Length!=0)throw new Exception("Native repair left missing cycle styles.");
                using(var check=new ComScope())
                {
                    dynamic existing=check.Own(Find(fixture,"1 3 place"));if(Math.Abs((double)existing.Height-0.0042)>1e-10)throw new Exception("Existing style formatting changed.");
                    dynamic added=check.Own(Find(fixture,"3 2 place (vert)"));if((int)added.TextOrientation!=2 || (int)added.PrimaryDecimalRoundOff!=4)throw new Exception("Native style precision/orientation not preserved.");
                }
                ((dynamic)fixture).Save();((dynamic)fixture).Close(false,Missing.Value,false);ComScope.Release(fixture);fixture=null;
                fixture=docs.Open(path);if(Repair.Missing(fixture,required).Length!=0)throw new Exception("Save/reopen lost styles.");
                using(var source=new MasterDocument((object)app,fixture,master))if(Repair.AddMissing(fixture,source.Document,required).Length!=0)throw new Exception("Repeat repair was not a no-op.");
                Console.WriteLine("PASS native UI repaired three missing cycle styles, preserved custom existing formatting and vertical precision, survived save/reopen, and reran without changes. Only a disposable project fixture was edited.");
                return 0;
            }
            catch(Exception error){Console.Error.WriteLine(error);return 1;}
            finally{try{if(fixture!=null){((dynamic)fixture).Close(false,Missing.Value,false);ComScope.Release(fixture);}}finally{if(previous!=null)((dynamic)previous).Activate();}}
        }
    }
}
