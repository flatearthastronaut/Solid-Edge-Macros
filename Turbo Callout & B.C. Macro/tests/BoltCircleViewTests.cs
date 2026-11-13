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
            int originalCount=(int)((dynamic)circles).Count;
            object duplicate=Keep(((dynamic)note).Duplicate(.02,0.0));
            ((dynamic)duplicate).SetTerminator(reference,tx,ty,tz,keypoint);((dynamic)duplicate).BalloonText="EXISTING CUSTOM HOLE NOTE";
            var position=new Placement(duplicate);
            ((dynamic)view).ScaleFactor=(double)((dynamic)view).ScaleFactor*.5;
            ((dynamic)view).Update();
            string detail;
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
            ((dynamic)circle).Circle2d(out x,out y,out r);
            double ox,oy;((dynamic)view).GetOrigin(out ox,out oy);((dynamic)view).SetOrigin(ox+.02,oy-.01);
            double mx,my,mr;((dynamic)circle).Circle2d(out mx,out my,out mr);
            Check(Math.Abs(mx-x-.02)<1e-8&&Math.Abs(my-y+.01)<1e-8&&Math.Abs(mr-r)<1e-8,"Native bolt circle follows a drawing-view move");
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
