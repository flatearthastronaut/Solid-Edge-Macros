using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HoleTableFormatter;
using TurboCallout;

// Live regression: 21931-01 base plate, callout 1 on a repeated (not seed)
// counterbore of the native 12-hole Fast Pattern. Writes only disposable drafts.
// Run with that drawing active, or pass a saved fixture path.
internal static class FastPatternTests
{
    static readonly List<object> held=new List<object>();
    static object K(object value){held.Add(value);return value;}
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    static object Note(object sheet){return K(((dynamic)K(((dynamic)sheet).Balloons)).Item(1));}
    static bool Same(object a,object b){return BoltCircleDimensions.Identity(a)==BoltCircleDimensions.Identity(b);}
    static void Verify(object doc,object sheet,bool dual)
    {
        object circles=K(((dynamic)sheet).BoltHoleCircles),marks=K(((dynamic)sheet).CenterMarks),dims=K(((dynamic)sheet).Dimensions);
        Check((int)((dynamic)circles).Count==1,"Only the verified 12-hole circle is annotated");
        object circle=K(((dynamic)circles).Item(1));Check((int)((dynamic)circle).Count==12,"All 12 physical holes are circle members");
        var projections=new List<object>();
        for(int i=1;i<=(int)((dynamic)marks).Count;i++)
        {
            object mark=K(((dynamic)marks).Item(i)),arc;double x,y,z;bool key;
            if(!(bool)((dynamic)mark).TwoPoint)continue;
            ((IBcCenterMark)mark).GetArcCenter(out arc,out x,out y,out z,out key);K(arc);
            if(arc==null||!Same(arc,circle))continue;
            Check(key&&(bool)((dynamic)mark).TwoPoint,"Radial mark attaches to native circle center");
            object reference;((IBcCenterMark)mark).GetCenter(out reference,out x,out y,out z,out key);K(reference);
            Check(key&&(bool)((dynamic)circle).IsMember[reference,0],"Projection references a verified hole");
            object style=K(((dynamic)mark).Style);
            Check((bool)((dynamic)style).AutoCenterProjectionLine&&(int)((dynamic)style).ProjectionLineDisplay==3,"Projection lines enabled");
            projections.Add(mark);
        }
        Check(projections.Count==12,"Exactly 12 linked projection marks");
        object diameter=null,angle=null;
        for(int i=1;i<=(int)((dynamic)dims).Count;i++)
        {
            object d=K(((dynamic)dims).Item(i));
            if((int)((dynamic)d).DimensionType==4&&BoltCircleDimensions.RelatedTo(d,circle))
            {Check(diameter==null,"No duplicate B.C diameter");diameter=d;}
            if(BoltCircleAngles.Tagged(d,BoltCircleAngles.DimensionTag))
            {Check(angle==null,"No duplicate angular dimension");angle=d;}
        }
        Check(diameter!=null&&angle!=null,"Native diameter and angle both created");
        Check((string)((dynamic)diameter).SuffixString=="B.C"&&String.IsNullOrEmpty((string)((dynamic)diameter).OverrideString),"Linked diameter with exact B.C suffix");
        object ds=K(((dynamic)diameter).Style);int units=(int)((dynamic)ds).PrimaryUnits;
        Check((bool)((dynamic)ds).DualDisplay==dual&&units==(dual?3:5),"Requested B.C units");
        Check((int)Property.Get(ds,dual?"SecondaryDecimalRoundOff":"PrimaryDecimalRoundOff")==4,"Two-place inch counterbore B.C precision");
        if(dual)Check((int)((dynamic)ds).PrimaryDecimalRoundOff==3,"One-place metric B.C precision");
        object note=Note(sheet),referenceToHole;double nx,ny,nz;bool nk;
        ((ICalloutTerminator)note).GetTerminator(out referenceToHole,out nx,out ny,out nz,out nk);K(referenceToHole);
        object graphic=K(((dynamic)referenceToHole).Object),view=K(((dynamic)graphic).DrawingView);
        double cx,cy,r;((dynamic)circle).Circle2d(out cx,out cy,out r);
        Check(Math.Abs((double)((dynamic)diameter).Value-2*r/(double)((dynamic)view).ScaleFactor)<1e-8,"Diameter measures model geometry at view scale");
        Check(Math.Abs((double)((dynamic)angle).Value-Math.PI/12)<1e-7&&String.IsNullOrEmpty((string)((dynamic)angle).OverrideString),"Native 15-degree angle with no override");
        int count;((IBcDimension)angle).GetRelatedCount(out count);Check(count==2,"Angle retains both native parents");
        object axis=null,radial=null;
        for(int i=0;i<count;i++)
        {object p;double x,y,z;bool key;((IBcDimension)angle).GetRelated(i,out p,out x,out y,out z,out key);K(p);if(BoltCircleAngles.Tagged(p,BoltCircleAngles.AxisTag))axis=p;else radial=p;}
        Check(axis!=null&&radial!=null&&projections.Any(p=>Same(p,radial)),"Angle uses the hole projection and associated axis");
        double sx,sy,ex,ey;((dynamic)axis).GetStartPoint(out sx,out sy);((dynamic)axis).GetEndPoint(out ex,out ey);
        Check(Math.Abs(sx-cx)+Math.Abs(sy-cy)<1e-8&&Math.Abs(ex-sx)<1e-8&&ey>sy,"Nearest axis is upward vertical through the B.C center");
        Check((int)((dynamic)K(((dynamic)axis).Relationships)).Count>=2,"Axis retains native center and vertical constraints");
        int t,h;((IBcDimension)angle).GetKeyPoint((int)((dynamic)angle).KeyPointCount-1,out nx,out ny,out nz,out t,out h);
        Check(nx<cx&&ny>cy,"15-degree text is in the hole's upper-left sector");

        // Exercise the seed as well as every combined result edge: attaching
        // another callout anywhere on this native pattern must reuse the circle.
        object duplicate=K(((dynamic)note).Duplicate(.01,0.0));
        try
        {
            using(var engine=new BoltCircles(doc))foreach(object mark in projections)
            {
                object reference;double x,y,z;bool key;string detail;
                ((IBcCenterMark)mark).GetCenter(out reference,out x,out y,out z,out key);K(reference);
                ((dynamic)duplicate).SetTerminator(reference,x,y,z,key);
                Check(engine.Ensure(duplicate,sheet,out detail)==BoltCircleResult.Existing,"Seed and repeated holes reuse the same verified circle: "+detail);
            }
        }
        finally{((dynamic)duplicate).Delete();}
    }
    [STAThread]static int Main(string[] args)
    {
        object source=null,copy=null;
        using(new BusyFilter())try
        {
            object app=K(Marshal.GetActiveObject("SolidEdge.Application")),docs=K(((dynamic)app).Documents);
            source=K(((dynamic)app).ActiveDocument);
            string root=Path.GetFullPath("Inspection/FastPattern-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);Console.WriteLine(root);
            string fixture=Path.Combine(root,"Source.dft");
            if(args.Length==0)((dynamic)source).SaveCopyAs(fixture);else File.Copy(Path.GetFullPath(args[0]),fixture);
            var rules=RuleBook.Load(Path.GetFullPath("../Turbo Hole Table Callouts/Custom.xml"));
            foreach(bool dual in new[]{false,true})
            {
                string path=Path.Combine(root,dual?"Dual.dft":"Inch.dft");File.Copy(fixture,path);
                copy=K(((dynamic)docs).Open(path));object sheet=K(((dynamic)copy).ActiveSheet),note=Note(sheet);
                string text=(string)((dynamic)note).BalloonText;var placement=new Placement(note);
                var off=Automation.FillDocument(copy,rules,dual,null,false);
                Check(off.Errors==0&&(int)((dynamic)K(((dynamic)sheet).BoltHoleCircles)).Count==0,"Without B.C creates no circle");
                var first=Automation.FillDocument(copy,rules,dual,null,true);Console.WriteLine(first);
                Check(first.Errors==0&&first.BoltCirclesCreated==1&&first.ProjectionMarksCreated==12&&first.BcDimensionsCreated==1&&first.BcAnglesCreated==1&&first.BcAnglesReview==0,"Complete Fast Pattern annotation succeeds");
                Check(first.BoltCirclesReview==1,"Unrelated noncircular hole group stays for review");
                Check(text==(string)((dynamic)note).BalloonText,"Existing correct machining note unchanged");placement.Verify(note);
                Verify(copy,sheet,dual);
                var rerun=Automation.FillDocument(copy,rules,dual,null,true);
                Check(rerun.Errors==0&&rerun.BoltCirclesCreated==0&&rerun.ProjectionMarksCreated==0&&rerun.BcDimensionsCreated==0&&rerun.BcAnglesCreated==0&&rerun.BcAnglesExisting==1,"Rerun creates no duplicates");
                Automation.FillDocument(copy,rules,dual,null,false);Verify(copy,sheet,dual);
                ((dynamic)copy).Save();((dynamic)copy).Close(false);copy=K(((dynamic)docs).Open(path));sheet=K(((dynamic)copy).ActiveSheet);
                Verify(copy,sheet,dual);
                var reopened=Automation.FillDocument(copy,rules,dual,null,true);
                Check(reopened.Errors==0&&reopened.BoltCirclesExisting==1&&reopened.BcAnglesExisting==1,"Save/reopen retains native membership");
                object window=K(((dynamic)app).ActiveWindow);((dynamic)window).SaveAsImage(Path.ChangeExtension(path,".jpg"),1800,1400,1.0,24,1,false);
                // Moving/scaling a view tests native links, not just matching
                // numbers at the initial drawing position.
                object reference;double x,y,z;bool key;note=Note(sheet);
                ((ICalloutTerminator)note).GetTerminator(out reference,out x,out y,out z,out key);K(reference);
                object graphic=K(((dynamic)reference).Object),view=K(((dynamic)graphic).DrawingView);
                double ox,oy;((dynamic)view).GetOrigin(out ox,out oy);((dynamic)view).SetOrigin(ox+.01,oy-.01);
                ((dynamic)view).ScaleFactor=(double)((dynamic)view).ScaleFactor*.8;((dynamic)view).Update();
                Verify(copy,sheet,dual);
                ((dynamic)copy).Close(false);copy=null;
            }
            Console.WriteLine(checks+" Fast Pattern checks passed.");return 0;
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
