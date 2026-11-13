using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using HoleTableFormatter;
using TurboCallout;

// Uses the active part draft as a read-only source; every mutation is confined
// to its disposable copy. Activate the six-hole sample used for SampleTest.
internal static class BoltCircleViewTests
{
    static readonly List<object> held=new List<object>();
    static object Keep(object value){held.Add(value);return value;}
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
    static bool Same(object a,object b){if(a==null||b==null)return false;IntPtr pa=Marshal.GetIUnknownForObject(a),pb=Marshal.GetIUnknownForObject(b);try{return pa==pb;}finally{Marshal.Release(pa);Marshal.Release(pb);}}
    static void VerifyMarks(object sheet,object circle,object view,string message)
    {
        object marks=Keep(((dynamic)sheet).CenterMarks);int found=0;
        for(int i=1;i<=(int)((dynamic)marks).Count;i++)
        {
            object mark=Keep(((dynamic)marks).Item(i)),arc=null,reference=null;double ax,ay,az,x,y,z;bool ak,key;
            ((IBcCenterMark)mark).GetArcCenter(out arc,out ax,out ay,out az,out ak);Keep(arc);
            if(!Same(arc,circle))continue;
            ((IBcCenterMark)mark).GetCenter(out reference,out x,out y,out z,out key);Keep(reference);
            object graphic=Keep(((dynamic)reference).Object),style=Keep(((dynamic)mark).Style);
            double vx,vy,sx,sy,cx,cy,r;((dynamic)graphic).GetCenterPoint(out vx,out vy);((dynamic)view).ViewToSheet(vx,vy,out sx,out sy);((dynamic)circle).Circle2d(out cx,out cy,out r);
            if(!(bool)((dynamic)mark).TwoPoint||!ak||!key||!(bool)((dynamic)circle).IsMember[reference,0]||
                Math.Abs(x-sx)>1e-8||Math.Abs(y-sy)>1e-8||Math.Abs(ax-cx)>1e-8||Math.Abs(ay-cy)>1e-8||
                !(bool)((dynamic)style).AutoCenterProjectionLine||(int)((dynamic)style).ProjectionLineDisplay!=3)
                throw new Exception("Projection mark lost its hole, radial reference, or display setting.");
            found++;
        }
        Check(found==(int)((dynamic)circle).Count,message);
    }
    [STAThread] static int Main(string[] args)
    {
        object source=null,copy=null;
        using(new BusyFilter())try
        {
            object app=Keep(Marshal.GetActiveObject("SolidEdge.Application")),docs=Keep(((dynamic)app).Documents);
            source=Keep(((dynamic)app).ActiveDocument);if((int)((dynamic)source).Type!=2)throw new Exception("Activate the sample draft.");
            string path=Path.GetFullPath(args[0]);Directory.CreateDirectory(Path.GetDirectoryName(path));
            ((dynamic)source).SaveCopyAs(path);int copyStart=held.Count;copy=Keep(((dynamic)docs).Open(path));
            object sheet=Keep(((dynamic)copy).ActiveSheet),notes=Keep(((dynamic)sheet).Balloons),note=Keep(((dynamic)notes).Item(1));
            object reference;double tx,ty,tz;bool keypoint;
            ((ICalloutTerminator)note).GetTerminator(out reference,out tx,out ty,out tz,out keypoint);Keep(reference);
            object graphic=Keep(((dynamic)reference).Object),view=Keep(((dynamic)graphic).DrawingView),circles=Keep(((dynamic)sheet).BoltHoleCircles);
            object marks=Keep(((dynamic)sheet).CenterMarks);
            string detail;
            if((int)((dynamic)circles).Count>0&&(int)((dynamic)marks).Count>0)
            {
                int count=(int)((dynamic)marks).Count;
                using(var engine=new BoltCircles(copy))
                {
                    Check(engine.Ensure(note,sheet,out detail)==BoltCircleResult.Existing&&engine.ProjectionMarksCreated==0&&engine.ProjectionMarksUpdated==0,"Manually corrected circle and projection marks are reused unchanged");
                }
                Check((int)((dynamic)marks).Count==count,"Manual projection marks are not duplicated");
            }
            // Only the disposable copy is cleared to exercise initial creation
            // even when the source already contains v0.1 circles or manual marks.
            for(int i=(int)((dynamic)marks).Count;i>=1;i--){object old=Keep(((dynamic)marks).Item(i));((dynamic)old).Delete();}
            for(int i=(int)((dynamic)circles).Count;i>=1;i--){object old=Keep(((dynamic)circles).Item(i));((dynamic)old).Delete();}
            int originalCount=(int)((dynamic)circles).Count;
            object duplicate=Keep(((dynamic)note).Duplicate(.02,0.0));
            ((dynamic)duplicate).SetTerminator(reference,tx,ty,tz,keypoint);((dynamic)duplicate).BalloonText="EXISTING CUSTOM HOLE NOTE";
            var position=new Placement(duplicate);
            ((dynamic)view).ScaleFactor=(double)((dynamic)view).ScaleFactor*.5;
            ((dynamic)view).Update();
            using(var engine=new BoltCircles(copy))
            {
                Check(engine.Ensure(note,sheet,out detail)==BoltCircleResult.Created,"Scaled face-on view creates its native bolt circle");
                Check(engine.Ensure(duplicate,sheet,out detail)==BoltCircleResult.Existing,"Two callouts on the same pattern share one circle");
            }
            Check((int)((dynamic)circles).Count==originalCount+1,"Only one bolt-circle annotation was added");
            Check((string)((dynamic)duplicate).BalloonText=="EXISTING CUSTOM HOLE NOTE","Existing custom callout text remains unchanged");
            // Scaling a view can move attached leader ends natively; capture
            // after that deliberate operation to isolate the next macro pass.
            position=new Placement(duplicate);
            using(var engine=new BoltCircles(copy))Check(engine.Ensure(duplicate,sheet,out detail)==BoltCircleResult.Existing,"Independent rerun reuses the circle");
            position.Verify(duplicate);Check(true,"Bolt-circle rerun preserves the callout's leader placement");
            object circle=Keep(((dynamic)circles).Item(originalCount+1));double x,y,r;
            VerifyMarks(sheet,circle,view,"Every hole receives a model-linked radial projection mark");
            for(int i=(int)((dynamic)marks).Count;i>=1;i--){object old=Keep(((dynamic)marks).Item(i));((dynamic)old).Delete();}
            using(var engine=new BoltCircles(copy))Check(engine.Ensure(note,sheet,out detail)==BoltCircleResult.Existing&&engine.ProjectionMarksCreated==(int)((dynamic)circle).Count,"A v0.1 circle receives all missing projections in place");
            Check((int)((dynamic)circles).Count==originalCount+1,"Repairing a v0.1 circle does not replace or duplicate it");
            object firstMark=Keep(((dynamic)marks).Item(1)),firstStyle=Keep(((dynamic)firstMark).Style);
            ((dynamic)firstStyle).AutoCenterProjectionLine=false;((dynamic)firstStyle).ProjectionLineDisplay=0;
            object removed=Keep(((dynamic)marks).Item(2));((dynamic)removed).Delete();
            using(var engine=new BoltCircles(copy))
                Check(engine.Ensure(note,sheet,out detail)==BoltCircleResult.Existing&&engine.ProjectionMarksCreated==1&&engine.ProjectionMarksUpdated==1,"Rerun repairs a missing mark and disabled projections without recreating the circle");
            VerifyMarks(sheet,circle,view,"Repaired projection marks retain native attachments");
            removed=Keep(((dynamic)marks).Item(2));object hole=null;double hx,hy,hz;bool hk;
            ((IBcCenterMark)removed).GetCenter(out hole,out hx,out hy,out hz,out hk);Keep(hole);((dynamic)removed).Delete();
            object conflict=Keep(((dynamic)marks).AddByCenter(hole,hx,hy,hz,hk));int conflictCount=(int)((dynamic)marks).Count;
            using(var engine=new BoltCircles(copy))
                Check(engine.Ensure(note,sheet,out detail)==BoltCircleResult.Review&&engine.ProjectionMarksCreated==0,"An unrelated existing center mark is left for review");
            Check((int)((dynamic)marks).Count==conflictCount,"Conflicting marks are neither overwritten nor duplicated");
            ((dynamic)conflict).Delete();
            using(var engine=new BoltCircles(copy))Check(engine.Ensure(note,sheet,out detail)==BoltCircleResult.Existing,"Missing radial mark can be repaired after conflict removal");
            ((dynamic)circle).Circle2d(out x,out y,out r);
            double ox,oy;((dynamic)view).GetOrigin(out ox,out oy);((dynamic)view).SetOrigin(ox+.02,oy-.01);
            double mx,my,mr;((dynamic)circle).Circle2d(out mx,out my,out mr);
            Check(Math.Abs(mx-x-.02)<1e-8&&Math.Abs(my-y+.01)<1e-8&&Math.Abs(mr-r)<1e-8,"Native bolt circle follows a drawing-view move");
            VerifyMarks(sheet,circle,view,"Projection marks follow the scaled and moved view");
            ((dynamic)copy).Save();
            // Drop child dispatch wrappers before closing their native document;
            // otherwise SE can recycle an identity while a legacy QI is cached.
            for(int i=held.Count-1;i>copyStart;i--)Com.Release(held[i]);
            held.RemoveRange(copyStart+1,held.Count-copyStart-1);
            ((dynamic)copy).Close(false);Com.Release(copy);held.RemoveAt(copyStart);copy=null;
            copy=Keep(((dynamic)docs).Open(path));sheet=Keep(((dynamic)copy).ActiveSheet);notes=Keep(((dynamic)sheet).Balloons);note=Keep(((dynamic)notes).Item(1));
            using(var engine=new BoltCircles(copy))Check(engine.Ensure(note,sheet,out detail)==BoltCircleResult.Existing,"Scaled and moved circle retains all model references after reopening");
            // Reacquire drawing interfaces after save/reopen and the model-link
            // activation. Solid Edge can retire an earlier dispatch wrapper.
            sheet=Keep(((dynamic)copy).ActiveSheet);notes=Keep(((dynamic)sheet).Balloons);note=Keep(((dynamic)notes).Item(1));
            // IDispatch marshaling inspects the previous object even for this
            // out argument; never pass a reference from the closed document.
            reference=null;
            ((ICalloutTerminator)note).GetTerminator(out reference,out tx,out ty,out tz,out keypoint);Keep(reference);
            graphic=Keep(((dynamic)reference).Object);view=Keep(((dynamic)graphic).DrawingView);circles=Keep(((dynamic)sheet).BoltHoleCircles);
            circle=Keep(((dynamic)circles).Item(1));VerifyMarks(sheet,circle,view,"Projection links and display survive save/reopen");
            using(var engine=new BoltCircles(copy))Check(engine.Ensure(note,sheet,out detail)==BoltCircleResult.Existing&&engine.ProjectionMarksCreated==0&&engine.ProjectionMarksUpdated==0,"Saved projection marks are reused without changes");
            int beforeIso=(int)((dynamic)circles).Count;((dynamic)view).SetViewOrientationStandard(7);((dynamic)view).Update();
            notes=Keep(((dynamic)sheet).Balloons);note=Keep(((dynamic)notes).Item(1));
            using(var engine=new BoltCircles(copy))Check(engine.Ensure(note,sheet,out detail)==BoltCircleResult.Skipped,"Isometric view receives no new bolt circle");
            Check((int)((dynamic)circles).Count==beforeIso,"Isometric skipping preserves existing annotations");
            Console.WriteLine(checks+" bolt-circle view checks passed.");return 0;
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
