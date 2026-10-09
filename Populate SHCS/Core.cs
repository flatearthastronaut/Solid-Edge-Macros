using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace PopulateSHCS
{
    // All geometry and calculations use meters. Convert only at the chart/UI boundary.
    public struct V
    {
        public double X,Y,Z;
        public V(double x,double y,double z){X=x;Y=y;Z=z;}
        public static V From(Array a){int n=a.GetLowerBound(0);return new V(Convert.ToDouble(a.GetValue(n)),Convert.ToDouble(a.GetValue(n+1)),Convert.ToDouble(a.GetValue(n+2)));}
        public static V operator +(V a,V b){return new V(a.X+b.X,a.Y+b.Y,a.Z+b.Z);}
        public static V operator -(V a,V b){return new V(a.X-b.X,a.Y-b.Y,a.Z-b.Z);}
        public static V operator *(V a,double s){return new V(a.X*s,a.Y*s,a.Z*s);}
        public double Dot(V b){return X*b.X+Y*b.Y+Z*b.Z;}
        public V Cross(V b){return new V(Y*b.Z-Z*b.Y,Z*b.X-X*b.Z,X*b.Y-Y*b.X);}
        public double Length{get{return Math.Sqrt(Dot(this));}}
        public V Unit{get{if(Length<1e-12)throw new InvalidDataException("Zero axis");return this*(1/Length);}}
        public override string ToString(){return String.Format(CultureInfo.InvariantCulture,"{0:F6},{1:F6},{2:F6}",X,Y,Z);}
    }
    public class Frame
    {
        public V X,Y,Z,O;
        public Frame(V origin,V z){O=origin;Z=z.Unit;X=(Math.Abs(Z.X)<.8?new V(1,0,0):new V(0,1,0)).Cross(Z).Unit;Y=Z.Cross(X);}
        public V Point(V p){return O+Vector(p);}
        public V Vector(V p){return X*p.X+Y*p.Y+Z*p.Z;}
        public V Unpoint(V p){p=p-O;return new V(p.Dot(X),p.Dot(Y),p.Dot(Z));}
        public static Frame Read(Array a){double[] m=a.Cast<object>().Select(Convert.ToDouble).ToArray();var f=new Frame(new V(m[12],m[13],m[14]),new V(m[8],m[9],m[10]));f.X=new V(m[0],m[1],m[2]);f.Y=new V(m[4],m[5],m[6]);if(Math.Abs(f.X.Cross(f.Y).Dot(f.Z)-1)>1e-5)throw new InvalidDataException("Mirrored or scaled occurrence requires manual review.");return f;}
        public Array Matrix(){return new[]{X.X,X.Y,X.Z,0,Y.X,Y.Y,Y.Z,0,Z.X,Z.Y,Z.Z,0,O.X,O.Y,O.Z,1};}
        public static Frame Map(V fromOrigin,V fromAxis,V toOrigin,V toAxis){var a=new Frame(fromOrigin,fromAxis);var b=new Frame(toOrigin,toAxis);var r=new Frame(new V(),new V(0,0,1));r.X=b.Vector(new V(a.X.X,a.Y.X,a.Z.X));r.Y=b.Vector(new V(a.X.Y,a.Y.Y,a.Z.Y));r.Z=b.Vector(new V(a.X.Z,a.Y.Z,a.Z.Z));r.O=toOrigin-r.Vector(fromOrigin);return r;}
    }
    public class Size
    {
        public string Name; public double Drill,Bore,Diameter; public bool Metric;
    }
    public class Cylinder
    {
        public V A,B; public double Radius; public bool Inner,OpenA,OpenB; public int FaceId;
        public HashSet<int> PlanesA=new HashSet<int>(),PlanesB=new HashSet<int>();
        public double Length{get{return (B-A).Length;}}
        public V Axis{get{return (B-A).Unit;}}
    }
    public class Hole
    {
        public V Seat,Axis; public double Grip,Depth,Bore,Drill; public int SeatFace,CylinderFace;
        public Size Size; public bool Through;public string Owner,Status; public CatalogPart Part; public double Required{get{return Grip+1.5*Size.Diameter;}}
    }
    public class CatalogPart
    {
        public string Path,Size; public double Diameter,Length;
    }
    public static class Rules
    {
        public const double Inch=.0254,Tol=.000005;
        public static List<Size> Chart()
        {
            var doc=new XmlDocument{XmlResolver=null};
            using(var s=typeof(Rules).Assembly.GetManifestResourceStream("CounterboreChart.xml"))
            using(var r=XmlReader.Create(s,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))doc.Load(r);
            var result=new List<Size>();
            foreach(XmlElement e in doc.SelectNodes("/CounterboreChart/Hole")){
                string n=e.GetAttribute("screw");bool metric=n.StartsWith("M");
                double d=metric?Double.Parse(n.Substring(1).Split(' ')[0],CultureInfo.InvariantCulture)*.001:Inch*Fraction(n);
                result.Add(new Size{Name=n,Metric=metric,Diameter=d,Drill=Double.Parse(e.GetAttribute("drill"),CultureInfo.InvariantCulture)*Inch,Bore=Double.Parse(e.GetAttribute("bore"),CultureInfo.InvariantCulture)*Inch});
            }
            return result;
        }
        public static double Fraction(string value)
        {
            value=value.Replace("No","#").Replace('I','/');
            if(value.StartsWith("#"))return .060+.013*Int32.Parse(value.Substring(1),CultureInfo.InvariantCulture);
            var mixed=value.Split('_');if(mixed.Length==2)return Fraction(mixed[0])+Fraction(mixed[1]);
            var f=value.Split('/');return f.Length==2?Double.Parse(f[0],CultureInfo.InvariantCulture)/Double.Parse(f[1],CultureInfo.InvariantCulture):Double.Parse(value,CultureInfo.InvariantCulture);
        }
        public static Size Match(double bore,double drill,List<Size> sizes)
        {
            // Both diameters are required: head clearance alone cannot distinguish many rows.
            return sizes.Where(s=>Math.Abs(s.Bore-bore)<=.001*Inch && Math.Abs(s.Drill-drill)<=.001*Inch)
                .OrderBy(s=>s.Metric?1:0).ThenBy(s=>Math.Abs(s.Bore-bore)+Math.Abs(s.Drill-drill)).FirstOrDefault();
        }
        public static List<Hole> Holes(List<Cylinder> cylinders,List<Size> sizes)
        {
            var holes=new List<Hole>();
            foreach(var big in cylinders.Where(c=>c.Inner))foreach(var small in cylinders.Where(c=>c.Inner && c.Radius<big.Radius-Tol)){
                if(Math.Abs(big.Axis.Dot(small.Axis))<.999999)continue;
                if((small.A-big.A).Cross(big.Axis).Length>Tol)continue;
                for(int i=0;i<2;i++)for(int j=0;j<2;j++){
                    V b=i==0?big.A:big.B,s=j==0?small.A:small.B;
                    if((b-s).Length>Tol)continue;
                    var planes=(i==0?big.PlanesA:big.PlanesB).Intersect(j==0?small.PlanesA:small.PlanesB).ToList();
                    if(planes.Count!=1)continue; // A real shared flat shoulder prevents matching unrelated coaxial bores.
                    V into=(j==0?small.B:small.A)-s,head=(i==0?big.B:big.A)-b;
                    if(into.Unit.Dot(head.Unit)>-.999999)continue;
                    var size=Match(big.Radius*2,small.Radius*2,sizes);if(size==null)continue;
                    if(holes.Any(h=>(h.Seat-b).Length<Tol && h.Axis.Dot(into.Unit)>.999999))continue;
                    holes.Add(new Hole{Seat=b,Axis=into.Unit,Grip=small.Length,Depth=big.Length,Bore=2*big.Radius,Drill=2*small.Radius,Size=size,SeatFace=planes[0],CylinderFace=small.FaceId,Through=j==0?small.OpenB:small.OpenA});
                }
            }
            return holes;
        }
        public static CatalogPart ParsePart(string path,List<Size> sizes)
        {
            // Only the verified ANSI B18.3.A socket-head family is accepted, never button/flat heads.
            var m=Regex.Match(System.IO.Path.GetFileName(path),@"^Screw_ANSI_B18_3_A_(.+)x(.+)_v[\d.]+\.par$",RegexOptions.IgnoreCase);
            if(!m.Success)return null;
            try{string token=m.Groups[1].Value;double d=Fraction(token)*Inch;
                // Standard Parts uses a leading number sign for numbered screws; never treat a bare 4 as #4.
                var size=sizes.FirstOrDefault(s=>!s.Metric && Math.Abs(s.Diameter-d)<1e-8);if(size==null)return null;
                double len=Fraction(m.Groups[2].Value)*Inch;if(len<=0 || Double.IsInfinity(len) || Double.IsNaN(len))return null;
                return new CatalogPart{Path=path,Size=size.Name,Diameter=d,Length=len};
            }catch(FormatException){return null;}
        }
        public static CatalogPart Choose(Hole h,IEnumerable<CatalogPart> parts)
        {return parts.Where(p=>p.Size==h.Size.Name && p.Length+1e-9>=h.Required).OrderBy(p=>p.Length).ThenBy(p=>p.Path,StringComparer.OrdinalIgnoreCase).FirstOrDefault();}
        public static bool Occupied(Hole h,Cylinder c)
        {
            if(c.Inner || Math.Abs(c.Axis.Dot(h.Axis))<.9999 || (c.A-h.Seat).Cross(h.Axis).Length>Math.Max(Tol,h.Drill*.1))return false;
            double a=(c.A-h.Seat).Dot(h.Axis),b=(c.B-h.Seat).Dot(h.Axis);
            return Math.Max(a,b)>-h.Depth+Tol && Math.Min(a,b)<h.Grip-Tol && c.Radius<h.Bore/2+Tol;
        }
    }
}
