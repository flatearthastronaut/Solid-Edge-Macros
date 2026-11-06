using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Hammill.AddSketch;
public sealed class Plane{public string Name;}
public sealed class Planes{public Plane[] Values={new Plane{Name="Top (xy)"},new Plane{Name="Right (yz)"}};public int Count{get{return Values.Length;}}public Plane Item(int index){return Values[index-1];}}
public sealed class Layouts
{
    public int Count;public bool ReturnExisting;public bool Omitted;public int FailAt=-1;
    public object Add(object plane,object existing,out object status){Omitted=Object.ReferenceEquals(existing,Missing.Value);status=0;if(Count==FailAt)throw new IOException("Test failure");if(!ReturnExisting)Count++;else status=2;return new object();}
}
public sealed class AssemblyDoc{public Planes AsmRefPlanes=new Planes();public Layouts Layouts=new Layouts();}
public sealed class App{public bool DelayCompute;}
internal static class Tests
{
    static int n;static void Check(bool ok,string name){if(!ok)throw new Exception(name);n++;Console.WriteLine("PASS "+name);}
    static void Reject(Action a,string name){try{a();}catch{Check(true,name);return;}throw new Exception(name);}
    [STAThread]static int Main(){try{
        Application.EnableVisualStyles();var app=new App();var doc=new AssemblyDoc();var choices=SketchEngine.Planes(doc);
        Check(choices.Count==2 && choices[1].Name=="Right (yz)","reference planes preserve native order and names");
        int progress=0;Check(SketchEngine.Add(app,doc,choices[1],3,i=>progress=i)==3 && doc.Layouts.Count==3 && progress==3,"requested layouts are created and progress reported");
        Check(doc.Layouts.Omitted && !app.DelayCompute,"legacy optional argument and compute state preserved");
        app.DelayCompute=true;SketchEngine.Add(app,doc,choices[1],1,i=>{});Check(app.DelayCompute,"pre-existing delayed-compute state retained");
        Reject(()=>SketchEngine.Add(app,doc,null,1,i=>{}),"missing plane rejected");
        Reject(()=>SketchEngine.Add(app,doc,choices[1],0,i=>{}),"zero count rejected");
        Reject(()=>SketchEngine.Add(app,doc,choices[1],101,i=>{}),"oversized count rejected");
        doc.AsmRefPlanes.Values[1].Name="Renamed";Reject(()=>SketchEngine.Add(app,doc,choices[1],1,i=>{}),"renamed plane requires reload");
        doc=new AssemblyDoc();app.DelayCompute=false;doc.Layouts.FailAt=1;Reject(()=>SketchEngine.Add(app,doc,SketchEngine.Planes(doc)[0],3,i=>{}),"partial batch reports failure");Check(doc.Layouts.Count==1 && !app.DelayCompute,"partial failure preserves completed work and restores compute");
        doc.Layouts.FailAt=-1;doc.Layouts.ReturnExisting=true;Reject(()=>SketchEngine.Add(app,doc,SketchEngine.Planes(doc)[0],1,i=>{}),"reused layout is never reported as a newly added sketch");
        string work=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..",".work"));Directory.CreateDirectory(work);
        using(var form=new SketchForm(app,new AssemblyDoc())){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);form.Show();Application.DoEvents();Check(form.Controls[0].Controls.OfType<ComboBox>().Single().Items.Count==2,"shown window loads its reference plane choices");using(var bmp=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bmp,new Rectangle(Point.Empty,form.Size));bmp.Save(Path.Combine(work,"preview.png"));}form.Hide();}
        Check(true,"window renders with native plane list");Console.WriteLine(n+" checks passed.");return 0;
    }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
