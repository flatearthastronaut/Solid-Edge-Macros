using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Hammill.OccurrenceViewer;
public sealed class Items{public object[] Values;public Items(params object[] values){Values=values;}public int Count{get{return Values.Length;}}public object Item(int i){return Values[i-1];}}
public class Occurrence
{
    public bool DisplayAsReference,IncludeInBom,DisplayInDrawings,IncludeInPhysicalProperties,DisplayInSubAssembly,IncludeInInterference,Locatable,Subassembly;
    public double Quantity=1;public Items SubOccurrences=new Items();public bool Fail;
    private bool visible=true;public int Writes;public bool Visible{get{return visible;}set{if(Fail)throw new IOException("write failed");Writes++;visible=value;}}
}
public sealed class SubOccurrence:Occurrence{public object ThisAsOccurrence;}
public sealed class AssemblyDoc{public Items Occurrences;}
public sealed class App{public bool DelayCompute;}
internal static class Tests
{
    static int n;static void Check(bool ok,string name){if(!ok)throw new Exception(name);n++;Console.WriteLine("PASS "+name);}
    [STAThread]static int Main(){try{
        Application.EnableVisualStyles();var property=new Occurrence{DisplayAsReference=true,IncludeInBom=true,DisplayInDrawings=true,IncludeInPhysicalProperties=true,DisplayInSubAssembly=true,IncludeInInterference=true,Locatable=true,Quantity=2};
        for(int f=1;f<=8;f++)Check(VisibilityEngine.Matches(property,f,true) && !VisibilityEngine.Matches(property,f,false),"yes/no filter "+f);
        property.Quantity=1;Check(VisibilityEngine.Matches(property,7,false),"No user-defined quantity matches quantity one");
        Check(VisibilityEngine.Matches(new object(),0,false),"All ignores property value");
        var leaf=new SubOccurrence{ThisAsOccurrence=property};var middle=new SubOccurrence{ThisAsOccurrence=new Occurrence(),Subassembly=true,SubOccurrences=new Items(leaf)};
        var parent=new Occurrence{Subassembly=true,SubOccurrences=new Items(middle)};var other=new Occurrence();var doc=new AssemblyDoc{Occurrences=new Items(parent,other)};var app=new App();
        var preview=VisibilityEngine.Run(app,doc,1,true,true,null);Check(preview.TopLevel==2 && preview.Nested==2 && preview.Matched==1 && leaf.Writes==0 && !app.DelayCompute,"preview traverses each nested instance once and never mutates");
        var result=VisibilityEngine.Run(app,doc,1,true,true,false);Check(result.Changed==1 && !leaf.Visible && property.Visible && parent.Visible,"nested visibility changes instance instead of source occurrence");
        Check(!app.DelayCompute,"compute mode restored after nested visibility change");
        result=VisibilityEngine.Run(app,doc,0,true,false,false);Check(result.Changed==2 && result.Nested==0 && middle.Visible,"top-level option does not traverse children");
        app.DelayCompute=true;other.Fail=true;result=VisibilityEngine.Run(app,doc,0,true,false,true);Check(result.Errors.Count==1 && result.Changed==1 && app.DelayCompute,"per-instance failure reported while original compute mode restored");
        var bad=new SubOccurrence{ThisAsOccurrence=new object()};parent.SubOccurrences=new Items(bad,leaf);result=VisibilityEngine.Run(app,doc,1,true,true,null);Check(result.Errors.Count==1 && result.Matched==1,"unreadable nested property does not hide valid matches");
        string work=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..",".work"));Directory.CreateDirectory(work);
        using(var form=new ViewerForm(app,doc)){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);form.Show();Application.DoEvents();using(var bmp=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bmp,new Rectangle(Point.Empty,form.Size));bmp.Save(Path.Combine(work,"preview.png"));}form.Hide();}
        Check(true,"visibility window renders");Console.WriteLine(n+" checks passed.");return 0;
    }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
