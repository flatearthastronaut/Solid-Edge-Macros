using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;
using Part = SolidEdge.Part.Interop;
using Geo = SolidEdge.Geometry.Interop;
using Support = SolidEdge.FrameworkSupport.Interop;
using Framework = SolidEdge.Framework.Interop;

// Drawing values remain in inches until the API boundary. Solid Edge uses meters.
// Following SDK FiniteExtrudedProtrusion / Holes / Automation examples, all CAD
// calls execute on one STA, with a bounded busy filter and explicit profile checks.
partial class BuildFitting
{
    const double Inch = .0254;
    static dynamic app;
    static Part.PartDocument doc;
    static Part.Model model;
    static readonly List<object> owned = new List<object>();
    static readonly Dictionary<Part.Profile,List<Segment>> segments=new Dictionary<Part.Profile,List<Segment>>();
    class Segment { public object Curve; public int Start,End; public Segment(object c,int a,int b){Curve=c;Start=a;End=b;} }
    static StreamWriter log;
    static string output;
    static double drawingDepth;
    static T Keep<T>(T value) { owned.Add(value); return value; }
    static void Say(string text) { Console.WriteLine(text); if(log!=null){log.WriteLine(text);log.Flush();} }
    static double[] P(double x,double y,double z) { return new[]{x,y,z}; }
    static double V() { var b=Keep((Geo.Body)model.Body); if(!b.IsSolid || !b.Valid || b.Volume<=0)throw new Exception("Invalid solid"); return b.Volume; }

    static Part.RefPlane Plane(int axis,double position)
    {
        var planes=Keep(doc.RefPlanes);
        for(int i=1;i<=3;i++)
        {
            var p=Keep(planes.Item(i));Array normal=new double[3],root=new double[3];
            p.GetNormal(ref normal);p.GetRootPoint(ref root);
            double n=Convert.ToDouble(normal.GetValue(axis));
            if(Math.Abs(n)<.99999)continue;
            if(Math.Abs(position)<1e-10)return p;
            var side=position*n>0 ? Part.ReferenceElementConstants.igNormalSide : Part.ReferenceElementConstants.igReverseNormalSide;
            var offset=Keep(planes.AddParallelByDistance(p,Math.Abs(position)*Inch,side,Local:false));
            offset.Visible=false;
            return offset;
        }
        throw new Exception("Base plane not found");
    }
    static Part.Profile Profile(int axis,double position)
    {
        var sets=Keep(doc.ProfileSets);var set=Keep(sets.Add());var profiles=Keep(set.Profiles);
        // On these third-angle top views the ordinate datum is at the back:
        // drawing distances increase down the page, opposite model +Y.
        return Keep(profiles.Add(Plane(axis,axis==1 && drawingDepth>0 ? drawingDepth-position : position)));
    }
    static double[] UV(Part.Profile p,double[] xyz)
    {
        if(drawingDepth>0)xyz=new[]{xyz[0],drawingDepth-xyz[1],xyz[2]};
        double u,v,x,y,z;p.Convert3DCoordinate(xyz[0]*Inch,xyz[1]*Inch,xyz[2]*Inch,out u,out v);
        p.Convert2DCoordinate(u,v,out x,out y,out z);
        if(Math.Abs(x-xyz[0]*Inch)+Math.Abs(y-xyz[1]*Inch)+Math.Abs(z-xyz[2]*Inch)>1e-8)throw new Exception("Point is off its sketch plane");
        return new[]{u,v};
    }
    static void Line(Part.Profile p,double[] a,double[] b)
    {
        var u=UV(p,a);var v=UV(p,b);var line=Keep(p.Lines2d.AddBy2Points(u[0],u[1],v[0],v[1]));
        AxisConstraints.Ensure(Keep(p.Relations2d),line);
        if(!segments.ContainsKey(p))segments[p]=new List<Segment>();
        segments[p].Add(new Segment(line,0,1));
    }
    static void Arc(Part.Profile p,double[] a,double[] mid,double[] b)
    {
        var u=UV(p,a);var v=UV(p,mid);var w=UV(p,b);
        var arc=Keep(p.Arcs2d.AddByStartAlongEnd(u[0],u[1],v[0],v[1],w[0],w[1]));
        double sx,sy;arc.GetStartPoint(out sx,out sy);bool forward=Math.Abs(sx-u[0])+Math.Abs(sy-u[1])<1e-8;
        if(!segments.ContainsKey(p))segments[p]=new List<Segment>();
        segments[p].Add(new Segment(arc,forward ? 1 : 2,forward ? 2 : 1));
    }
    static Part.Profile Polygon(int axis,double pos,params double[][] points)
    {
        var p=Profile(axis,pos);
        for(int i=0;i<points.Length;i++)Line(p,points[i],points[(i+1)%points.Length]);
        return p;
    }
    static void End(Part.Profile p)
    {
        // Coincident coordinates alone do not make a closed Ordered profile.
        // Join explicit curve keypoints as prescribed by the SDK examples.
        if(segments.ContainsKey(p))
        {
            var list=segments[p];var relations=Keep(p.Relations2d);
            for(int i=0;i<list.Count;i++) {var a=list[i];var b=list[(i+1)%list.Count];Keep(relations.AddKeypoint(a.Curve,a.End,b.Curve,b.Start,true));}
        }
        int status=p.End(Part.ProfileValidationType.igProfileClosed);
        if(status!=0)throw new Exception("Profile validation failed: "+status);
        p.Visible=false;
    }
    static Part.FeaturePropertyConstants Direction(Part.Profile p,int axis,int sign)
    {
        if(axis==1 && drawingDepth>0)sign=-sign;
        double x0,y0,z0,x1,y1,z1,x2,y2,z2;
        p.Convert2DCoordinate(0,0,out x0,out y0,out z0);
        p.Convert2DCoordinate(1,0,out x1,out y1,out z1);
        p.Convert2DCoordinate(0,1,out x2,out y2,out z2);
        var a=P(x1-x0,y1-y0,z1-z0);var b=P(x2-x0,y2-y0,z2-z0);
        var cross=P(a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]);
        return cross[axis]*sign>0 ? Part.FeaturePropertyConstants.igRight : Part.FeaturePropertyConstants.igLeft;
    }
    static void Extrude(Part.Profile p,int axis,int sign,double depth,string name,bool cut=false)
    {
        End(p);double before=model==null ? 0 : V();
        if(model==null)
        {
            Array profiles=new object[]{p};var models=Keep(doc.Models);
            model=Keep(models.AddFiniteExtrudedProtrusion(1,ref profiles,Direction(p,axis,sign),depth*Inch));
            Keep(model.ExtrudedProtrusions.Item(1)).Name=name;
        }
        else if(cut)
        {
            // Mirroring a sketch across its second coordinate reverses its
            // winding; the interior side must reverse along with the contour.
            var interior=drawingDepth>0 && axis!=1 ? Part.FeaturePropertyConstants.igRight : Part.FeaturePropertyConstants.igLeft;
            var f=Keep(model.ExtrudedCutouts.AddFinite(p,interior,Direction(p,axis,sign),depth*Inch));
            object description; if(f.get_Status(out description)!=Part.FeatureStatusConstants.igFeatureOK)throw new Exception(name+": "+description);
            f.Name=name;
        }
        else
        {
            var f=Keep(model.ExtrudedProtrusions.AddFinite(p,Part.FeaturePropertyConstants.igRight,Direction(p,axis,sign),depth*Inch));
            object description;if(f.get_Status(out description)!=Part.FeatureStatusConstants.igFeatureOK)throw new Exception(name+": "+description);
            f.Name=name;
        }
        double after=V();
        if(cut ? after>=before-1e-14 : after<=before+1e-14)throw new Exception(name+": wrong volume change");
        Say(name+" volume_in3="+(after/(Inch*Inch*Inch)).ToString("F7"));
    }
    static void Box(double x0,double x1,double y0,double y1,double z0,double z1,string name,bool cut=false)
    {
        Extrude(Polygon(2,z0,P(x0,y0,z0),P(x1,y0,z0),P(x1,y1,z0),P(x0,y1,z0)),2,1,z1-z0,name,cut);
    }
    static void Hole(int axis,int sign,double[] at,double dia,double depth,string name,double bore=0,double boreDepth=0,string thread=null,string series="UNC",double threadDepth=0)
    {
        var p=Profile(axis,at[axis]);var uv=UV(p,at);Keep(p.Holes2d.Add(uv[0],uv[1]));End(p);
        Part.HoleData data;
        if(thread!=null)
        {
            data=Keep(doc.HoleDataCollection.AddEx(Part.FeaturePropertyConstants.igRegularHole,"ANSI Inch",series,thread,"2B",HoleDiameter:dia*Inch,TreatmentType:Part.FeaturePropertyConstants.igTappedHole,IgnoreSavedDefaultValues:true));
            Say("Thread table: "+data.ThreadDescription+" requested="+thread+" tap_in="+(data.ThreadTapDrillDiameter/Inch));
            if(!string.Equals(data.ThreadDescription.Trim(),thread,StringComparison.OrdinalIgnoreCase))throw new Exception("Thread table mismatch for "+thread);
            data.ThreadDiameterOption=Part.ThreadDiameterOptionConstants.seTapDrillDiameter;
            data.ThreadDepthMethod=Part.FeaturePropertyConstants.igFinite;
            data.ThreadDepth=threadDepth*Inch;
            data.BottomAngle=0;
        }
        else data=Keep(doc.HoleDataCollection.Add(bore>0 ? Part.FeaturePropertyConstants.igCounterboreHole : Part.FeaturePropertyConstants.igRegularHole,dia*Inch,CounterboreDiameter:bore*Inch,CounterboreDepth:boreDepth*Inch,BottomAngle:0.0,TreatmentType:Part.FeaturePropertyConstants.igNone,CounterboreProfileLocationType:Part.FeaturePropertyConstants.igCounterboreProfileIsAtTop,IgnoreSavedDefaultValues:true));
        double before=V();var h=Keep(model.Holes.AddFinite(p,Direction(p,axis,sign),depth*Inch,data));h.Name=name;
        object desc;if(h.get_Status(out desc)!=Part.FeatureStatusConstants.igFeatureOK)throw new Exception(name+": "+desc);
        if(V()>=before-1e-14)throw new Exception(name+": no material removed");
        Say(name+" OK");
    }


}
[ComImport,Guid("00000016-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMessageFilter
{
    [PreserveSig]int HandleInComingCall(int type,IntPtr caller,int ticks,IntPtr info);
    [PreserveSig]int RetryRejectedCall(IntPtr callee,int ticks,int type);
    [PreserveSig]int MessagePending(IntPtr callee,int ticks,int type);
}
sealed class BusyFilter:IMessageFilter,IDisposable
{
    IMessageFilter previous;
    public BusyFilter(){Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(this,out previous));}
    public int HandleInComingCall(int type,IntPtr caller,int ticks,IntPtr info){return 0;}
    public int RetryRejectedCall(IntPtr callee,int ticks,int type){return type==2 && ticks<30000 ? 250 : -1;}
    public int MessagePending(IntPtr callee,int ticks,int type){return 2;}
    public void Dispose(){IMessageFilter removed;Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(previous,out removed));GC.KeepAlive(this);}
    [DllImport("ole32.dll")]static extern int CoRegisterMessageFilter(IMessageFilter current,out IMessageFilter previous);
}

