using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Hammill.AddSketch;
public sealed class Plane{private string name;public bool FailName;public string Name{get{if(FailName)throw new COMException("Unreadable plane",unchecked((int)0x80004005));return name;}set{name=value;}}}
public sealed class Planes{public Plane[] Values={new Plane{Name="Top (xy)"},new Plane{Name="Right (yz)"}};public int FailItem=-1;public int Count{get{return Values.Length;}}public Plane Item(int index){if(index==FailItem)throw new COMException("Unreadable item",unchecked((int)0x80004005));return Values[index-1];}}
public sealed class Layouts
{
    public int Count;public bool ReturnExisting;public bool Omitted;public int FailAt=-1;public object LastPlane;
    public object Add(object plane,object existing,out object status){LastPlane=plane;Omitted=Object.ReferenceEquals(existing,Missing.Value);status=0;if(Count==FailAt)throw new IOException("Test failure");if(!ReturnExisting)Count++;else status=2;return new object();}
}
public sealed class AssemblyDoc{private Planes planes=new Planes();public bool FailPlanes;public Planes AsmRefPlanes{get{if(FailPlanes)throw new COMException("Collection unavailable",unchecked((int)0x80004005));return planes;}}public Layouts Layouts=new Layouts();}
public sealed class App{public bool DelayCompute;}
internal static class Tests
{
    static int n;static void Check(bool ok,string name){if(!ok)throw new Exception(name);n++;Console.WriteLine("PASS "+name);}
    static void Reject(Action a,string name){try{a();}catch{Check(true,name);return;}throw new Exception(name);}
    [STAThread]static int Main(){try{
        Application.EnableVisualStyles();var app=new App();var doc=new AssemblyDoc();var choices=SketchEngine.Planes(doc).Choices;
        Check(choices.Count==2 && choices[1].Name=="Right (yz)","reference planes preserve native order and names");
        int progress=0;Check(SketchEngine.Add(app,doc,choices[1],3,i=>progress=i)==3 && doc.Layouts.Count==3 && progress==3,"requested layouts are created and progress reported");
        Check(doc.Layouts.Omitted && !app.DelayCompute,"legacy optional argument and compute state preserved");
        app.DelayCompute=true;SketchEngine.Add(app,doc,choices[1],1,i=>{});Check(app.DelayCompute,"pre-existing delayed-compute state retained");
        Reject(()=>SketchEngine.Add(app,doc,null,1,i=>{}),"missing plane rejected");
        Reject(()=>SketchEngine.Add(app,doc,choices[1],0,i=>{}),"zero count rejected");
        Reject(()=>SketchEngine.Add(app,doc,choices[1],101,i=>{}),"oversized count rejected");
        doc.AsmRefPlanes.Values[1].Name="Renamed";Reject(()=>SketchEngine.Add(app,doc,choices[1],1,i=>{}),"renamed plane requires reload");
        doc=new AssemblyDoc();app.DelayCompute=false;doc.Layouts.FailAt=1;Reject(()=>SketchEngine.Add(app,doc,SketchEngine.Planes(doc).Choices[0],3,i=>{}),"partial batch reports failure");Check(doc.Layouts.Count==1 && !app.DelayCompute,"partial failure preserves completed work and restores compute");
        doc.Layouts.FailAt=-1;doc.Layouts.ReturnExisting=true;Reject(()=>SketchEngine.Add(app,doc,SketchEngine.Planes(doc).Choices[0],1,i=>{}),"reused layout is never reported as a newly added sketch");

        doc=new AssemblyDoc();doc.AsmRefPlanes.Values=new[]{new Plane{Name="First"},new Plane{FailName=true},new Plane{Name=" "},null,new Plane{Name="Fifth"},new Plane{Name="Sixth"},new Plane{Name="Seventh"}};doc.AsmRefPlanes.FailItem=6;
        var scan=SketchEngine.Planes(doc);
        Check(scan.Choices.Select(p=>p.Index).SequenceEqual(new[]{1,5,7}) && scan.Notices.Count==4,"unreadable items/names, blanks and nulls do not discard valid planes on either side");
        Check(scan.Notices.Any(s=>s.Contains("0x80004005")) && scan.Choices.All(p=>!String.IsNullOrWhiteSpace(p.Name)),"skipped entries include a diagnostic and no empty dropdown rows");
        Check(SketchEngine.Add(app,doc,scan.Choices[2],1,i=>{})==1 && Object.ReferenceEquals(doc.Layouts.LastPlane,doc.AsmRefPlanes.Values[6]),"filtered dropdown preserves original collection index when creating a sketch");
        doc.AsmRefPlanes.Values[6].FailName=true;
        Reject(()=>SketchEngine.Add(app,doc,scan.Choices[2],1,i=>{}),"a selected plane that becomes unreadable requires reload");
        Check(doc.Layouts.Count==1 && !app.DelayCompute,"unreadable selection creates nothing and does not change compute state");
        doc.FailPlanes=true;Reject(()=>SketchEngine.Planes(doc),"collection-level failures remain visible");

        string work=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..",".work"));Directory.CreateDirectory(work);
        // Match the failing assembly: four named planes, six blank names,
        // twenty-one E_FAIL entries. Test both recovery and stale-list clearing.
        doc=new AssemblyDoc();doc.AsmRefPlanes.Values=Enumerable.Range(0,31).Select(i=>new Plane{Name=i<4?new[]{"Top (xy)","Right (yz)","Front (xz)","STOP HEIGHT"}[i]:"",FailName=i>=10}).ToArray();
        using(var form=new SketchForm(app,doc))
        {
            form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);form.Show();Application.DoEvents();
            var list=form.Controls[0].Controls.OfType<ComboBox>().Single();var buttons=form.Controls[0].Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<Button>().ToArray();var add=buttons.Single(b=>b.Text=="Add sketches");var reload=buttons.Single(b=>b.Text=="Reload planes");
            Check(form.Text=="Add Sketch" && form.Controls[0].Controls.OfType<Label>().All(l=>!l.Text.Contains("HAMMILL")),"compact window uses only the Add Sketch name");
            Check(buttons.All(b=>b.Parent.ClientRectangle.Contains(b.Bounds)) && form.Controls[0].Controls.Cast<Control>().All(c=>c.Parent.ClientRectangle.Contains(c.Bounds)),"compact window keeps fields and action buttons inside their containers");
            Check(list.Items.Count==4 && ((PlaneChoice)list.SelectedItem).Name=="Right (yz)" && add.Enabled,"affected assembly pattern loads four named planes and enables Add");
            Check(form.Controls[0].Controls.OfType<Label>().Any(l=>l.Text.Contains("Skipped 27")),"window explains skipped entries instead of showing an empty list");
            using(var bmp=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bmp,new Rectangle(Point.Empty,form.Size));bmp.Save(Path.Combine(work,"preview.png"));}
            doc.FailPlanes=true;reload.PerformClick();Check(list.Items.Count==0 && !add.Enabled,"failed reload clears stale choices and disables Add");
            doc.FailPlanes=false;reload.PerformClick();Check(list.Items.Count==4 && add.Enabled,"Reload planes recovers without restarting the macro");
            doc.AsmRefPlanes.Values=new[]{new Plane{FailName=true},new Plane{Name=""}};reload.PerformClick();Check(list.Items.Count==0 && !add.Enabled,"all-unavailable scan leaves Add disabled");form.Hide();
        }
        Console.WriteLine(n+" checks passed.");return 0;
    }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
