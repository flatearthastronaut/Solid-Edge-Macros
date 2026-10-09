using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PopulateSHCS;
using P=SolidEdge.Part.Interop;
using G=SolidEdge.Geometry.Interop;
using F=SolidEdge.Framework.Interop;
class Tests
{
    static int count;
    static void Check(bool pass,string name){if(!pass)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);count++;}
    [STAThread]static int Main(string[] args){try{if(args.Length>0){if(args[0]=="--live")LiveTests.Run();else Inspect(args[0]);return 0;}Unit();return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
    static void Unit()
    {
        var chart=Rules.Chart();Check(chart.Count==26,"All chart rows");
        var size=Rules.Match(.35*Rules.Inch,.22*Rules.Inch,chart);Check(size.Name=="#10","Identical metric/inch holes prefer inch");
        Check(Rules.Match(.41*Rules.Inch,.28*Rules.Inch,chart).Name=="1/4","Drill diameter disambiguates head size");
        Check(Rules.Match(.35*Rules.Inch,.19*Rules.Inch,chart)==null,"Unknown drill rejected");
        var big=new Cylinder{A=new V(0,0,0),B=new V(0,0,.007112),Radius=.410*Rules.Inch/2,Inner=true};big.PlanesB.Add(7);
        var small=new Cylinder{A=big.B,B=new V(0,0,.019812),Radius=.280*Rules.Inch/2,Inner=true};small.PlanesA.Add(7);
        var holes=Rules.Holes(new List<Cylinder>{big,small},chart);Check(holes.Count==1,"Counterbore shared shoulder recognized");
        Check(!holes[0].Through,"Blind pilot is not marked through");small.OpenB=true;Check(Rules.Holes(new List<Cylinder>{big,small},chart)[0].Through,"Open pilot exit retained");
        var h=holes[0];Check(Math.Abs(h.Required/Rules.Inch-.875)<1e-9,"Length is nominal diameter times 1.5 plus pilot cylinder");
        var parts=new[]{.75,.875,1.0}.Select(l=>new CatalogPart{Size="1/4",Length=l*Rules.Inch,Path=l.ToString()}).ToList();
        Check(Rules.Choose(h,parts).Length==.875*Rules.Inch,"Exact available length is retained");h.Grip+=.001*Rules.Inch;
        Check(Rules.Choose(h,parts).Length==Rules.Inch,"Round up to next available length");h.Grip=2*Rules.Inch;Check(Rules.Choose(h,parts)==null,"No shorter fallback");
        small.PlanesA.Clear();Check(Rules.Holes(new List<Cylinder>{big,small},chart).Count==0,"Disconnected cylinders rejected");small.PlanesA.Add(7);
        big.Inner=false;Check(Rules.Holes(new List<Cylinder>{big,small},chart).Count==0,"External stepped shafts rejected");big.Inner=true;
        var from=new V(.4,.3,.2);var to=new V(-.2,.5,.7);var dir=new V(1,2,3).Unit;var frame=Frame.Map(from,new V(0,0,-1),to,dir);
        Check((frame.Point(from)-to).Length<1e-10 && (frame.Vector(new V(0,0,-1))-dir).Length<1e-10,"Rotated/reversed arbitrary mounting frame");
        Check((Frame.Read(frame.Matrix()).Point(from)-to).Length<1e-10,"Solid Edge matrix layout round-trip");
        Check(Rules.ParsePart("Screw_ANSI_B18_3_A_1I4x1_1I4_v6.50.par",chart).Length==1.25*Rules.Inch,"Standard Parts fractions");
        Check(Rules.ParsePart("Screw_ANSI_B18_3_A_No10x1I2_v6.50.par",chart).Size=="#10","Standard Parts numbered screws");
        Check(Rules.ParsePart("Screw_ANSI_B18_3_C_1I4x1_v6.50.par",chart)==null,"Other head families rejected");
        h.Grip=.5*Rules.Inch;
        var bolt=new Cylinder{A=h.Seat-h.Axis*.003,B=h.Seat+h.Axis*.01,Radius=.003,Inner=false};
        Check(Rules.Occupied(h,bolt),"Existing coaxial hardware blocks duplicates");bolt.A+=new V(.1,0,0);bolt.B+=new V(.1,0,0);Check(!Rules.Occupied(h,bolt),"Adjacent hardware does not occupy hole");
        var near=new Shape{Min=new V(-.01,-.01,-.01),Max=new V(.01,.01,h.Seat.Z-.001)};Check(near.MayOccupy(h),"Noncylindrical obstruction flagged conservatively");near.Max=new V(.01,.01,-.1);near.Min=new V(-.01,-.01,-.2);Check(!near.MayOccupy(h),"Remote component does not obstruct head cavity");
        string index=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"catalog-files.txt");if(File.Exists(index)){var indexed=File.ReadAllLines(index).Select(p=>Rules.ParsePart(p,chart)).Where(p=>p!=null).ToList();Check(chart.Where(s=>!s.Metric).All(s=>indexed.Any(p=>p.Size==s.Name)),"Real library filenames cover every inch chart size");}
        Console.WriteLine(count+" regression checks passed.");
    }
    static void Inspect(string path)
    {
        using(var filter=new BusyFilter())using(var c=new ComScope()){
            var app=c.Keep((F.Application)Marshal.GetActiveObject("SolidEdge.Application"));var docs=c.Keep(app.Documents);object previous=c.Keep(app.ActiveDocument);
            var doc=c.Keep((P.PartDocument)docs.Open(Path.GetFullPath(path)));try{
                var models=c.Keep(doc.Models);var model=c.Keep(models.Item(1));var body=c.Keep((G.Body)model.Body);var s=Geometry.Read(body);
                Console.WriteLine("Bounds "+s.Min+" / "+s.Max);
                foreach(var cy in s.Cylinders)Console.WriteLine("Cylinder "+cy.FaceId+" inner="+cy.Inner+" d="+cy.Radius*2/Rules.Inch+" len="+cy.Length/Rules.Inch+" A="+cy.A+" B="+cy.B+" planes="+String.Join(",",cy.PlanesA)+" / "+String.Join(",",cy.PlanesB));
                double hd,hh;var mount=Geometry.Mount(s,new CatalogPart{Path=path,Diameter=.25*Rules.Inch,Length=.875*Rules.Inch},out hd,out hh);
                Console.WriteLine("Mount="+mount.O+" direction="+mount.Z+" head dia="+hd/Rules.Inch+" height="+hh/Rules.Inch);
            }finally{doc.Close(false);if(previous!=null)((dynamic)previous).Activate();}
        }
    }
}
