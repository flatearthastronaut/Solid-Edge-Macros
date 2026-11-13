using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HoleTableFormatter;
using TurboCallout;

// Run against the open sample. All mutations are made in SaveCopyAs drafts;
// the user's source document and manually positioned dimension are audited.
internal static class BoltCircleDimensionTests
{
    static readonly List<object> held=new List<object>();
    static object K(object o){held.Add(o);return o;}
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static string Position(object d)
    {
        var result=new List<string>();
        foreach(string name in new[]{"DimensionType","TrackDistance","TrackAngle","BreakPosition","BreakDistance","TerminatorPosition","LeaderDistance","TextScale"})
        {
            object value=Property.Get(d,name);
            // SE serializes some leader settings as single precision. Compare
            // at 0.1 micrometre on paper, not differing binary round-off tails.
            result.Add(value is double?((double)value).ToString("F7",CultureInfo.InvariantCulture):Convert.ToString(value,CultureInfo.InvariantCulture));
        }
        double x,y;((dynamic)d).GetTextOffsets(out x,out y);
        result.Add(x.ToString("F9",CultureInfo.InvariantCulture));result.Add(y.ToString("F9",CultureInfo.InvariantCulture));
        return String.Join("|",result);
    }
    static Dictionary<int,string> Capture(object sheet,bool full)
    {
        var result=new Dictionary<int,string>();object dims=K(((dynamic)sheet).Dimensions);
        for(int i=1;i<=(int)((dynamic)dims).Count;i++)
        {
            object d=K(((dynamic)dims).Item(i));string text=Position(d);
            if(full)
            {
                object style=K(((dynamic)d).Style);
                text+="|"+((double)((dynamic)d).Value).ToString("F9",CultureInfo.InvariantCulture)+"|"+((dynamic)d).SuffixString;
                foreach(string name in new[]{"PrimaryUnits","SecondaryUnits","DualDisplay","PrimaryDecimalRoundOff","SecondaryDecimalRoundOff"})
                    text+="|"+Property.Get(style,name);
            }
            result.Add(i,text);
        }
        return result;
    }
    static void Same(Dictionary<int,string> a,Dictionary<int,string> b,string label)
    {
        foreach(var p in a)if(!b.ContainsKey(p.Key)||p.Value!=b[p.Key])
            Console.WriteLine("DIFFERENCE "+p.Key+"\nBefore="+p.Value+"\nAfter="+(b.ContainsKey(p.Key)?b[p.Key]:"missing"));
        Check(a.Count==b.Count&&a.All(p=>b.ContainsKey(p.Key)&&p.Value==b[p.Key]),label);
    }
    static List<BoltCircleTarget> Targets(BoltCircles engine,object sheet)
    {
        object notes=K(((dynamic)sheet).Balloons);
        for(int i=1;i<=(int)((dynamic)notes).Count;i++)
        {object n=K(((dynamic)notes).Item(i));string detail;if(Automation.IsCallout(n))engine.Ensure(n,sheet,out detail);}
        return engine.DimensionTargets;
    }
    static void Verify(object doc,object sheet,RuleBook rules,bool dual,int originalCount,bool checkPlacement)
    {
        object dims=K(((dynamic)sheet).Dimensions),notes=K(((dynamic)sheet).Balloons);
        using(var engine=new BoltCircles(doc))
        using(var reader=new NativeHole(doc))
        foreach(var group in Targets(engine,sheet).GroupBy(t=>BoltCircleDimensions.Identity(t.Circle)))
        {
            var target=group.First();object n=K(((dynamic)notes).Item(target.NoteIndex));int? count;
            int places=BoltCircleDimensions.InchPlaces(Notes.Recognize(rules,reader.Read(n,out count)));
            var found=new List<Tuple<int,object>>();
            for(int i=1;i<=(int)((dynamic)dims).Count;i++)
            {object d=K(((dynamic)dims).Item(i));if(BoltCircleDimensions.RelatedTo(d,target.Circle))found.Add(Tuple.Create(i,d));}
            Check(found.Count==1,"Exactly one native dimension per verified bolt circle");
            object dimension=found[0].Item2,style=K(((dynamic)dimension).Style);
            Check(((string)((dynamic)dimension).SuffixString)=="B.C","Exact native B.C suffix");
            Check(String.IsNullOrEmpty((string)((dynamic)dimension).OverrideString),"Diameter has no typed value override");
            int units=(int)((dynamic)style).PrimaryUnits;
            Check((int)Property.Get(style,units==5?"PrimaryDecimalRoundOff":"SecondaryDecimalRoundOff")==places+2,"Family-specific inch precision");
            double cx,cy,r;((dynamic)target.Circle).Circle2d(out cx,out cy,out r);
            Check(Math.Abs((double)((dynamic)dimension).Value-2*r/(double)((dynamic)target.View).ScaleFactor)<1e-7,"Model diameter matches native scaled bolt circle");
            if(found[0].Item1<=originalCount)continue;
            Check((bool)((dynamic)style).DualDisplay==dual&&units==(dual?3:5),"New dimension matches selected unit mode");
            if(dual)Check((int)((dynamic)style).PrimaryDecimalRoundOff==places+1,"New metric precision is one place fewer than inch");
            Check(Math.Abs((double)((dynamic)style).Height-.003175)<1e-9,"Reference .125-inch text height");
            if(checkPlacement)
            {
                double nx,ny,nz;int type,handle;((IBcNotePosition)n).GetKeyPoint(1,out nx,out ny,out nz,out type,out handle);
                double xmin,ymin,xmax,ymax;((dynamic)n).Range(out xmin,out ymin,out xmax,out ymax);
                double tx,ty,tz;((IBcDimension)dimension).GetKeyPoint(3,out tx,out ty,out tz,out type,out handle);
                Check((ny>=cy?ty>ymax:ty<ymin)&&Math.Abs(tx-nx)<.0381,"Text stays outside and near its own callout on both left and right");
            }
        }
    }
    [STAThread]static int Main(string[] args)
    {
        object source=null,copy=null;
        using(new BusyFilter())try
        {
            object app=K(Marshal.GetActiveObject("SolidEdge.Application")),docs=K(((dynamic)app).Documents);
            source=K(((dynamic)app).ActiveDocument);object sourceSheet=K(((dynamic)source).ActiveSheet);
            var sourceSnapshot=Capture(sourceSheet,true);Check(sourceSnapshot.Count>0,"Sample contains user's manual Smart Dimension");
            var rules=RuleBook.Load(Path.GetFullPath("../Turbo Hole Table Callouts/Custom.xml"));
            string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
            foreach(bool dual in new[]{false,true})
            {
                string path=Path.Combine(root,dual?"Dual.dft":"Inch.dft");
                ((dynamic)source).SaveCopyAs(path);copy=K(((dynamic)docs).Open(path));object sheet=K(((dynamic)copy).ActiveSheet);
                var before=Capture(sheet,false);object dims=K(((dynamic)sheet).Dimensions);
                var report=Automation.FillDocument(copy,rules,dual,Console.WriteLine);Console.WriteLine(report);
                Check(report.Errors==0&&report.BcDimensionsReview==0&&report.BcDimensionsCreated>0,"Recognized circles receive dimensions without dimension errors");
                var positions=Capture(sheet,false);
                Check(before.All(p=>positions[p.Key]==p.Value),"Manual Smart Dimension position and leader settings retained");
                Verify(copy,sheet,rules,dual,before.Count,true);
                var snapshot=Capture(sheet,true);((dynamic)copy).Save();((dynamic)copy).Close(false);copy=null;
                copy=K(((dynamic)docs).Open(path));sheet=K(((dynamic)copy).ActiveSheet);
                Same(snapshot,Capture(sheet,true),"Dimension settings persist after save/reopen");
                Verify(copy,sheet,rules,dual,before.Count,true);
                var repeat=Automation.FillDocument(copy,rules,dual,null);
                Check(repeat.Errors==0&&repeat.BcDimensionsCreated==0&&repeat.BcDimensionsUpdated==0&&repeat.BcDimensionsReview==0,"Rerun does not duplicate or reposition dimensions");
                Same(snapshot,Capture(sheet,true),"Rerun preserves all native dimensions");
                object window=K(((dynamic)app).ActiveWindow);((dynamic)window).SaveAsImage(Path.ChangeExtension(path,".jpg"),1800,1400,1.0,24,1,false);
                // Exercise native association at a different view scale and origin.
                using(var engine=new BoltCircles(copy))
                {
                    var views=Targets(engine,sheet).GroupBy(t=>BoltCircleDimensions.Identity(t.View)).Select(g=>g.First().View).ToArray();
                    foreach(object view in views)
                    {double x,y;((dynamic)view).GetOrigin(out x,out y);((dynamic)view).ScaleFactor=(double)((dynamic)view).ScaleFactor*.5;
                        ((dynamic)view).SetOrigin(x+.02,y-.01);((dynamic)view).Update();}
                }
                Verify(copy,sheet,rules,dual,before.Count,false);
                // Protect a manual override and roll back any suffix/precision edits.
                dims=K(((dynamic)sheet).Dimensions);object first=K(((dynamic)dims).Item(1));((dynamic)first).OverrideString="MANUAL";
                var custom=Capture(sheet,true);var review=Automation.FillDocument(copy,rules,dual,null);
                Check(review.BcDimensionsReview==1,"Typed diameter override is left for review");
                Same(custom,Capture(sheet,true),"Manual override and other dimensions preserved");
                ((dynamic)copy).Close(false);copy=null;Console.WriteLine("PASS "+path);
            }
            Same(sourceSnapshot,Capture(sourceSheet,true),"Original drawing and manual dimension unchanged");
            Console.WriteLine(checks+" native B.C dimension checks passed.");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
        finally
        {if(copy!=null)try{((dynamic)copy).Close(false);}catch{}if(source!=null)try{((dynamic)source).Activate();}catch{}
            for(int i=held.Count-1;i>=0;i--)Com.Release(held[i]);}
    }
}
