using System;
using System.Collections.Generic;
using System.Linq;

namespace TurboCallout
{
    internal sealed class CirclePoint
    {
        internal double X, Y;
        internal CirclePoint(double x, double y) { X = x; Y = y; }
    }
    internal sealed class CircleFit
    {
        internal double X, Y, Radius;
        internal int A, B, C;
    }
    internal static class BoltCircleGeometry
    {
        // Coordinates are view-plane metres. This is numerical equivalence,
        // never a manufacturing tolerance or a rounded displayed dimension.
        internal const double Tolerance = 1e-8;
        internal static double Distance(CirclePoint a, CirclePoint b)
        { double x=a.X-b.X,y=a.Y-b.Y;return Math.Sqrt(x*x+y*y); }
        internal static bool Finite(double n) { return !Double.IsNaN(n)&&!Double.IsInfinity(n); }
        internal static CircleFit Fit(IList<CirclePoint> points, bool allowDiameterPair)
        {
            if(points.Count<2||points.Any(p=>!Finite(p.X)||!Finite(p.Y)))return null;
            for(int i=0;i<points.Count;i++)for(int j=0;j<i;j++)
                if(Distance(points[i],points[j])<=Tolerance)return null;
            var a=points[0];
            if(points.Count==2)
                return allowDiameterPair?new CircleFit{X=(a.X+points[1].X)/2,Y=(a.Y+points[1].Y)/2,
                    Radius=Distance(a,points[1])/2,A=0,B=1,C=-1}:null;
            // Choose a well-separated triangle, working relative to the first
            // point so a large drawing origin cannot spoil the determinant.
            int b=1,c=-1;double far=0,area=0;
            for(int i=1;i<points.Count;i++){double d=Distance(a,points[i]);if(d>far){far=d;b=i;}}
            double bx=points[b].X-a.X,by=points[b].Y-a.Y;
            for(int i=1;i<points.Count;i++)
            {double cross=Math.Abs(bx*(points[i].Y-a.Y)-by*(points[i].X-a.X));if(cross>area){area=cross;c=i;}}
            if(c<0||area<=far*far*1e-8)return null;
            double cx=points[c].X-a.X,cy=points[c].Y-a.Y,det=2*(bx*cy-by*cx);
            double bb=bx*bx+by*by,cc=cx*cx+cy*cy;
            var fit=new CircleFit{X=a.X+(cy*bb-by*cc)/det,Y=a.Y+(bx*cc-cx*bb)/det,A=0,B=b,C=c};
            var center=new CirclePoint(fit.X,fit.Y);fit.Radius=Distance(a,center);
            if(!Finite(fit.Radius)||fit.Radius<=Tolerance)return null;
            return points.All(p=>Math.Abs(Distance(p,center)-fit.Radius)<=Tolerance)?fit:null;
        }
        internal static bool EquallySpaced(IList<CirclePoint> points,CircleFit fit)
        {
            if(fit==null||points.Count<3)return false;
            double[] angles=points.Select(p=>Math.Atan2(p.Y-fit.Y,p.X-fit.X)).OrderBy(a=>a).ToArray();
            double step=2*Math.PI/points.Count;
            for(int i=0;i<angles.Length;i++)
            {double gap=i==angles.Length-1?angles[0]+2*Math.PI-angles[i]:angles[i+1]-angles[i];if(Math.Abs(gap-step)>1e-5)return false;}
            return true;
        }
    }
}
