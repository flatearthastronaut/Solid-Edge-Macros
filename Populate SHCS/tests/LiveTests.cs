using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PopulateSHCS;
using A=SolidEdge.Assembly.Interop;
using P=SolidEdge.Part.Interop;
using F=SolidEdge.Framework.Interop;
using G=SolidEdge.Geometry.Interop;

static class LiveTests
{
    static void Check(bool value,string label){if(!value)throw new Exception("FAIL: "+label);Console.WriteLine("PASS: "+label);}
    public static void Run()
    {
        using(var filter=new BusyFilter())using(var c=new ComScope()){
            var app=c.Keep((F.Application)Marshal.GetActiveObject("SolidEdge.Application"));var docs=c.Keep(app.Documents);dynamic previous=c.Keep(app.ActiveDocument);
            string output=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"artifacts",DateTime.Now.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(output);
            var opened=new List<object>();
            try{
                var part=c.Keep((P.PartDocument)docs.Add("SolidEdge.PartDocument"));opened.Add(part);part.ModelingMode=P.ModelingModeConstants.seModelingModeOrdered;
                var planes=c.Keep(part.RefPlanes);P.RefPlane plane=null;
                for(int i=1;i<=3;i++){var p=c.Keep(planes.Item(i));Array n=new double[3];p.GetNormal(ref n);if(V.From(n).Z>.99){plane=p;break;}}
                if(plane==null)throw new Exception("Positive XY plane not found");
                var sets=c.Keep(part.ProfileSets);var set=c.Keep(sets.Add());var profiles=c.Keep(set.Profiles);var profile=c.Keep(profiles.Add(plane));var circles=c.Keep(profile.Circles2d);
                c.Keep(circles.AddByCenterRadius(0,0,.03));
                Check(profile.End(P.ProfileValidationType.igProfileClosed)==0,"Scratch base profile");Array list=new object[]{profile};var models=c.Keep(part.Models);var model=c.Keep(models.AddFiniteExtrudedProtrusion(1,ref list,P.FeaturePropertyConstants.igRight,.78*Rules.Inch));profile.Visible=false;
                foreach(double diameter in new[]{.28,.41}){
                    var cutset=c.Keep(sets.Add());var cutprofiles=c.Keep(cutset.Profiles);var cutprofile=c.Keep(cutprofiles.Add(plane));var cutcircles=c.Keep(cutprofile.Circles2d);c.Keep(cutcircles.AddByCenterRadius(0,0,diameter*Rules.Inch/2));
                    Check(cutprofile.End(P.ProfileValidationType.igProfileClosed)==0,"Scratch hole profile");var cuts=c.Keep(model.ExtrudedCutouts);var cut=c.Keep(cuts.AddFinite(cutprofile,P.FeaturePropertyConstants.igLeft,P.FeaturePropertyConstants.igRight,(diameter==.28?.78:.28)*Rules.Inch));cutprofile.Visible=false;
                    object description;Check(cut.get_Status(out description)==P.FeatureStatusConstants.igFeatureOK,"Scratch cut geometry");
                }
                var body=c.Keep((G.Body)model.Body);var shape=Geometry.Read(body);foreach(var cy in shape.Cylinders)Console.WriteLine("fixture cylinder "+cy.FaceId+" inner="+cy.Inner+" d="+cy.Radius*2/Rules.Inch+" length="+cy.Length/Rules.Inch);
                var localHoles=Rules.Holes(shape.Cylinders,Rules.Chart());Check(localHoles.Count==1,"Live topology detects exactly one counterbore");var hole=localHoles[0];Check(Math.Abs(hole.Required/Rules.Inch-.875)<1e-8,"Live pilot length and required screw length");
                string fixture=Path.Combine(output,"Fixture.par");part.SaveAs(fixture);
                var child=c.Keep((A.AssemblyDocument)docs.Add("SolidEdge.AssemblyDocument"));opened.Add(child);var childOcc=c.Keep(child.Occurrences);var childFrame=new Frame(new V(.04,.03,.02),new V(1,0,0));Array childMatrix=childFrame.Matrix();c.Keep(childOcc.AddWithMatrix(fixture,ref childMatrix));string childFile=Path.Combine(output,"Nested.asm");child.SaveAs(childFile);
                var root=c.Keep((A.AssemblyDocument)docs.Add("SolidEdge.AssemblyDocument"));opened.Add(root);var occ=c.Keep(root.Occurrences);
                var parentFrame=new Frame(new V(.2,.1,.08),new V(0,1,0));Array parentMatrix=parentFrame.Matrix();c.Keep(occ.AddWithMatrix(childFile,ref parentMatrix));
                var directFrame=new Frame(new V(-.2,0,0),new V(0,0,1));Array directMatrix=directFrame.Matrix();var direct=c.Keep(occ.AddWithMatrix(fixture,ref directMatrix));
                var occupiedFrame=new Frame(new V(-.4,0,0),new V(0,0,-1));Array occupiedMatrix=occupiedFrame.Matrix();c.Keep(occ.AddWithMatrix(fixture,ref occupiedMatrix));
                var screw=new CatalogPart{Path=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Sample-SHCS.par")),Size="1/4",Diameter=.25*Rules.Inch,Length=.875*Rules.Inch};
                using(var engine=new Engine()){
                    engine.Progress=Console.WriteLine;var mount=engine.ValidatePart(screw).Item1;var filled=Frame.Map(mount.O,mount.Z,occupiedFrame.Point(hole.Seat),occupiedFrame.Vector(hole.Axis));Array filledMatrix=filled.Matrix();c.Keep(occ.AddWithMatrix(screw.Path,ref filledMatrix));
                    var catalog=new List<CatalogPart>{screw};var scan=engine.Review(catalog);foreach(var issue in scan.Issues)Console.WriteLine("ISSUE "+issue);foreach(var h in scan.Holes)Console.WriteLine("HOLE "+h.Owner+" seat="+h.Seat+" axis="+h.Axis+" "+h.Status);
                    Check(scan.Issues.Count==0 && scan.Holes.Count==3,"All direct and nested fixture occurrences scanned");
                    var expected=parentFrame.Point(childFrame.Point(hole.Seat));Check(scan.Holes.Any(h=>(h.Seat-expected).Length<Rules.Tol),"Nested transforms are in main assembly coordinates");
                    Check(scan.Holes.Count(h=>h.Status.StartsWith("Occupied"))==1 && scan.Holes.Count(h=>h.Status=="Ready")==2,"Existing screw detected; only empty counterbores ready");
                    int original=occ.Count,inserting=0;engine.Progress=text=>{Console.WriteLine(text);if(text.StartsWith("Inserting") && ++inserting==2)throw new OperationCanceledException();};bool rolled=false;
                    try{engine.Populate(scan,scan.Holes.Where(h=>h.Status=="Ready").ToList(),catalog);}catch(InvalidOperationException ex){Console.WriteLine(ex.Message);rolled=ex.Message.Contains("All screws from this batch were removed");}
                    Check(rolled && occ.Count==original,"Mid-batch cancellation rolls back only new screws");
                    engine.Progress=Console.WriteLine;
                    Array moved=new Frame(new V(-.21,0,0),new V(0,0,1)).Matrix();direct.PutMatrix(ref moved,true);bool stale=false;try{engine.Populate(scan,scan.Holes.Where(h=>h.Status=="Ready").ToList(),catalog);}catch(InvalidOperationException ex){stale=ex.Message.Contains("assembly changed");}
                    Check(stale && occ.Count==original,"Stale scan rejected before inserting");direct.PutMatrix(ref directMatrix,true);
                    scan=engine.Review(catalog);int added=engine.Populate(scan,scan.Holes.Where(h=>h.Status=="Ready").ToList(),catalog);Check(added==2 && occ.Count==original+2,"Two screws inserted into main assembly");
                    var repeat=engine.Review(catalog);Check(repeat.Holes.Count(h=>h.Status.StartsWith("Occupied"))==3 && repeat.Holes.All(h=>h.Status!="Ready"),"Repeat scan prevents duplicate screws");
                }
            }finally{
                for(int i=opened.Count-1;i>=0;i--)try{((dynamic)opened[i]).Close(false);}catch(Exception ex){Console.WriteLine("Scratch cleanup: "+ex.Message);}
                if(previous!=null)previous.Activate();
            }
        }
    }
}
