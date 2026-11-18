using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using HoleTableFormatter;
using TurboCallout;

// Live 21931-01 fixture: one blank callout on a .249 seat at .42 depth.
// All edits and saves are confined to disposable copies, in all four menu modes.
internal static class DowelDepthTests
{
    static readonly List<object> held=new List<object>();
    static object K(object o){held.Add(o);return o;}
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    static string Attachment(object note)
    {
        object reference;double x,y,z;bool key;
        ((ICalloutTerminator)note).GetTerminator(out reference,out x,out y,out z,out key);K(reference);
        object graphic=K(((dynamic)reference).Object);string file="";Array bytes=new byte[0];int size=0;
        ((dynamic)graphic).GetReferenceKey(ref file,ref bytes,ref size);
        return file+"/"+Convert.ToBase64String((byte[])bytes);
    }
    static string Rendered(object note)
    {
        object data=K(((dynamic)note).GetDisplayData());var text=new List<string>();
        for(int i=0;i<(int)((dynamic)data).GetTextCount();i++)
        {
            string s="";double a=0,b=0,c=0,d=0,e=0,f=0,g=0,h=0,j=0;
            ((dynamic)data).GetTextAtIndex(i,ref s,ref a,ref b,ref c,ref d,ref e,ref f,ref g,ref h,ref j);
            if(!String.IsNullOrEmpty(s)&&!s.Contains("%"))text.Add(s);
        }
        return String.Join("|",text);
    }
    static void Verify(object note,bool dual,string attachment)
    {
        string raw=(string)((dynamic)note).BalloonText,shown=Rendered(note);Console.WriteLine(shown);
        Check(raw.Contains("%HS")&&raw.Contains("%ZH")&&raw.Contains(dual?"%{%BD/DU/@1}":"%{%BD/@2}"),"Actual pilot and seat depths remain native fields");
        Check(raw.Contains("P.F. FOR 1/4 DOWEL")&&!raw.Contains("TYP(")&&!raw.Contains("PLC'S"),"Quarter-inch PF purpose retained and quantity one omitted");
        Check(shown.Contains(".2500")&&shown.Contains(".2495")&&shown.Contains(".42"),"Rendered fit limits and actual .42 depth are correct");
        if(dual)Check(shown.Contains("6.350")&&shown.Contains("6.337")&&shown.Contains("10.7"),"Metric limits and one-place metric seat depth are correct");
        Check(dual?raw.Contains("%{/ST"):raw.Contains("%BS/LT"),"Dual fixed limits and inch parametric limits follow approved fallback");
        object style=K(((dynamic)note).Style);
        Check((bool)((dynamic)style).DualDisplay==dual&&Math.Abs((double)((dynamic)style).Height-.003175)<1e-8,"Selected units and .125-inch text retained");
        if(dual)Check(Math.Abs((double)((dynamic)style).ToleranceHeightRatio-1)<1e-8,"Typed stacked limits use full note height");
        Check(Attachment(note)==attachment,"Original native model attachment retained");
    }
    [STAThread]static int Main(string[] args)
    {
        object source=null,copy=null;
        using(new BusyFilter())try
        {
            object app=K(Marshal.GetActiveObject("SolidEdge.Application")),docs=K(((dynamic)app).Documents);
            source=K(((dynamic)app).ActiveDocument);
            string root=Path.GetFullPath("Inspection/DowelDepth-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);Console.WriteLine(root);
            string fixture=Path.Combine(root,"Source.dft");
            if(args.Length==0)((dynamic)source).SaveCopyAs(fixture);else File.Copy(Path.GetFullPath(args[0]),fixture);
            var rules=RuleBook.Load(Path.GetFullPath("../Turbo Hole Table Callouts/Custom.xml"));
            foreach(bool dual in new[]{false,true})foreach(bool bc in new[]{false,true})
            {
                string path=Path.Combine(root,(dual?"Dual":"Inch")+(bc?"-BC":"-Notes")+".dft");File.Copy(fixture,path);
                copy=K(((dynamic)docs).Open(path));object sheet=K(((dynamic)copy).ActiveSheet),notes=K(((dynamic)sheet).Balloons),note=null;
                int index=0;
                using(var reader=new NativeHole(copy))for(int i=1;i<=(int)((dynamic)notes).Count;i++)
                {
                    object candidate=K(((dynamic)notes).Item(i));if(!Automation.IsBlank(candidate))continue;
                    int? quantity;var f=reader.Read(candidate,out quantity);
                    if(f.Type!=7||!RuleBook.Near(f.Bore,.249)||!RuleBook.Near(f.BoreDepth,.42))continue;
                    Check(index==0&&quantity==1,"Fixture has one unambiguous single dowel callout");index=i;note=candidate;
                }
                Check(note!=null,"Sample dowel callout located");int count=(int)((dynamic)notes).Count;
                var before=new Placement(note);string attachment=Attachment(note);
                var report=Automation.FillDocument(copy,rules,dual,null,bc);Console.WriteLine(report);
                Check(report.Filled==1&&report.Errors==0,"Macro fills previously unmatched dowel without errors");
                Check((int)((dynamic)notes).Count==count,"One original leader becomes one native callout");
                before.Verify(note,NoteAlignment.Target(note));Verify(note,dual,attachment);
                string text=(string)((dynamic)note).BalloonText;var placed=new Placement(note);
                var rerun=Automation.FillDocument(copy,rules,dual,null,bc);
                Check(rerun.Filled==0&&rerun.Errors==0&&(string)((dynamic)note).BalloonText==text,"Rerun preserves filled text");placed.Verify(note);
                ((dynamic)copy).Save();((dynamic)copy).Close(false);copy=K(((dynamic)docs).Open(path));
                sheet=K(((dynamic)copy).ActiveSheet);notes=K(((dynamic)sheet).Balloons);note=K(((dynamic)notes).Item(index));
                Check((string)((dynamic)note).BalloonText==text,"Saved note text persists");placed.Verify(note);Verify(note,dual,attachment);
                object window=K(((dynamic)app).ActiveWindow);((dynamic)window).SaveAsImage(Path.ChangeExtension(path,".jpg"),2200,1600,1.0,24,1,false);
                ((dynamic)copy).Close(false);copy=null;
            }
            Console.WriteLine(checks+" live dowel-depth checks passed.");return 0;
        }
        catch(Exception e){Console.WriteLine(e);return 1;}
        finally
        {
            if(copy!=null)try{((dynamic)copy).Close(false);}catch{}
            if(source!=null)try{((dynamic)source).Activate();}catch{}
            for(int i=held.Count-1;i>=0;i--)Com.Release(held[i]);
        }
    }
}
