using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using G=SolidEdge.Geometry.Interop;

namespace PopulateSHCS
{
    // Short scopes release geometry RCWs immediately after copying numeric data.
    // Never FinalReleaseComObject: Solid Edge and other callers may share a wrapper.
    public sealed class ComScope:IDisposable
    {
        readonly List<object> values=new List<object>();
        public T Keep<T>(T value){if(value!=null && !values.Any(x=>Object.ReferenceEquals(x,value)))values.Add(value);return value;}
        public void Dispose(){for(int i=values.Count-1;i>=0;i--)if(Marshal.IsComObject(values[i]))try{Marshal.ReleaseComObject(values[i]);}catch(COMException){} values.Clear();}
    }
    public class Shape
    {
        public List<Cylinder> Cylinders=new List<Cylinder>(); public V Min,Max;
        public Shape Transform(Frame f)
        {
            var s=new Shape();var corners=new List<V>();
            foreach(double x in new[]{Min.X,Max.X})foreach(double y in new[]{Min.Y,Max.Y})foreach(double z in new[]{Min.Z,Max.Z})corners.Add(f.Point(new V(x,y,z)));
            s.Min=new V(corners.Min(p=>p.X),corners.Min(p=>p.Y),corners.Min(p=>p.Z));s.Max=new V(corners.Max(p=>p.X),corners.Max(p=>p.Y),corners.Max(p=>p.Z));
            foreach(var c in Cylinders)s.Cylinders.Add(new Cylinder{A=f.Point(c.A),B=f.Point(c.B),Radius=c.Radius,Inner=c.Inner,FaceId=c.FaceId,PlanesA=c.PlanesA,PlanesB=c.PlanesB,OpenA=c.OpenA,OpenB=c.OpenB});return s;
        }
        public bool MayOccupy(Hole h)
        {
            // A conservative box test rejects uncertain obstructions; it never calls an
            // unreadable or noncylindrical object an empty hole. Exclude mere seat contact.
            var a=h.Seat-h.Axis*(h.Depth-Rules.Tol);var b=h.Seat-h.Axis*Rules.Tol;
            double r=h.Bore*.49;var d=h.Axis;
            var e=new V(r*Math.Sqrt(Math.Max(0,1-d.X*d.X)),r*Math.Sqrt(Math.Max(0,1-d.Y*d.Y)),r*Math.Sqrt(Math.Max(0,1-d.Z*d.Z)));
            return Math.Max(a.X,b.X)+e.X>Min.X+Rules.Tol && Math.Min(a.X,b.X)-e.X<Max.X-Rules.Tol &&
                Math.Max(a.Y,b.Y)+e.Y>Min.Y+Rules.Tol && Math.Min(a.Y,b.Y)-e.Y<Max.Y-Rules.Tol &&
                Math.Max(a.Z,b.Z)+e.Z>Min.Z+Rules.Tol && Math.Min(a.Z,b.Z)-e.Z<Max.Z-Rules.Tol;
        }
    }
    public static class Geometry
    {
        class Ring{public V Center;public double Length;public bool Open;public HashSet<int> Planes=new HashSet<int>();}
        public static Shape Read(G.Body body)
        {
            if(!body.IsSolid || !body.Valid)throw new InvalidOperationException("Component has no valid solid body.");
            var result=new Shape();Array lo=new double[3],hi=new double[3];body.GetExactRange(ref lo,ref hi);result.Min=V.From(lo);result.Max=V.From(hi);
            using(var scope=new ComScope()){
                var faces=scope.Keep((G.Faces)body.Faces[G.FeatureTopologyQueryTypeConstants.igQueryCylinder]);
                for(int i=1;i<=faces.Count;i++)using(var fs=new ComScope()){
                    var face=fs.Keep((G.Face)faces.Item(i));var cylinder=fs.Keep((G.Cylinder)face.Geometry);
                    Array origin=new double[3],axis=new double[3];double radius;cylinder.GetCylinderData(ref origin,ref axis,out radius);
                    V o=V.From(origin),z=V.From(axis).Unit;var rings=new List<Ring>();var edges=fs.Keep((G.Edges)face.Edges);
                    for(int e=1;e<=edges.Count;e++)using(var es=new ComScope()){
                        var edge=es.Keep((G.Edge)edges.Item(e));var curve=es.Keep(edge.Geometry);var circle=curve as G.Circle;if(circle==null)continue;
                        Array ctr=new double[3],normal=new double[3];double rr;circle.GetCircleData(ref ctr,ref normal,out rr);
                        if(Math.Abs(rr-radius)>Rules.Tol || Math.Abs(V.From(normal).Unit.Dot(z))<.999999)continue;
                        var center=V.From(ctr);double p,q,len;edge.GetParamExtents(out p,out q);edge.GetLengthAtParam(p,q,out len);
                        var ring=rings.FirstOrDefault(v=>(v.Center-center).Length<Rules.Tol);if(ring==null){ring=new Ring{Center=center};rings.Add(ring);}ring.Length+=Math.Abs(len);
                        int count;Array neighbors=new object[0];edge.GetFaces(out count,ref neighbors);
                        foreach(object n in neighbors){var nf=es.Keep((G.Face)n);var ng=es.Keep(nf.Geometry);if(ng is G.Plane){ring.Planes.Add(nf.ID);var loops=es.Keep((G.Loops)nf.Loops);if(loops.Count>1)ring.Open=true;}}
                    }
                    rings=rings.Where(r=>Math.Abs(r.Length-2*Math.PI*radius)<Math.Max(Rules.Tol,radius*.001)).OrderBy(r=>(r.Center-o).Dot(z)).ToList();
                    if(rings.Count!=2 || (rings[1].Center-rings[0].Center).Length<Rules.Tol)continue;
                    Array mn=new double[2],mx=new double[2];face.GetParamRange(ref mn,ref mx);
                    Array uv=new[]{(Convert.ToDouble(mn.GetValue(0))+Convert.ToDouble(mx.GetValue(0)))*.5,(Convert.ToDouble(mn.GetValue(1))+Convert.ToDouble(mx.GetValue(1)))*.5};
                    Array pt=new double[3],norm=new double[3];face.GetPointAtParam(1,ref uv,ref pt);face.GetNormal(1,ref uv,ref norm);
                    V radial=V.From(pt)-o;radial=radial-z*radial.Dot(z);
                    // GetNormal returns oriented face normals (verified by scratch-hole regression).
                    bool inner=V.From(norm).Dot(radial)<0;
                    result.Cylinders.Add(new Cylinder{A=rings[0].Center,B=rings[1].Center,Radius=radius,Inner=inner,FaceId=face.ID,PlanesA=rings[0].Planes,PlanesB=rings[1].Planes,OpenA=rings[0].Open,OpenB=rings[1].Open});
                }
            }
            return result;
        }
        public static Frame Mount(Shape s,CatalogPart part,out double headDiameter,out double headHeight)
        {
            var shafts=s.Cylinders.Where(c=>!c.Inner && Math.Abs(2*c.Radius-part.Diameter)<Math.Min(.00005,part.Diameter*.015)).ToList();
            var heads=s.Cylinders.Where(c=>!c.Inner && c.Radius>part.Diameter*.65 && c.Radius<part.Diameter).OrderByDescending(c=>c.Radius).ToList();
            var mounts=new List<Tuple<Frame,double,double>>();
            foreach(var head in heads)foreach(var shaft in shafts){
                if(Math.Abs(head.Axis.Dot(shaft.Axis))<.999999 || (shaft.A-head.A).Cross(head.Axis).Length>Rules.Tol)continue;
                foreach(var seat in new[]{head.A,head.B}){
                    var other=(seat-head.A).Length<Rules.Tol?head.B:head.A;var direction=(seat-other).Unit;
                    double a=(shaft.A-seat).Dot(direction),b=(shaft.B-seat).Dot(direction);
                    if(Math.Min(a,b)<-Rules.Tol || Math.Max(a,b)<Rules.Tol)continue;
                    // The outer head rim must border a planar underside. This excludes the top.
                    if(((seat-head.A).Length<Rules.Tol?head.PlanesA:head.PlanesB).Count==0)continue;
                    // Catalog models use a principal axis. Reject uncertain tilted models
                    // instead of estimating length from a rotated bounding box.
                    var n=direction;double max=Math.Max(Math.Abs(n.X),Math.Max(Math.Abs(n.Y),Math.Abs(n.Z)));if(max<.999999)continue;
                    double tip=Math.Max((s.Min-seat).Dot(n),(s.Max-seat).Dot(n));
                    if(Math.Abs(tip-part.Length)>.002*Rules.Inch)continue;
                    double height=Math.Max((s.Min-seat).Dot(direction*-1),(s.Max-seat).Dot(direction*-1));
                    mounts.Add(Tuple.Create(new Frame(seat,direction),head.Radius*2,height));
                }
            }
            if(mounts.Count==0)throw new InvalidOperationException("Could not verify SHCS underside, shank diameter and catalog length: "+System.IO.Path.GetFileName(part.Path));
            var m=mounts[0];if(mounts.Any(x=>(x.Item1.O-m.Item1.O).Length>Rules.Tol || x.Item1.Z.Dot(m.Item1.Z)<.999999))throw new InvalidOperationException("Ambiguous SHCS mounting geometry.");
            headDiameter=m.Item2;headHeight=m.Item3;return m.Item1;
        }
    }
    // Based on the bundled SDK OleMessageFilter example; retry only for a bounded
    // interval, and restore the previous filter rather than overwriting its owner.
    [ComImport,Guid("00000016-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOleFilter
    {
        [PreserveSig]int HandleInComingCall(int type,IntPtr task,int ticks,IntPtr info);
        [PreserveSig]int RetryRejectedCall(IntPtr task,int ticks,int reject);
        [PreserveSig]int MessagePending(IntPtr task,int ticks,int pending);
    }
    public sealed class BusyFilter:IOleFilter,IDisposable
    {
        IOleFilter old;
        [DllImport("ole32.dll")]static extern int CoRegisterMessageFilter(IOleFilter value,out IOleFilter previous);
        public BusyFilter(){Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(this,out old));}
        public int HandleInComingCall(int a,IntPtr b,int c,IntPtr d){return 0;}
        public int RetryRejectedCall(IntPtr a,int ticks,int reject){return reject==2 && ticks<10000?100:-1;}
        public int MessagePending(IntPtr a,int b,int c){return 2;}
        public void Dispose(){IOleFilter discarded;CoRegisterMessageFilter(old,out discarded);}
    }
}
