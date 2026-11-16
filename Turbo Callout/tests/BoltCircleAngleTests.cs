using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HoleTableFormatter;
using TurboCallout;

// Live regression fixture: Greg's saved manual 30-degree Angle Between sample.
// Every write is confined to a uniquely named copy under Inspection. Restore
// whichever document the user was working on when this test started.
internal static class BoltCircleAngleTests
{
    static readonly List<object> held=new List<object>();
    static object K(object item){held.Add(item);return item;}
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static List<object> Angles(object sheet)
    {
        var result=new List<object>();object dims=K(((dynamic)sheet).Dimensions);
        for(int i=1;i<=(int)((dynamic)dims).Count;i++)
        {object d=K(((dynamic)dims).Item(i));if((int)((dynamic)d).DimensionType==3)result.Add(d);}
        return result;
    }
    static string Snapshot(object sheet)
    {
        var result=new List<string>();
        foreach(object d in Angles(sheet))
        {
            result.Add(((double)((dynamic)d).Value).ToString("F9",CultureInfo.InvariantCulture));
            result.Add(((double)((dynamic)d).TrackDistance).ToString("F7",CultureInfo.InvariantCulture));
            for(int i=0;i<(int)((dynamic)d).KeyPointCount;i++)
            {
                double x,y,z;int t,h;((IBcDimension)d).GetKeyPoint(i,out x,out y,out z,out t,out h);
                result.Add(x.ToString("F7",CultureInfo.InvariantCulture)+","+y.ToString("F7",CultureInfo.InvariantCulture));
            }
        }
        object lines=K(((dynamic)sheet).Lines2d);result.Add("Lines="+((dynamic)lines).Count);
        return String.Join("|",result);
    }
    static void Verify(object sheet)
    {
        var angles=Angles(sheet);var managed=angles.Where(d=>BoltCircleAngles.Tagged(d,BoltCircleAngles.DimensionTag)).ToArray();
        Check(angles.Count==3&&managed.Length==2,"Two generated angles plus the untouched manual angle");
        var values=managed.Select(d=>(double)((dynamic)d).Value*180/Math.PI).OrderBy(d=>d).ToArray();
        Check(Math.Abs(values[0]-40)<1e-6&&Math.Abs(values[1]-42)<1e-6,"40-degree vertical and 42-degree horizontal angle measurements");
        foreach(object d in managed)
        {
            Check(String.IsNullOrEmpty((string)((dynamic)d).OverrideString),"Angle has no numeric override");
            object style=K(((dynamic)d).Style);
            Check(Math.Abs((double)((dynamic)style).Height-.003175)<1e-9,"Full .125-inch angular text");
            Check((int)((dynamic)style).AngularUnits==3&&!(bool)((dynamic)style).DualDisplay,"Native degree display in both note unit modes");
            int count;((IBcDimension)d).GetRelatedCount(out count);Check(count==2,"Angle has two native line parents");
            object axis=null,radial=null;
            for(int i=0;i<count;i++)
            {
                object related;double x,y,z;bool key;((IBcDimension)d).GetRelated(i,out related,out x,out y,out z,out key);K(related);
                if((int)((dynamic)related).Type==760091584)axis=related;
                if((int)((dynamic)related).Type==505332096)radial=related;
            }
            Check(axis!=null&&radial!=null,"Dimension references its constrained axis and native projection mark");
            Check(BoltCircleAngles.Tagged(axis,BoltCircleAngles.AxisTag)&&(bool)((dynamic)radial).TwoPoint,"Axis ownership and two-point projection survive");
            object arc;double cx,cy,cz;bool arcKey;((IBcCenterMark)radial).GetArcCenter(out arc,out cx,out cy,out cz,out arcKey);K(arc);
            double radius;((dynamic)arc).Circle2d(out cx,out cy,out radius);
            double sx,sy,ex,ey;((dynamic)axis).GetStartPoint(out sx,out sy);((dynamic)axis).GetEndPoint(out ex,out ey);
            Check(Math.Abs(sx-cx)+Math.Abs(sy-cy)<1e-8,"Axis starts at its linked bolt-circle center");
            Check(Math.Abs(ex-sx)<1e-8||Math.Abs(ey-sy)<1e-8,"Axis stays horizontal or vertical");
            object relations=K(((dynamic)axis).Relationships);Check((int)((dynamic)relations).Count>=2,"Native coincidence and direction constraints retained");
            object axisStyle=K(((dynamic)axis).Style);Check((string)((dynamic)axisStyle).DashName=="Chain","Axis uses centerline dash pattern");
        }
        var manual=angles.Single(d=>!BoltCircleAngles.Tagged(d,BoltCircleAngles.DimensionTag));
        Check(Math.Abs((double)((dynamic)manual).Value-Math.PI/6)<1e-8,"Manual 30-degree sample retained");
    }
    [STAThread]static int Main(string[] args)
    {
        object source=null,copy=null;
        using(new BusyFilter())try
        {
            object app=K(Marshal.GetActiveObject("SolidEdge.Application")),docs=K(((dynamic)app).Documents);
            source=K(((dynamic)app).ActiveDocument);
            string fixture=Path.GetFullPath(args[0]),root=Path.GetFullPath("Inspection/Angles-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);Console.WriteLine(root);
            var rules=RuleBook.Load(Path.GetFullPath("../Turbo Hole Table Callouts/Custom.xml"));
            foreach(bool dual in new[]{false,true})
            {
                string path=Path.Combine(root,dual?"Dual.dft":"Inch.dft");File.Copy(fixture,path);copy=K(((dynamic)docs).Open(path));
                object sheet=K(((dynamic)copy).ActiveSheet);string before=Snapshot(sheet);
                var off=Automation.FillDocument(copy,rules,dual,null,false);
                Check(Snapshot(sheet)==before,"Without B.C leaves all angular geometry unchanged");
                var report=Automation.FillDocument(copy,rules,dual,Console.WriteLine,true);Console.WriteLine(report);
                foreach(string detail in report.Details)Console.WriteLine(detail);
                Check(report.Errors==0&&report.BcAnglesReview==0&&report.BcAnglesCreated==2,"Generate two verified native angles without angular review failures");
                Check(report.BcAnglesSkipped>=3,"Axis-aligned target holes are skipped");Verify(sheet);
                string placed=Snapshot(sheet);
                var rerun=Automation.FillDocument(copy,rules,dual,null,true);
                Check(rerun.BcAnglesCreated==0&&rerun.BcAnglesReview==0&&rerun.BcAnglesExisting==2,"Rerun reuses both generated angles");
                Check(Snapshot(sheet)==placed,"Rerun preserves angular placement and adds no axes");
                ((dynamic)copy).Save();((dynamic)copy).Close(false);copy=K(((dynamic)docs).Open(path));sheet=K(((dynamic)copy).ActiveSheet);
                Verify(sheet);Check(Snapshot(sheet)==placed,"Angular values and placement survive save/reopen");
                object window=K(((dynamic)app).ActiveWindow);((dynamic)window).SaveAsImage(Path.ChangeExtension(path,".jpg"),1800,1400,1.0,24,1,false);
                // Move and scale only the test copy's face-on drawing view.
                object views=K(((dynamic)sheet).DrawingViews);
                for(int i=1;i<=(int)((dynamic)views).Count;i++)
                {
                    object view=K(((dynamic)views).Item(i));double x,y;((dynamic)view).GetOrigin(out x,out y);
                    ((dynamic)view).ScaleFactor=(double)((dynamic)view).ScaleFactor*.8;
                    ((dynamic)view).SetOrigin(x+.012,y-.008);((dynamic)view).Update();
                }
                Verify(sheet);
                var moved=Automation.FillDocument(copy,rules,dual,null,true);
                Check(moved.BcAnglesCreated==0&&moved.BcAnglesReview==0,"Moved and scaled native angles remain reusable");
                string movedState=Snapshot(sheet);Automation.FillDocument(copy,rules,!dual,null,false);
                Check(movedState==Snapshot(sheet),"Without B.C preserves existing angular annotations after movement");
                ((dynamic)copy).Close(false);copy=null;
            }
            Console.WriteLine(checks+" native angular checks passed.");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
        finally
        {
            if(copy!=null)try{((dynamic)copy).Close(false);}catch{}
            if(source!=null)try{((dynamic)source).Activate();}catch{}
            for(int i=held.Count-1;i>=0;i--)Com.Release(held[i]);
        }
    }
}
