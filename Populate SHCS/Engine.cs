using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using A=SolidEdge.Assembly.Interop;
using F=SolidEdge.Framework.Interop;
using G=SolidEdge.Geometry.Interop;
using P=SolidEdge.Part.Interop;

namespace PopulateSHCS
{
    public class Component
    {
        public string Name,File;public Shape Shape;
    }
    public class Scan
    {
        public List<Hole> Holes=new List<Hole>();public List<Component> Components=new List<Component>();public List<string> Issues=new List<string>();
        public string Signature;
    }
    public sealed class Engine:IDisposable
    {
        readonly ComScope scope=new ComScope();
        public F.Application App;public A.AssemblyDocument Assembly;
        public readonly List<Size> Sizes=Rules.Chart();
        public Action<string> Progress=delegate{};public Func<bool> Cancel=()=>false;
        public Engine(){try{App=scope.Keep((F.Application)Marshal.GetActiveObject("SolidEdge.Application"));Assembly=scope.Keep(App.ActiveDocument as A.AssemblyDocument);if(Assembly==null)throw new InvalidOperationException("Open the main assembly in Solid Edge first.");EnsureActive();}catch{scope.Dispose();throw;}}
        public void Dispose(){scope.Dispose();}
        void EnsureActive(){using(var c=new ComScope()){var current=c.Keep(App.ActiveDocument);if(!Object.ReferenceEquals(current,Assembly))throw new InvalidOperationException("The active Solid Edge document changed. Return to the original main assembly and scan again.");if(Assembly.InPlaceActivated)throw new InvalidOperationException("Finish Edit In Place and activate the main assembly first.");}}
        public Scan Review(IEnumerable<CatalogPart> catalog)
        {
            EnsureActive();var scan=new Scan();var cache=new Dictionary<string,List<Shape>>(StringComparer.OrdinalIgnoreCase);
            using(var c=new ComScope()){
                var occurrences=c.Keep(Assembly.Occurrences);
                for(int i=1;i<=occurrences.Count;i++)using(var oc=new ComScope()){
                    var o=oc.Keep(occurrences.Item(i));Walk(o,null,o.Name,scan,cache,oc,0);
                }
            }
            foreach(var component in scan.Components){
                foreach(var h in Rules.Holes(component.Shape.Cylinders,Sizes)){
                    h.Owner=component.Name;
                    var other=scan.Components.Where(x=>x.Name!=component.Name).ToList();
                    var occupied=other.FirstOrDefault(x=>x.Shape.Cylinders.Any(c=>Rules.Occupied(h,c)));
                    var obstructed=other.FirstOrDefault(x=>x.Shape.MayOccupy(h));
                    if(occupied!=null)h.Status="Occupied: "+occupied.Name;
                    else if(obstructed!=null)h.Status="Review obstruction: "+obstructed.Name;
                    else if(!h.Through)h.Status="Blind or interrupted pilot: manual review";
                    else{h.Part=Rules.Choose(h,catalog);h.Status=h.Part==null?"Choose SHCS in Standard Parts":"Ready";}
                    if(scan.Issues.Count>0 && h.Status=="Ready")h.Status="Blocked: incomplete assembly scan";
                    scan.Holes.Add(h);
                }
            }
            foreach(var h in scan.Holes)if(scan.Holes.Any(x=>x!=h && x.Owner==h.Owner && x.CylinderFace==h.CylinderFace && x.Axis.Dot(h.Axis)<-.99999))h.Status="Opposed counterbores: manual review";
            // Rebuild this fingerprint before any insertion. Include the numeric scene,
            // including obstacles, not just occurrence counts or a document filename.
            scan.Signature=String.Join("|",scan.Components.Select(c=>c.Name+";"+c.File+";"+c.Shape.Min+";"+c.Shape.Max+";"+String.Join("/",c.Shape.Cylinders.Select(x=>x.A+":"+x.B+":"+x.Radius.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":"+x.Inner+":"+x.OpenA+":"+x.OpenB))));
            return scan;
        }
        void Walk(A.Occurrence top,A.SubOccurrence sub,string name,Scan scan,Dictionary<string,List<Shape>> cache,ComScope own,int depth)
        {
            if(Cancel())throw new OperationCanceledException();Progress("Checking "+name);
            try{
                if(depth>64)throw new InvalidOperationException("Subassembly nesting exceeds 64 levels.");
                bool assembly=sub==null?top.Subassembly:sub.Subassembly;
                if(assembly){var children=own.Keep(sub==null?top.SubOccurrences:sub.SubOccurrences);for(int i=1;i<=children.Count;i++)using(var childScope=new ComScope()){var child=childScope.Keep(children.Item(i));Walk(top,child,name+" / "+child.Name,scan,cache,childScope,depth+1);}return;}
                if(sub==null && top.IsNongraphic)return;
                if(sub==null && !top.Activate)throw new InvalidOperationException("Inactive component. Activate components before scanning.");
                if(sub==null?top.HasBodyOverride:sub.HasBodyOverride)throw new InvalidOperationException("Assembly body override requires manual review.");
                if(sub==null?top.IsAdjustablePart:sub.IsAdjustablePart)throw new InvalidOperationException("Adjustable part requires manual review.");
                Array matrix=new double[16];if(sub==null)top.GetMatrix(ref matrix);else sub.GetMatrix(ref matrix);var frame=Frame.Read(matrix);
                string file=sub==null?top.OccurrenceFileName:sub.SubOccurrenceFileName;
                var document=own.Keep(sub==null?top.OccurrenceDocument:sub.SubOccurrenceDocument);
                if(document==null)throw new InvalidOperationException("Unresolved component document.");
                List<Shape> shapes;
                if(!cache.TryGetValue(file,out shapes)){
                    shapes=new List<Shape>();dynamic d=document;var models=own.Keep((object)d.Models);dynamic mm=models;
                    for(int i=1;i<=mm.Count;i++)using(var mc=new ComScope()){
                        dynamic model=mc.Keep((object)mm.Item(i));var body=mc.Keep((G.Body)model.Body);if(body!=null)shapes.Add(Geometry.Read(body));
                    }
                    if(shapes.Count==0)throw new InvalidOperationException("No readable solid models.");cache[file]=shapes;
                }
                for(int n=0;n<shapes.Count;n++)scan.Components.Add(new Component{Name=name+" [body "+(n+1)+"]",File=file,Shape=shapes[n].Transform(frame)});
            }catch(OperationCanceledException){throw;}catch(Exception ex){scan.Issues.Add(name+": "+ex.Message);}
        }
        public Tuple<Frame,double,double> ValidatePart(CatalogPart part)
        {
            using(var c=new ComScope()){
                var docs=c.Keep(App.Documents);P.PartDocument doc=null;bool opened=false;
                // Reuse an already-open library document and preserve its unsaved state.
                for(int i=1;i<=docs.Count;i++){
                    dynamic d=c.Keep(docs.Item(i));if(String.Equals((string)d.FullName,part.Path,StringComparison.OrdinalIgnoreCase)){doc=d as P.PartDocument;break;}
                }
                if(doc==null){doc=c.Keep((P.PartDocument)docs.Open(part.Path));opened=true;}
                try{var models=c.Keep(doc.Models);if(models.Count!=1)throw new InvalidOperationException("SHCS model must contain one solid body.");var model=c.Keep(models.Item(1));var body=c.Keep((G.Body)model.Body);var shape=Geometry.Read(body);double hd,hh;var f=Geometry.Mount(shape,part,out hd,out hh);return Tuple.Create(f,hd,hh);}
                finally{if(opened)doc.Close(false);Assembly.Activate();}
            }
        }
        public int Populate(Scan reviewed,List<Hole> selected,List<CatalogPart> catalog)
        {
            EnsureActive();if(selected.Count==0)return 0;
            var mounts=new Dictionary<string,Tuple<Frame,double,double>>(StringComparer.OrdinalIgnoreCase);
            // Verify every chosen source before altering the assembly. Failed checks
            // never leave half of a batch placed or silently substitute a shorter screw.
            foreach(var h in selected){if(h.Status!="Ready" || h.Part==null)throw new InvalidOperationException("Only Ready rows can be inserted.");if(!mounts.ContainsKey(h.Part.Path)){Progress("Verifying "+Path.GetFileName(h.Part.Path));mounts[h.Part.Path]=ValidatePart(h.Part);}var m=mounts[h.Part.Path];if(m.Item2>h.Bore-Rules.Tol || m.Item3>h.Depth+Rules.Tol)throw new InvalidOperationException("SHCS head does not fit counterbore on "+h.Owner);}
            var fresh=Review(catalog);if(fresh.Issues.Count>0 || fresh.Signature!=reviewed.Signature)throw new InvalidOperationException("The assembly changed or contains unreadable components. Scan and review again.");
            var created=new List<A.Occurrence>();using(var c=new ComScope()){
                var occurrences=c.Keep(Assembly.Occurrences);int before=occurrences.Count;
                try{
                    foreach(var h in selected){
                        if(Cancel())throw new OperationCanceledException();EnsureActive();Progress("Inserting "+h.Size.Name+" SHCS in "+h.Owner);
                        var source=mounts[h.Part.Path].Item1;var f=Frame.Map(source.O,source.Z,h.Seat,h.Axis);Array matrix=f.Matrix();
                        var o=c.Keep(occurrences.AddWithMatrix(h.Part.Path,ref matrix));created.Add(o);
                        // AddWithMatrix inserts grounded occurrences. Verify that placement
                        // survived the solver, and add a native ground if the host omitted it.
                        dynamic relations=c.Keep(o.Relations3d);if(relations.Count==0){var all=c.Keep(Assembly.Relations3d);c.Keep(all.AddGround(o));}
                        Array placed=new double[16];o.GetMatrix(ref placed);var actual=Frame.Read(placed);
                        if((actual.Point(source.O)-h.Seat).Length>Rules.Tol || actual.Vector(source.Z).Dot(h.Axis)<.999999)throw new InvalidOperationException("Solid Edge did not retain the requested seating position.");
                    }
                    if(occurrences.Count!=before+created.Count)throw new InvalidOperationException("Unexpected assembly occurrence count.");
                    return created.Count;
                }catch(Exception error){
                    var failed=new List<string>();for(int i=created.Count-1;i>=0;i--)try{created[i].Delete();}catch(Exception ex){failed.Add(ex.Message);}
                    if(failed.Count>0 || occurrences.Count!=before)throw new InvalidOperationException("Insertion failed; some newly inserted screws could not be removed. Inspect the assembly before retrying. "+error.Message+" / "+String.Join(";",failed),error);
                    throw new InvalidOperationException("Insertion cancelled or failed. All screws from this batch were removed. "+error.Message,error);
                }
            }
        }
    }
}
