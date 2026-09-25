/* EXPLANATORY NOTE
SKETCH TO MODELS -- DOCUMENTED v2.31 SOURCE
Updated September 25, 2026. SDK-reviewed stability rebuild; v2.30 color rules retained.
SketchToModels.cs and SketchToModels-Commented.cs contain the same documented source.

READING ORDER
Start at Program.Main, then MainWindow.ProcessSelection, then Engine.Create.
These show startup, the selected-row batch, and the lifecycle of one part.
The other classes provide placement, assisted modeling, validation and styling.

This is a standalone C# Windows Forms executable, not an installed CAD add-in.
It attaches to the running Solid Edge 2026 application through COM automation.
Most CAD calls use dynamic, which resolves COM properties/methods at runtime.
Some calls use typed Siemens interfaces for precise array/ref/interface marshaling.

STANDARD WORKFLOW
Assembly sketch -> new part at default placement -> associative sketch copy
-> solid revolve on Right (yz), with automatic Z centerline -> Project to Sketch
-> user picks outline and finishes -> Continue -> validate, color and save.

BUSHING WORKFLOW
New part -> user picks assembly coordinate system -> coordinate relationship
-> associative sketch copy -> surface revolve using Feature's Plane, automatic
part-Z centerline and projection -> Continue -> solid revolve on Right (yz)
with user-picked axis -> Continue -> validate, color, save and next part.

This is assisted modeling, not an unattended geometry generator. The user still
selects source geometry and finishes features. A feature's internal ProfileSet
is expected; a second standalone Sketch is not.

BUILDING
Build.cmd compiles SketchToModels.cs and ComSupport.cs together. To compile the alternate copy, substitute its input filename and a
different output EXE name. Do not compile both source copies together: they define
the same classes. Earlier versioned executables and ZIP files remain available.
*/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Windows.Automation;

namespace SketchToModels
{
/* EXPLANATORY NOTE
A grid row's data. Source is a COM assembly sketch/layout reference; Name comes
from the sketch, and FileName adds the assembly's drawing number. BushingStyle
selects the alternate workflow. ResolveSketch refreshes references before use
because closing/reopening a CAD document invalidates old automation objects.
*/
    public sealed class SketchItem
    {
        public object Source;
        public string Name, FileName;
        public bool BushingStyle;
    }

/* EXPLANATORY NOTE
Shared CAD operations and validation. Solid Edge COM collections commonly use
one-based Item indexes. Geometry lengths are metres; revolve angles are radians.
These conventions differ from screen units and normal zero-based C# arrays.
*/
    public static class Engine
    {
        // Solid Edge 2026 enum values, verified against the installed type library.
        const int IgRight = 2, IgProfileClosed = 1, IgFeatureOK = 1216476310;
/* EXPLANATORY NOTE
Opening an in-context part can ask to open its parent assembly. Temporarily
suppress alerts for this Open only, then restore the prior setting in finally.
Check FullName so a redirected open cannot send subsequent edits to the wrong
document. This is not blanket suppression of save errors or application prompts.
*/
        public static object OpenPartQuietly(dynamic app, string path)
        {
            bool previous = (bool)app.DisplayAlerts;
            object opened;
            try
            {
                app.DisplayAlerts = false;
                opened = app.Documents.Open(path);
            }
            finally { app.DisplayAlerts = previous; }
            dynamic document = opened;
            if (!String.Equals((string)document.FullName, path, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Solid Edge opened a different document instead of the requested part: " + path);
            return opened;
        }
/* EXPLANATORY NOTE
Activate the intended document before saving. An open document can still be
read-only or locked; SeekWriteAccess requests permission through Solid Edge.
Let failures propagate. A file existing on disk does not prove the latest
features were saved: it may contain only the earlier linked-sketch checkpoint.
*/
        public static void SaveWritable(dynamic document)
        {
            document.Activate();
            if ((bool)document.ReadOnly)
            {
                bool granted = false;
                document.SeekWriteAccess(ref granted);
                if (!granted || (bool)document.ReadOnly)
                    throw new IOException("Solid Edge cannot obtain write access to " + (string)document.FullName + ". Keep the document open and resolve its read-only or file-lock state before saving.");
            }
            document.Save();
        }

/* EXPLANATORY NOTE
Retained direct-API helper from the earlier automatic revolve approach.
Engine.Create currently uses the interactive prompts instead of this helper.
Typed Models/RefAxis interfaces and a ref Array fix the argument marshaling that
failed with dynamic dispatch. Type.Missing means an omitted optional argument.
*/
        public static object CreateRevolvedModel(object modelsObject, object[] profiles, object axisObject)
        {
            // Use Siemens' typed COM signatures here. Dynamic dispatch cannot reliably
            // marshal the reference-axis interface and profile SAFEARRAY for this call.
            var models = (SolidEdge.Part.Interop.Models)modelsObject;
            var axis = (SolidEdge.Part.Interop.RefAxis)axisObject;
            Array profileArray = profiles;
            return models.AddFiniteRevolvedProtrusion(profiles.Length, ref profileArray, axis,
                SolidEdge.Part.Interop.FeaturePropertyConstants.igRight, 2.0 * Math.PI,
                Type.Missing, Type.Missing);
        }

/* EXPLANATORY NOTE
Convert a profile's local two-dimensional (u,v) coordinates to part (x,y,z).
Never infer a model axis from its current screen direction or camera angle.
*/
        public static double[] PointOnProfile(dynamic profile, double u, double v)
        {
            double x = 0, y = 0, z = 0;
            profile.Convert2DCoordinate(u, v, ref x, ref y, ref z);
            return new[] { x, y, z };
        }
/* EXPLANATORY NOTE
COM SAFEARRAYs need not start at index zero. Read the actual lower bound before
extracting three components. Convert.ToDouble handles boxed numeric values.
*/
        static double[] Vector(Array values)
        {
            int first = values.GetLowerBound(0);
            return new[] { Convert.ToDouble(values.GetValue(first)), Convert.ToDouble(values.GetValue(first + 1)), Convert.ToDouble(values.GetValue(first + 2)) };
        }
        static double Dot(double[] a, double[] b) { return a[0]*b[0] + a[1]*b[1] + a[2]*b[2]; }
        public static bool IsRightName(string name)
        {
            return !String.IsNullOrWhiteSpace(name) && Regex.IsMatch(name.Trim(), @"^Right(?:\s*\([^)]*\))?$", RegexOptions.IgnoreCase);
        }
/* EXPLANATORY NOTE
Try Right and Right (yz), including the display name. Some base planes returned
null automation names during testing. The fallback identifies YZ geometrically
among the base planes: x=0, with a normal parallel to X. Ambiguity is an error;
hard-coding a reference-plane collection index would be unreliable.
*/
        public static object FindRightPlane(dynamic part)
        {
            object named = null, geometric = null;
            for (int i = 1; i <= (int)part.RefPlanes.Count; i++)
            {
                dynamic plane = part.RefPlanes.Item(i);
                string name = Convert.ToString(plane.Name);
                string display = "";
                try { display = Convert.ToString(plane.DisplayName); } catch (COMException) { }
                if (IsRightName(name) || IsRightName(display))
                {
                    if (named != null) throw new InvalidOperationException("More than one reference plane is named Right.");
                    named = plane;
                }
                // Base planes can have null automation names. Identify the YZ plane
                // geometrically among the three base planes, not by a fixed plane index.
                if (i <= 3)
                {
                    Array r = new double[3], n = new double[3];
                    plane.GetRootPoint(ref r); plane.GetNormal(ref n);
                    double[] root = Vector(r), normal = Vector(n);
                    double length = Math.Sqrt(Dot(normal, normal));
                    if (length > 1e-12 && Math.Abs(normal[0])/length > 1 - 1e-10 &&
                        Math.Abs(normal[1])/length < 1e-8 && Math.Abs(normal[2])/length < 1e-8 && Math.Abs(root[0]) < 1e-7)
                    {
                        if (geometric != null) throw new InvalidOperationException("The base Right (YZ) plane is ambiguous.");
                        geometric = plane;
                    }
                }
            }
            if (named != null) return named;
            if (geometric != null) return geometric;
            throw new InvalidOperationException("Cannot identify the Right plane by name or base-plane geometry.");
        }
/* EXPLANATORY NOTE
Use point-to-plane distances to validate support geometry. Normalize the normal
before measuring distances. A 1e-7 metre tolerance allows numerical roundoff.
Supplying the origin and X normal specifically tests the part's YZ plane.
*/
        public static void RequireRightPlane(double[] root, double[] normal, IEnumerable<double[]> profilePoints)
        {
            double length = Math.Sqrt(Dot(normal, normal));
            if (length < 1e-12) throw new InvalidOperationException("The Right plane has an invalid normal.");
            normal = normal.Select(x => x / length).ToArray();
            // SE length units are metres. The support plane must contain the global Z axis.
            if (Math.Abs(normal[2]) > 1e-8 || Math.Abs(Dot(root, normal)) > 1e-7)
                throw new InvalidOperationException("The named Right plane does not contain the part's global Z axis.");
            foreach (var point in profilePoints)
            {
                double distance = Dot(new[] { point[0]-root[0], point[1]-root[1], point[2]-root[2] }, normal);
                if (Math.Abs(distance) > 1e-7)
                    throw new InvalidOperationException("The linked sketch is not on the Right plane. Place the assembly sketch on that plane before processing.");
            }
        }
/* EXPLANATORY NOTE
Use endpoints at -50 mm and +50 mm on part Z, convert into profile coordinates,
then convert back. The round trip rejects a plane that merely receives a
projection of Z but does not contain the actual axis. In a placed bushing,
part-local Z follows the selected assembly coordinate system's Z direction;
it need not be the assembly origin's global Z direction.
*/
        public static double[] ZAxisCoordinates(dynamic profile)
        {
            double u0 = 0, v0 = 0, u1 = 0, v1 = 0;
            profile.Convert3DCoordinate(0.0, 0.0, -0.05, ref u0, ref v0);
            profile.Convert3DCoordinate(0.0, 0.0, 0.05, ref u1, ref v1);
            double[] a = PointOnProfile(profile, u0, v0), b = PointOnProfile(profile, u1, v1);
            if (Math.Abs(a[0]) > 1e-7 || Math.Abs(a[1]) > 1e-7 || Math.Abs(a[2] + 0.05) > 1e-7 ||
                Math.Abs(b[0]) > 1e-7 || Math.Abs(b[1]) > 1e-7 || Math.Abs(b[2] - 0.05) > 1e-7)
                throw new InvalidOperationException("Cannot construct the global Z axis on the Right plane.");
            return new[] { u0, v0, u1, v1 };
        }
/* EXPLANATORY NOTE
Standard-path entry to the assisted solid revolve. The template must have no
existing model. firstNewSketch is a retained parameter, unused here; the current
standalone sketch count is read directly from the part for validation.
*/
        public static void Revolve(dynamic part, int firstNewSketch)
        {
            if ((int)part.Models.Count != 0) throw new InvalidOperationException("The template already contains a model.");
            using(var prompt=new ProjectionPrompt((object)part,FindRightPlane(part),(int)part.Sketches.Count))
                if(prompt.ShowDialog()!=DialogResult.OK)
                    throw new OperationCanceledException("Revolve canceled. The linked part is preserved; the batch has stopped.");
        }
        public static void ValidateManualRevolve(dynamic part, int sketchCount) {ValidateManualRevolve(part,sketchCount,false);}
        public static void ValidateManualRevolve(dynamic part, int sketchCount, bool userAxis) {ValidateManualRevolve(part,sketchCount,userAxis,true);}
/* EXPLANATORY NOTE
Validate before saving/advancing: expected sketch/model/feature counts, healthy
feature status, positive-volume solid, full 360 degrees, and profile on Right.
Standard parts require part Z; userAxis=true permits the bushing solid's chosen
axis. requireIdle=false allows checking a finished feature before Continue exits
its command; the next check requires the idle Select command.

The Relations2d test recognizes known projection relation types, not every
possible dependency. For Bushing Style, an unrecognized relation is a warning
if the part retains an interpart link. That does NOT prove each solid-profile
entity will update when the assembly sketch changes. This distinction fixed
a real Continue blocker without claiming a complete associativity audit.
*/
        public static void ValidateManualRevolve(dynamic part, int sketchCount, bool userAxis, bool requireIdle)
        {
            if(userAxis && requireIdle && (int)part.Application.GetActiveCommand()!=45000)throw new InvalidOperationException("Finish the protrusion and press Esc to return to Select before continuing.");
            if((int)part.Sketches.Count!=sketchCount)
                throw new InvalidOperationException("A standalone sketch was added. This workflow projects into the revolve profile. Correct this in Solid Edge, or Cancel to preserve your work.");
            if((int)part.Models.Count!=1)throw new InvalidOperationException("Finish the revolve in Solid Edge before continuing.");
            dynamic model=part.Models.Item(1);
            if((int)model.RevolvedProtrusions.Count!=1)throw new InvalidOperationException("Expected one completed revolved protrusion.");
            dynamic feature=model.RevolvedProtrusions.Item(1);
            if((int)feature.Status!=IgFeatureOK || !(bool)model.Body.IsSolid || (double)model.Body.Volume<=0)
                throw new InvalidOperationException("The revolve has not produced a valid solid. Finish or correct it before continuing.");
            if(Math.Abs((double)feature.Angle-2*Math.PI)>1e-7)throw new InvalidOperationException("Set the revolve angle to 360 degrees.");
            Array start=new double[3],direction=new double[3];
            feature.Axis.StartPoint(ref start);feature.Axis.Direction(ref direction);
            double[] p=Vector(start),d=Vector(direction);
            if(!userAxis && (Math.Abs(p[0])>1e-7 || Math.Abs(p[1])>1e-7 || Math.Abs(d[0])>1e-7 || Math.Abs(d[1])>1e-7 || Math.Abs(d[2])<1e-7))
                throw new InvalidOperationException("The revolution axis must coincide with global Z (through the origin in Right view).");
            int n=0;Array profiles=new object[0];
            ((SolidEdge.Part.Interop.RevolvedProtrusion)feature).GetProfiles(out n,ref profiles);
            if(n==0)throw new InvalidOperationException("No completed feature profile was found.");
            foreach(dynamic profile in profiles)
            {
                RequireRightPlane(new[]{0.0,0.0,0.0},new[]{1.0,0.0,0.0},new double[][]{PointOnProfile(profile,0,0),PointOnProfile(profile,1,0),PointOnProfile(profile,0,1)});
                bool projected=false;
                for(int i=1;i<=(int)profile.Relations2d.Count;i++)
                {int type=(int)profile.Relations2d.Item(i).Type;if(type==1650559521 || type==296913277)projected=true;}
                if(!projected) {
                    if(!userAxis)throw new InvalidOperationException("No projection relationship was found in the revolve profile. Use Project to Sketch with the original linked sketch as the source.");
                    // Bushing profiles can contain user-created geometry; Relations2d
                    // alone is not a reliable proof of the projection's associativity.
                    bool linked=false;part.HasInterpartLinks(ref linked);
                    if(!linked)throw new InvalidOperationException("The part no longer has its assembly sketch link.");
                    try{File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SketchToModels-assist.txt"),
                        DateTime.Now.ToString("s")+" Bushing validation warning: no recognized projection relation in the solid profile. Valid solid and part interpart link verified; profile associativity is not verified.\r\n");}catch{}
                }
            }
        }
/* EXPLANATORY NOTE
Require exactly one isolated five-digit number in the assembly filename.
Negative lookarounds prevent extracting five digits from a longer number.
Ambiguous or missing drawing numbers are rejected rather than guessed.
*/
        public static string DrawingNumber(string assembly)
        {
            var matches = Regex.Matches(Path.GetFileNameWithoutExtension(assembly), @"(?<!\d)\d{5}(?!\d)");
            if (matches.Count != 1) throw new InvalidOperationException("The assembly filename must contain exactly one five-digit drawing number.");
            return matches[0].Value;
        }
/* EXPLANATORY NOTE
Produce <sketch name> <drawing number>.par. Reject invalid Windows filename
characters and trailing spaces/dots. Do not silently rename sketches or files.
*/
        public static string PartName(string sketch, string number)
        {
            if (String.IsNullOrWhiteSpace(sketch) || sketch.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || sketch.EndsWith(".") || sketch.EndsWith(" "))
                throw new InvalidOperationException("Rename sketch '" + sketch + "' in Solid Edge: its name cannot be used safely as a filename.");
            return sketch + " " + number + ".par";
        }
/* EXPLANATORY NOTE
Supported 2D assembly sketches are read through Layouts. This is not a recursive
search inside component parts. Assembly 3D sketches are reported but unsupported.
*/
        public static List<SketchItem> Read(dynamic assembly)
        {
            string number = DrawingNumber((string)assembly.FullName);
            var result = new List<SketchItem>();
            dynamic layouts = assembly.Layouts;
            for (int i = 1; i <= (int)layouts.Count; i++)
            {
                dynamic layout = layouts.Item(i);
                string name = layout.Name;
                result.Add(new SketchItem { Source = layout, Name = name, FileName = PartName(name, number) });
            }
            return result;
        }
/* EXPLANATORY NOTE
Find the displayed sketch in a fresh collection. A missing, renamed or duplicate
name requires Refresh instead of trusting an old COM object.
*/
        public static SketchItem ResolveSketch(IEnumerable<SketchItem> current, SketchItem displayed)
        {
            var matches = current.Where(s => String.Equals(s.Name, displayed.Name, StringComparison.Ordinal)).ToList();
            if (matches.Count != 1 || matches[0].FileName != displayed.FileName)
                throw new InvalidOperationException("Sketch '" + displayed.Name + "' changed or is ambiguous. Click Refresh and select it again.");
            return matches[0];
        }
/* EXPLANATORY NOTE
Preflight before CAD changes: validate template, destination and output names.
File existence is not a complete loaded-document check. Solid Edge can retain
an earlier test document even after its on-disk file is removed, causing the
later SaveAs to reject the same name. Inspect unsaved work before closing it.
*/
        public static void Validate(string folder, IEnumerable<SketchItem> items, string template)
        {
            if (!File.Exists(template) || !String.Equals(Path.GetFileName(template), "normal.par", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Choose an accessible normal.par template.");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                string path = Path.Combine(folder, item.FileName);
                if (!seen.Add(path)) throw new InvalidOperationException("Two selected sketches produce the same filename: " + item.FileName);
                if (File.Exists(path) || Directory.Exists(path)) throw new InvalidOperationException("Already exists: " + path + "\nUncheck this sketch or rename it. Existing files are never intentionally replaced.");
                if (path.Length >= 260) throw new InvalidOperationException("The part path is too long: " + path);
            }
        }
/* EXPLANATORY NOTE
ONE PART'S LIFECYCLE AND RECOVERY CHECKPOINTS
This is not an atomic transaction; CAD modifications cannot all be rolled back.
The stage string records the operation responsible for a failure.

Order matters: create from normal.par, save, then CLOSE the new document before
insertion into the assembly. CopySketch targets the inserted occurrence. Keeping
the original editing document open caused copy failures in earlier versions.
Save the linked sketch and assembly before reopening the part for modeling.

Bushing placement creates an identity coordinate system inside the new part
and aligns it to the system the user selects in the assembly. Only that new
occurrence's grounding is adjusted.

After modeling, verify the interpart link, apply an existing color style, and
save the part and assembly. Preserve failed/canceled parts for inspection;
deleting a file may break an assembly reference. Earlier completed parts stay
saved when a later part fails. Do not retry blindly over existing filenames.
*/
        public static void Create(dynamic app, dynamic assembly, SketchItem item, string template)
        {
            string path = Path.Combine(Path.GetDirectoryName((string)assembly.FullName), item.FileName);
            dynamic part = null;
            dynamic occurrence = null;
            bool created = false, canceled = false;
            string stage = "creating the part from normal.par";
            try
            {
                if (File.Exists(path)) throw new IOException("File already exists: " + path);
                part = app.Documents.Add("SolidEdge.PartDocument", template);
                int initialSketchCount = (int)part.Sketches.Count;
                if(item.BushingStyle) { Array identity=new double[]{1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1}; dynamic origin=part.CoordinateSystems.AddByMatrix(ref identity); origin.Name="Bushing placement origin"; }
                stage = "saving the new part before assembly insertion";
                part.SaveAs(path);
                created = true;
                // CopySketch must operate on an assembly-owned target, as in v1/v2.2.
                stage = "closing the new part before assembly insertion";
                part.Close();
                part = null;
                stage = "inserting the part into the assembly";
                assembly.Activate();
                occurrence = assembly.Occurrences.AddByFilename(path);
                Array matrix = new double[] { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 };
                occurrence.PutMatrix(ref matrix, false);
                if(item.BushingStyle) { stage="choosing the bushing coordinate system"; BushingWorkflow.Place(assembly,occurrence); }
                object status = null;
                stage = "copying the linked sketch";
                assembly.CopySketch(item.Source, occurrence, true, ref status);
                part = occurrence.OccurrenceDocument;
                bool linked = false;
                part.HasInterpartLinks(ref linked);
                if (!linked || (int)part.Sketches.Count <= initialSketchCount) throw new InvalidOperationException("Solid Edge did not create a new linked sketch. CopySketch status: " + Convert.ToString(status));
                // Persist the working linked copy even if the solid cannot be generated.
                stage = "saving the linked sketch part";
                part.Save();
                stage = "saving the assembly before revolution";
                assembly.Save();
                // Open an editing document only after CopySketch has completed.
                stage = "opening the linked part for modeling";
                part = OpenPartQuietly(app, path);
                part.Activate();
                if ((bool)part.ReadOnly)
                {
                    bool writable = false;
                    part.SeekWriteAccess(ref writable);
                    if (!writable || (bool)part.ReadOnly)
                        throw new IOException("Solid Edge cannot obtain write access to the new part: " + path);
                }
                stage = "creating the revolved protrusion";
                if(item.BushingStyle) BushingWorkflow.Model(part); else Revolve(part, initialSketchCount + 1);
                linked = false;
                part.HasInterpartLinks(ref linked);
                if (!linked) throw new InvalidOperationException("The revolution did not retain the assembly sketch link.");
                stage = "applying the part color";
                PartColors.Apply(part,item.Name);
                stage = "saving the revolved part";
                SaveWritable(part);
                stage = "saving the assembly after revolution";
                SaveWritable(assembly);
            }
            catch (Exception error)
            {
                canceled = error is OperationCanceledException;
                // Keep failed output for diagnosis; never delete a part that might now be referenced.
                string log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SketchToModels-error.txt");
                string diagnostic = "";
                try { File.WriteAllText(log, DateTime.Now.ToString("s") + "\r\nStage: " + stage + "\r\nPart: " + path + "\r\n" + error.ToString()); diagnostic = "\nError details: " + log; } catch { }
                throw new InvalidOperationException("Stopped while " + stage + ":\n" + error.Message + (created ? "\nA partial part exists at " + path + ". Any open document may contain unsaved work; inspect and save it before closing or retrying." : "") + diagnostic, error);
            }
            finally { if(!canceled)try { assembly.Activate(); } catch { } }
        }
/* EXPLANATORY NOTE
Find the initial template setting using the fallbacks below. template.txt stores
the last successful user selection beside the executable, keeping the selected
network/local template path out of the actual feature-creation logic.
*/
        public static string DefaultTemplate()
        {
            string config = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template.txt");
            if (File.Exists(config)) return File.ReadAllText(config).Trim();
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Siemens\Solid Edge\Version 226\PrefSets\Assembly"))
            {
                if (key != null)
                {
                    string folder = Convert.ToString(key.GetValue("SyncCreateInPlaceTemplateLocation"));
                    if (!String.IsNullOrEmpty(folder)) return Path.Combine(folder, "normal.par");
                }
            }
            return "normal.par";
        }
    }

/* EXPLANATORY NOTE
Coordinate graphical placement and the surface-then-solid workflow. Standard
parts bypass this class and keep default occurrence placement.
*/
    public static class BushingWorkflow
    {
/* EXPLANATORY NOTE
Enumerate coordinate systems by numeric Item index. Item("name") failed with
this collection in testing. Reject a missing or duplicate placement origin.
*/
        public static object FindPlacementOrigin(dynamic systems)
        {
            object found=null;
            for(int i=1;i<=(int)systems.Count;i++) {
                dynamic system=systems.Item(i);
                if(!String.Equals(Convert.ToString(system.Name),"Bushing placement origin",StringComparison.Ordinal))continue;
                if(found!=null)throw new InvalidOperationException("More than one bushing placement origin was found.");
                found=system;
            }
            if(found==null)throw new InvalidOperationException("The new part's bushing placement origin could not be found.");
            return found;
        }
/* EXPLANATORY NOTE
The user selects exactly one coordinate system in Solid Edge. Resolve either
the direct selection or its object wrapper to the coordinate-system interface.
Suppress this new occurrence's active ground relations before adding its
coordinate-system relation, otherwise it may remain at the default position.
Check solved status. On failure, delete the attempted new relation and restore
the ground relations changed by this attempt. Leave unrelated assembly alone.
*/
        public static void Place(dynamic assembly,dynamic occurrence)
        {
            using(var prompt=new Form {Text="Bushing Style - choose coordinate system",ClientSize=new Size(570,205),TopMost=true,StartPosition=FormStartPosition.CenterScreen})
            {
                var note=new Label {Left=16,Top=16,Width=538,Height=65,Text="In the assembly, show Coordinate Systems and select the coordinate system for this part (in the graphics area or PathFinder).\nThen click Use selected coordinate system."};
                var status=new Label {Left=16,Top=128,Width=538,Height=65};
                var use=new Button {Left=16,Top=90,Width=345,Height=30,Text="Use selected coordinate system",ForeColor=Color.ForestGreen};
                var cancel=new Button {Left=375,Top=90,Width=175,Height=30,Text="Cancel batch",DialogResult=DialogResult.Cancel};
                prompt.Controls.AddRange(new Control[]{note,status,use,cancel});prompt.CancelButton=cancel;
                assembly.SelectSet.RemoveAll();
                use.Click+=delegate {
                    string stage="reading the selected coordinate system";
                    try {
                        if(!String.Equals((string)assembly.Application.ActiveDocument.FullName,(string)assembly.FullName,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Activate the assembly tab first.");
                        if((int)assembly.SelectSet.Count!=1)throw new InvalidOperationException("Select exactly one coordinate system.");
                        object target=assembly.SelectSet.Item(1);object raw=target;
                        if(!(raw is SolidEdge.Part.Interop.CoordinateSystem)) {
                            try {raw=((dynamic)target).Object;}catch { }
                        }
                        if(!(raw is SolidEdge.Part.Interop.CoordinateSystem))throw new InvalidOperationException("The selected item is not a coordinate system. Select its entry in PathFinder.");
                        dynamic doc=occurrence.OccurrenceDocument;
                        stage="finding the new part's placement origin";
                        dynamic local=FindPlacementOrigin(doc.CoordinateSystems);
                        stage="creating the occurrence reference for placement";
                        object source=assembly.CreateReference(occurrence,local);
                        var grounds=new List<object>();object relation=null;
                        try {
                            stage="releasing grounding on the new occurrence";
                            for(int i=1;i<=(int)occurrence.Relations3d.Count;i++) {
                                dynamic r=occurrence.Relations3d.Item(i);
                                if((int)r.Type==1959028688 && !(bool)r.Suppress){r.Suppress=true;grounds.Add(r);}
                            }
                            stage="adding the coordinate-system assembly relationship";
                            relation=assembly.Relations3d.AddCoordinateSystem(target,source);
                            stage="checking the coordinate-system assembly relationship";
                            if((int)((dynamic)relation).Status!=1)throw new InvalidOperationException("Solid Edge could not solve the coordinate-system placement.");
                        } catch {
                            if(relation!=null)try{((dynamic)relation).Delete();}catch{}
                            foreach(dynamic r in grounds)try{r.Suppress=false;}catch{}
                            throw;
                        }
                        prompt.DialogResult=DialogResult.OK;prompt.Close();
                    } catch(Exception e){
                        status.Text="Stopped while "+stage+": "+e.Message;
                        try{File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SketchToModels-placement.txt"),DateTime.Now.ToString("s")+"\r\n"+stage+"\r\n"+e+"\r\n");}catch{}
                    }
                };
                if(prompt.ShowDialog()!=DialogResult.OK)throw new OperationCanceledException("Coordinate-system selection canceled.");
            }
        }
/* EXPLANATORY NOTE
ShowDialog keeps a Windows message loop running while the user models in CAD.
OK from the surface prompt starts the solid prompt; OK from the solid prompt
returns to Engine.Create for coloring/saving. Cancel stops the batch and keeps
the partial part, rather than silently continuing to another selected row.
*/
        public static void Model(dynamic part)
        {
            if((int)part.Models.Count!=0)throw new InvalidOperationException("Bushing Style requires a template without a solid.");
            int sketchCount=(int)part.Sketches.Count;
            using(var surface=new BushingSurfacePrompt((object)part))
                if(surface.ShowDialog()!=DialogResult.OK)throw new OperationCanceledException("Bushing surface canceled; part preserved.");
            using(var solid=new ProjectionPrompt((object)part,Engine.FindRightPlane(part),sketchCount,true))
                if(solid.ShowDialog()!=DialogResult.OK)throw new OperationCanceledException("Bushing protrusion canceled; part preserved.");
        }
    }
/* EXPLANATORY NOTE
Two real Solid Edge quirks drove this native dropdown implementation:
1. The Revolved panel may be an owned top-level floating window, outside the
   main window's child tree. Search visible windows of the same process.
2. Create From uses ComboBoxEx32 wrapping an inner ComboBox. Reading the inner
   control yielded truncated text such as "Feat". Read through the wrapper.

Match the complete label instead of hard-coding an option index. Only inspect
visible/enabled controls in the target process. SendMessageTimeout bounds each
message wait. CB_SETCURSEL changes the value; WM_COMMAND notifications tell the
owning panel the selection changed. Read-back verifies the control selection,
not the resulting CAD geometry, which has separate validation.
*/
    public static class NativePlaneChoice
    {
        delegate bool EnumProc(IntPtr hwnd,IntPtr data);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc callback,IntPtr data);
        [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent,EnumProc callback,IntPtr data);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr hwnd,System.Text.StringBuilder name,int count);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] static extern bool IsWindowEnabled(IntPtr hwnd);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint process);
        [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr hwnd);
        [DllImport("user32.dll")] static extern int GetDlgCtrlID(IntPtr hwnd);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr SendMessageTimeout(IntPtr hwnd,uint msg,IntPtr w,IntPtr l,uint flags,uint timeout,out UIntPtr result);
        static long Send(IntPtr hwnd,uint msg,IntPtr w,IntPtr l)
        {
            UIntPtr result;
            if(SendMessageTimeout(hwnd,msg,w,l,2,500,out result)==IntPtr.Zero)throw new TimeoutException("Solid Edge's dropdown did not respond.");
            return unchecked((long)result.ToUInt64());
        }
/* EXPLANATORY NOTE
Allocate and initialize a UTF-16 buffer from the reported length, then release
it in finally. Reject unreasonable lengths. Redirecting to ComboBoxEx32 is
essential: its owner-drawn inner ComboBox does not expose normal label storage.
*/
        static string ItemText(IntPtr hwnd,int index)
        {
            var parentClass=new System.Text.StringBuilder(256);
            IntPtr parent=GetParent(hwnd);GetClassName(parent,parentClass,256);
            // Read through the ComboBoxEx wrapper: the inner owner-drawn combo
            // does not expose normal string storage across process boundaries.
            if(parentClass.ToString().Equals("ComboBoxEx32",StringComparison.OrdinalIgnoreCase))hwnd=parent;
            long length=Send(hwnd,0x149,new IntPtr(index),IntPtr.Zero); // CB_GETLBTEXTLEN
            if(length<0 || length>4096)return "";
            IntPtr memory=Marshal.AllocHGlobal(((int)length+1)*2);
            try{Marshal.Copy(new byte[((int)length+1)*2],0,memory,((int)length+1)*2);long copied=Send(hwnd,0x148,new IntPtr(index),memory);if(copied<0)return "";return Marshal.PtrToStringUni(memory,(int)Math.Min(copied,length))??"";}
            finally{Marshal.FreeHGlobal(memory);}
        }
        public static bool Select(IntPtr root,int processId,Func<bool> canceled)
        {return Select(root,processId,canceled,"Feature's Plane");}
        public static bool Select(IntPtr root,int processId,Func<bool> canceled,string option)
        {
            bool selected=false;var details=new System.Text.StringBuilder();
            var roots=new System.Collections.Generic.List<IntPtr>(); roots.Add(root);
            EnumWindows(delegate(IntPtr window,IntPtr unused){ uint owner;GetWindowThreadProcessId(window,out owner);if(owner==(uint)processId && window!=root && IsWindowVisible(window))roots.Add(window);return !canceled();},IntPtr.Zero);
            foreach(IntPtr window in roots) {
            if(canceled()||selected)break;
            details.AppendLine("Searching window "+window);
            EnumChildWindows(window,delegate(IntPtr hwnd,IntPtr unused){
                if(canceled()||selected)return false;
                uint pid;GetWindowThreadProcessId(hwnd,out pid);
                if(pid!=(uint)processId || !IsWindowVisible(hwnd)||!IsWindowEnabled(hwnd))return true;
                var cls=new System.Text.StringBuilder(256);GetClassName(hwnd,cls,256);
                if(!cls.ToString().Equals("ComboBox",StringComparison.OrdinalIgnoreCase) && !cls.ToString().StartsWith("WindowsForms10.COMBOBOX",StringComparison.OrdinalIgnoreCase))return true;
                try {
                    long count=Send(hwnd,0x146,IntPtr.Zero,IntPtr.Zero); // CB_GETCOUNT
                    if(count<1||count>100)return true;
                    for(int i=0;i<count && !canceled();i++){
                        string label=ItemText(hwnd,i);details.AppendLine(cls+" | "+i+" | "+label);
                        if(!String.Equals(BushingSurfacePrompt.NormalizeOption(label),option,StringComparison.OrdinalIgnoreCase))continue;
                        Send(hwnd,0x14E,new IntPtr(i),IntPtr.Zero); // CB_SETCURSEL
                        if(canceled())return false;
                        IntPtr notificationControl=hwnd;IntPtr parent=GetParent(hwnd);
                        var parentClass=new System.Text.StringBuilder(256);GetClassName(parent,parentClass,256);
                        if(parentClass.ToString().Equals("ComboBoxEx32",StringComparison.OrdinalIgnoreCase)){notificationControl=parent;parent=GetParent(parent);}
                        int id=GetDlgCtrlID(notificationControl);
                        Send(parent,0x111,new IntPtr((1<<16)|(id&0xffff)),notificationControl); // CBN_SELCHANGE
                        if(canceled())return false;
                        Send(parent,0x111,new IntPtr((9<<16)|(id&0xffff)),notificationControl); // CBN_SELENDOK
                        selected=Send(hwnd,0x147,IntPtr.Zero,IntPtr.Zero)==i;
                        return !selected;
                    }
                }catch(Exception e){details.AppendLine(e.Message);}
                return true;
            },IntPtr.Zero);
            }
            try{File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SketchToModels-plane.txt"),DateTime.Now.ToString("s")+" native selection="+selected+"\r\n"+details);}catch{}
            return selected;
        }
    }

/* EXPLANATORY NOTE
Starts Revolved Surface (45037), selects Feature's Plane, waits for the feature
profile, assigns part Z and starts projection. The user picks/accepts the outline,
closes the sketch and finishes the surface. Continue validates, requests Select
(45000) and lets a later timer tick complete the handoff to the solid prompt.
TopMost keeps instructions visible; timers allow normal form interaction.
*/
    public sealed class BushingSurfacePrompt : Form
    {
        readonly dynamic part;
        readonly string path;
        readonly int baseline;
        readonly Label status;
        readonly Timer timer=new Timer {Interval=1100};
        ChainSelectionJob planeJob,projectionJob;
        bool projectionStarted,projectionDone,continueRequested;
        readonly ProfileAxisSetup surfaceAxis=new ProfileAxisSetup();
        int projectionAttempts;
        public BushingSurfacePrompt(object document)
        {
            part=document;path=(string)part.FullName;baseline=(int)part.Constructions.RevolvedSurfaces.Count;
            Text="Bushing Style - revolved surface";ClientSize=new Size(585,395);TopMost=true;StartPosition=FormStartPosition.CenterScreen;
            Controls.Add(new Label {Left=16,Top=16,Width=553,Height=200,Text=
                "1  Create the revolved surface\nThe macro starts Revolved Surface and tries to select Feature's Plane in Create From. If needed, select Feature's Plane yourself.\n\n"+
                "2  Select the copied sketch\nClick the ORIGINAL copied sketch to define the feature plane. Enter the feature sketch. The selected coordinate system's Z centerline is set automatically, then Project to Sketch / Wireframe Chain starts. Pick the outline and accept.\n\n"+
                "3  Close Sketch and Finish\nUse a full 360-degree revolution and click Finish, then click Continue after Finish below. The macro starts Revolved Protrusion on Right (yz)."});
            var next=new Button {Left=16,Top=224,Width=275,Height=30,Text="Continue after Finish",Font=new Font(Font,FontStyle.Bold),ForeColor=Color.ForestGreen};
            var cancel=new Button {Left=310,Top=224,Width=259,Height=30,Text="Cancel batch",DialogResult=DialogResult.Cancel};
            var retry=new Button {Left=16,Top=262,Width=553,Height=28,Text="Set Create From to Feature's Plane"};
            status=new Label {Left=16,Top=302,Width=553,Height=78};Controls.AddRange(new Control[]{next,cancel,retry,status});CancelButton=cancel;
            retry.Click+=delegate {
                try {
                    if(planeJob!=null && !planeJob.IsCompleted){status.Text="The plane selection is still running; no duplicate search was started.";return;}
                    if((int)part.Application.GetActiveCommand()!=45037 || part.ActiveSketch!=null)throw new InvalidOperationException("Return to the Revolved Surface plane-selection step first.");
                    int pid=(int)part.Application.ProcessID;IntPtr hwnd=new IntPtr(Convert.ToInt64(part.Application.hWnd));
                    planeJob=new ChainSelectionJob(stop=>PickFeaturePlane(pid,hwnd,stop));
                    status.Text="Selecting Feature's Plane...";
                }catch(Exception e){status.Text=e.Message;}
            };
            next.Click+=delegate{Check(true);};
            Shown+=delegate {
                try {
                    part.SelectSet.RemoveAll();part.Application.StartCommand(45037);
                    int pid=(int)part.Application.ProcessID;IntPtr hwnd=new IntPtr(Convert.ToInt64(part.Application.hWnd));
                    planeJob=new ChainSelectionJob(stop=>PickFeaturePlane(pid,hwnd,stop));
                    status.Text="Revolved Surface started. Choose the copied sketch for Feature's Plane.";timer.Start();
                }catch(Exception e){status.Text=e.Message;}
            };
            timer.Tick+=delegate {
                if(planeJob!=null && planeJob.IsCompleted) {
                    try {status.Text=planeJob.Result?"Feature's Plane selected. Click the copied sketch.":"Select Feature's Plane in Create From, then click the copied sketch.";}catch(Exception e){status.Text=e.Message;}
                    planeJob=null;
                }
                AssistProjection();
                Check(false);
            };
            FormClosed+=delegate{timer.Stop();timer.Dispose();if(planeJob!=null)planeJob.Cancel();if(projectionJob!=null)projectionJob.Cancel();};
        }
        // Called only on the form's STA timer; native dropdown work runs separately.
/* EXPLANATORY NOTE
State machine: plane selection -> feature profile -> one projection launch ->
background Wireframe Chain selection. Keep CAD COM access on the form's STA
thread. The worker handles native controls. Do not recreate axes on every tick.
*/
        void AssistProjection()
        {
            try {
                if(planeJob!=null || projectionDone)return;
                if(!String.Equals((string)part.Application.ActiveDocument.FullName,path,StringComparison.OrdinalIgnoreCase))return;
                if(!projectionStarted) {
                    if(!StartSurfaceProjection())return;
                    status.Text="Project to Sketch started. Selecting Wireframe Chain...";
                }
                if(projectionJob!=null) {
                    if(!projectionJob.IsCompleted)return;
                    bool selected=projectionJob.Result;projectionJob=null;
                    if(selected){projectionDone=true;status.Text="Wireframe Chain selected. Pick the original outline and accept. The Z centerline is already set. Close Sketch and Finish.";return;}
                }
                if(part.ActiveSketch==null || (int)part.Application.GetActiveCommand()!=50011)return;
                if(++projectionAttempts>8){projectionDone=true;status.Text="Project to Sketch is active. Select Wireframe Chain, pick the outline, and accept. The Z centerline is already set.";return;}
                int pid=(int)part.Application.ProcessID;IntPtr hwnd=new IntPtr(Convert.ToInt64(part.Application.hWnd));
                projectionJob=new ChainSelectionJob(stop=>NativePlaneChoice.Select(hwnd,pid,stop,"Wireframe Chain"));
            }catch(Exception e){projectionDone=true;status.Text="Projection assistance: "+e.Message;}
        }
/* EXPLANATORY NOTE
ActiveSketch must be a Profile owned by ProfileSet, not the original linked
standalone Sketch. Create/verify the axis before starting projection so an
incompatible plane fails without launching the next command.
*/
        bool StartSurfaceProjection()
        {
            if(projectionStarted)return false;
            dynamic active=part.ActiveSketch;
            if(active==null)return false;
            // Never start projection while the original linked Sketch is being edited.
            if((int)active.Type!=1584866912 || (int)active.Parent.Type!=-1521484909)return false;
            surfaceAxis.Ensure((object)active);
            part.Application.StartCommand(50011);
            projectionStarted=true;
            return true;
        }
/* EXPLANATORY NOTE
Only accept an existing healthy full-revolution surface with the sketch closed.
A finished command can remain ready to create another surface. Continue exits
it only after validation, then waits for Select before opening the next prompt.
*/
        void Check(bool requested)
        {
            try {
                if(!String.Equals((string)part.Application.ActiveDocument.FullName,path,StringComparison.OrdinalIgnoreCase)){if(requested)status.Text="Return to the bushing part tab.";return;}
                if(part.ActiveSketch!=null || (int)part.Constructions.RevolvedSurfaces.Count<=baseline){if(requested)status.Text="Finish the revolved surface first.";return;}
                if((int)part.Application.GetActiveCommand()!=45000 && !requested && !continueRequested){status.Text="Click Finish in Solid Edge, then Continue after Finish here to start the revolved protrusion.";return;}
                var surface=(SolidEdge.Part.Interop.RevolvedSurface)part.Constructions.RevolvedSurfaces.Item(baseline+1);
                object description=null;
                if((int)surface.GetStatusEx(out description)!=1216476310)throw new InvalidOperationException("The surface is not valid yet: "+Convert.ToString(description));
                SolidEdge.Part.Interop.FeaturePropertyConstants extent,side;double angle;
                surface.GetDirection1Extent(out extent,out side,out angle);
                if(extent!=SolidEdge.Part.Interop.FeaturePropertyConstants.igThreeHundredAndSixty && Math.Abs(angle-2*Math.PI)>1e-7)throw new InvalidOperationException("Set the revolved surface to a full 360 degrees and Finish.");
                if((int)part.Application.GetActiveCommand()!=45000) {
                    // A completed surface can leave Revolved Surface ready to make another.
                    // Only dismiss it after validating the completed feature above.
                    continueRequested=true;
                    if(planeJob!=null)planeJob.Cancel();
                    if(projectionJob!=null)projectionJob.Cancel();
                    projectionDone=true;
                    part.Application.StartCommand(45000);
                    status.Text="Surface complete. Starting the revolved protrusion...";
                    return; // Let Solid Edge finish the command change before opening the solid prompt.
                }
                if(planeJob!=null)planeJob.Cancel();timer.Stop();DialogResult=DialogResult.OK;Close();
            }catch(Exception e){status.Text=e.Message;}
        }
        public static string NormalizeOption(string text)
        {return (text??"").Replace("&","").Replace("’","'").Trim();}
        static string ComboValue(AutomationElement box)
        {
            object p;
            if(box.TryGetCurrentPattern(ValuePattern.Pattern,out p))return NormalizeOption(((ValuePattern)p).Current.Value);
            if(box.TryGetCurrentPattern(SelectionPattern.Pattern,out p)) {
                var selected=((SelectionPattern)p).Current.GetSelection();
                if(selected.Length==1)return NormalizeOption(selected[0].Current.Name);
            }
            return "";
        }
        static bool PickFeaturePlane(int pid,IntPtr hwnd,Func<bool> stop)
        {
            for(int nativeAttempt=0;nativeAttempt<8 && !stop();nativeAttempt++) { if(NativePlaneChoice.Select(hwnd,pid,stop))return true; System.Threading.Thread.Sleep(250); }
            return false;
        }
    }
/* EXPLANATORY NOTE
Idempotent axis creation: remember profile, line, fixing relation and axis so a
retry does not duplicate them. Reject a changed profile. Fixing the line holds
the intended centerline; verify the resulting axis position/direction in 3D.
*/
    public sealed class ProfileAxisSetup
    {
        object profileObject, lineObject, axisObject;
        bool fixedLine;
        public void Ensure(object profile)
        {
            if(profileObject!=null && !Object.ReferenceEquals(profileObject,profile))
                throw new InvalidOperationException("The revolve profile changed. Cancel and restart this part's assisted step.");
            dynamic p=profile;
            profileObject=profile;
            if(lineObject==null)
            {
                double[] coordinates=Engine.ZAxisCoordinates(p);
                lineObject=p.Lines2d.AddBy2Points(coordinates[0],coordinates[1],coordinates[2],coordinates[3]);
            }
            if(!fixedLine){p.Relations2d.AddFix(lineObject);fixedLine=true;}
            if(axisObject==null)axisObject=p.SetAxisOfRevolution(lineObject);
            if(axisObject==null)throw new InvalidOperationException("Solid Edge did not return the profile's revolution axis.");
            dynamic axis=axisObject;
            Array start=new double[3],direction=new double[3];
            axis.StartPoint(ref start);axis.Direction(ref direction);
            int s=start.GetLowerBound(0),d=direction.GetLowerBound(0);
            if(Math.Abs(Convert.ToDouble(start.GetValue(s)))>1e-7 || Math.Abs(Convert.ToDouble(start.GetValue(s+1)))>1e-7 ||
                Math.Abs(Convert.ToDouble(direction.GetValue(d)))>1e-7 || Math.Abs(Convert.ToDouble(direction.GetValue(d+1)))>1e-7 ||
                Math.Abs(Convert.ToDouble(direction.GetValue(d+2)))<1e-7)
                throw new InvalidOperationException("The profile axis does not coincide with global Z. Project to Sketch has not been started.");
        }
    }
/* EXPLANATORY NOTE
Run slow UI selection in a Task so the instruction window does not freeze.
Cancellation is cooperative; it cannot interrupt a UI Automation provider call
already blocked internally. Continue must not wait indefinitely for Result.
*/
    public sealed class ChainSelectionJob
    {
        volatile bool canceled;
        readonly System.Threading.Tasks.Task<bool> task;
        public readonly DateTime Started = DateTime.UtcNow;
        public ChainSelectionJob(Func<Func<bool>,bool> select)
        {
            // UI Automation must not run on the WinForms STA. In particular,
            // FindAll/Expand can stall while the CAD command is changing panels.
            task=System.Threading.Tasks.Task.Factory.StartNew(()=>select(()=>canceled),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskCreationOptions.None,
                System.Threading.Tasks.TaskScheduler.Default);
        }
        public bool IsCompleted {get{return task.IsCompleted;}}
        public bool Result {get {if(!task.IsCompleted)throw new InvalidOperationException("Selection is still running.");if(task.IsFaulted)throw task.Exception.GetBaseException();return task.Result;}}
        public void Cancel(){canceled=true;}
    }
/* EXPLANATORY NOTE
Shared solid-revolve assistant. manualAxis=false creates part Z for standard
parts; true lets the user select the bushing solid's axis. This differs from
the bushing surface stage, whose centerline is automatic.

Continue cancels selection assistance and validates the feature. For bushings,
it exits a still-active command and waits for Select before final validation.
OK closes the form and hands control back to Engine.Create to save/advance.

The independent completion timer can notice a finished bushing automatically.
Once Continue is clicked, manualContinueClicked prevents that timer overwriting
errors. Previously a real validation error was replaced with routine instructions,
making the button appear to do nothing.
*/
    public sealed class ProjectionPrompt : Form
    {
        readonly dynamic part,right;
        readonly int initialCount,initialSets;
        readonly string path;
        readonly Label message;
        readonly Timer timer=new Timer {Interval=800};
        bool started;int attempts;
        ChainSelectionJob selectionJob;
        bool manualContinueClicked;
        readonly ProfileAxisSetup axisSetup=new ProfileAxisSetup();
        readonly bool userAxis;
        public ProjectionPrompt(object document,object plane,int count) : this(document,plane,count,false) {}
        public ProjectionPrompt(object document,object plane,int count, bool manualAxis)
        {
            userAxis=manualAxis;
            part=document;right=plane;initialCount=count;initialSets=(int)part.ProfileSets.Count;path=(string)part.FullName;
            Text="Revolve original sketch - "+Path.GetFileName(path);
            ClientSize=new Size(570,590);TopMost=true;ShowInTaskbar=true;
            FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
            Controls.Add(new Label {Left=16,Top=12,Width=538,Height=36,Text="Use the ORIGINAL linked sketch as the source.\nProject into the revolve profile; do not create a separate Sketch."});
            string[] titles={"","2  Pick the sketch to revolve","3  Close Sketch","4  Continue"};
            string[] words={
                "The macro starts Revolve. Use Coincident Plane and choose Right (yz). Enter Draw Profile if needed.",
                "The macro defines the Z centerline first, then starts Project to Sketch. Pick the ORIGINAL outline using Wireframe Chain and accept. Keep projection associative.",
                "Close the profile, set 360 degrees, then click Finish in Solid Edge. Z is predefined. If Solid Edge still asks for an axis, select the centerline the macro added.",
                "Once the finished solid appears, click Continue below. The macro saves this part and continues with the next selected sketch."};
            if(userAxis) { words[1]="Project to Sketch starts automatically. Pick the outline you want to revolve and accept the projection. Sketch or select your centerline."; words[2]="Close Sketch, select your revolve axis, set 360 degrees, and click Finish. Click Continue here after Finish to save and proceed."; words[3]="After Finish, click Continue to save this part, close this window, and proceed to the next selected part."; }
            for(int i=0;i<4;i++)
            {
                if(i==0) { Controls.Add(new Label {Left=16,Top=73,Width=538,Height=35,Text="You are sketching on the Right plate",Font=new Font(Font,FontStyle.Bold)}); continue; }
                int step=i;
                var panel=new Panel {Left=16,Top=58+i*105,Width=96,Height=92,BackColor=Color.White};
                panel.Paint += delegate(object sender,PaintEventArgs e){DrawStep(e.Graphics,step);};Controls.Add(panel);
                Controls.Add(new Label {Left=124,Top=58+i*105,Width=430,Height=23,Text=titles[i],Font=new Font(Font,FontStyle.Bold)});
                Controls.Add(new Label {Left=124,Top=83+i*105,Width=426,Height=72,Text=words[i]});
            }
            var project=new Button {Left=16,Top=487,Width=180,Height=28,Text="Retry Project to Sketch"};
            var next=new Button {Left=206,Top=487,Width=170,Height=28,Text="Continue",Font=new Font(Font,FontStyle.Bold),ForeColor=Color.ForestGreen};
            var cancel=new Button {Left=386,Top=487,Width=168,Height=28,Text="Cancel batch",DialogResult=DialogResult.Cancel};
            Controls.AddRange(new Control[]{project,next,cancel});CancelButton=cancel;
            message=new Label {Left=16,Top=525,Width=538,Height=58};Controls.Add(message);
            project.Click+=delegate {Try(delegate {RequireSelectionIdle();StartProjection();timer.Start();});};
            var advanceTimer=new Timer {Interval=250};
            int advanceTicks=0;
            advanceTimer.Tick+=delegate {
                try {
                    CheckDocument();
                    if((int)part.Application.GetActiveCommand()!=45000) {
                        if(++advanceTicks<40)return;
                        throw new InvalidOperationException("Solid Edge is still finishing its command. Click Finish, then Continue again.");
                    }
                    advanceTimer.Stop();
                    Engine.ValidateManualRevolve(part,initialCount,userAxis);
                    DialogResult=DialogResult.OK;Close();
                }catch(Exception e){advanceTimer.Stop();message.Text=e.Message;Log();}
            };
            FormClosed+=delegate{advanceTimer.Stop();advanceTimer.Dispose();};
            next.Click+=delegate {
                manualContinueClicked=true;
                // Projection assistance is no longer needed after the user finishes
                // the feature. Never gate validation/save on an unresponsive UIA provider.
                timer.Stop();
                if(selectionJob!=null)selectionJob.Cancel();
                message.Text="Checking the completed revolve...";message.Refresh();Log();
                Try(delegate {
                    CheckDocument();
                    if(userAxis) {
                        if(part.ActiveSketch!=null)throw new InvalidOperationException("Close Sketch and Finish the protrusion before clicking Continue.");
                        // Validate the finished solid before leaving its still-active command.
                        Engine.ValidateManualRevolve(part,initialCount,true,false);
                        part.Application.StartCommand(45000);
                        advanceTicks=0;advanceTimer.Start();
                        message.Text="Finishing this part and continuing...";
                    } else {
                        Engine.ValidateManualRevolve(part,initialCount,false);
                        DialogResult=DialogResult.OK;Close();
                    }
                });
            };
            Shown+=delegate {Try(delegate {CheckDocument();part.SelectSet.RemoveAll();part.SelectSet.Add(right);part.Application.StartCommand(45018);message.Text="Choose Right (yz) in the Revolve command. The original sketch is your projection source.";timer.Start();});};
            timer.Tick+=delegate {
                try {
                    // While the worker is inside UI Automation, make no more CAD
                    // calls from the UI thread. Let this window process input.
                    if(selectionJob!=null && !selectionJob.IsCompleted) {
                        if((DateTime.UtcNow-selectionJob.Started).TotalSeconds>6)
                            message.Text="Solid Edge's selection control is taking longer than expected. You can move this window or cancel the batch. No second search will be started.";
                        return;
                    }
                    CheckDocument();
                    if(!started) {if(!InFeatureProfile())return;StartProjection();}
                    else {
                        bool selected=false;
                        if(selectionJob!=null) {
                            var completed=selectionJob;selectionJob=null;selected=completed.Result;
                        } else if((int)part.Application.GetActiveCommand()==50011) {
                            int pid=(int)part.Application.ProcessID;IntPtr hwnd=new IntPtr(Convert.ToInt64(part.Application.hWnd));
                            selectionJob=new ChainSelectionJob(stop=>SelectWireframeChain(pid,hwnd,stop));
                            return;
                        }
                        if(selected)
                        {message.Text="Project to Sketch: Wireframe Chain selected. Click the original outline and accept.";timer.Stop();}
                        else if(++attempts>=12){message.Text="Automatic selection did not complete. Close any Project Options dialog and click Retry, or select Project to Sketch / Wireframe Chain in Solid Edge.";timer.Stop();}
                    }
                }catch(Exception e){message.Text=e.Message;timer.Stop();}
                finally{Log();}
            };
            FormClosed+=delegate{timer.Stop();timer.Dispose();if(selectionJob!=null)selectionJob.Cancel();};
            if(userAxis) {
                var completion=new Timer {Interval=1200};
                completion.Tick+=delegate {
                    if(manualContinueClicked)return;
                    try {
                        CheckDocument();
                        if(part.ActiveSketch!=null || (int)part.Models.Count==0)return;
                        if((int)part.Application.GetActiveCommand()!=45000){message.Text="Click Finish, then Continue here. The macro will save this part and proceed to the next selection.";return;}
                        if((int)part.Models.Item(1).RevolvedProtrusions.Count==0)return;
                        Engine.ValidateManualRevolve(part,initialCount,true);
                        timer.Stop();if(selectionJob!=null)selectionJob.Cancel();
                        DialogResult=DialogResult.OK;Close();
                    } catch(Exception e) {message.Text=e.Message;Log();}
                };
                Shown+=delegate{completion.Start();};
                FormClosed+=delegate{completion.Stop();completion.Dispose();};
            }
        }
        void RequireSelectionIdle()
        {
            if(selectionJob!=null && !selectionJob.IsCompleted)
                throw new InvalidOperationException("The automatic selection search is still waiting for Solid Edge. Cancel batch remains available; please wait before retrying or saving.");
            if(selectionJob!=null){selectionJob.Cancel();selectionJob=null;}
        }
/* EXPLANATORY NOTE
Require a new ProfileSet and a Profile as ActiveSketch while the standalone
Sketches.Count stays unchanged. Avoid editing the linked source or creating
another standalone sketch instead of the feature's internal profile.
*/
        bool InFeatureProfile()
        {
            if((int)part.Sketches.Count!=initialCount)return false;
            if((int)part.ProfileSets.Count<=initialSets || part.ActiveSketch==null)return false;
            dynamic active=part.ActiveSketch;
            // A feature profile belongs to a ProfileSet; a linked source belongs to a Sketch.
            return (int)active.Type==1584866912 && (int)active.Parent.Type==-1521484909;
        }
        void StartProjection()
        {
            CheckDocument();
            if(!InFeatureProfile())throw new InvalidOperationException("Enter the revolve's Draw Profile step on Right (yz) first. Do not edit the original linked sketch.");
            message.Text=userAxis?"Starting projection; you will choose the revolve axis.":"Defining the Z-axis centerline in the revolve profile...";message.Refresh();Log();
            if(!userAxis) axisSetup.Ensure((object)part.ActiveSketch);
            part.Application.StartCommand(50011);started=true;attempts=0;
            message.Text=userAxis?"Starting Project to Sketch and selecting Wireframe Chain...":"Z-axis centerline defined. Starting Project to Sketch and selecting Wireframe Chain...";
        }
        void CheckDocument(){if(!String.Equals((string)part.Application.ActiveDocument.FullName,path,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Return to the part tab: "+Path.GetFileName(path));}
        void Try(Action action){try{action();}catch(Exception e){message.Text=e.Message;}Log();}
        string previous;
        void Log(){if(message.Text==previous)return;previous=message.Text;try{File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SketchToModels-assist.txt"),DateTime.Now.ToString("s")+" "+path+"\r\n"+message.Text+"\r\n");}catch{}}
        public static void DrawStep(Graphics g,int step)
        {
            g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using(var blue=new Pen(Color.SteelBlue,2))using(var green=new Pen(Color.ForestGreen,3))using(var gray=new Pen(Color.Gray,1))using(var font=new Font("Segoe UI",9))
            {
                if(step==0){g.DrawPolygon(blue,new[]{new Point(18,24),new Point(76,13),new Point(76,65),new Point(18,76)});g.DrawString("Right (yz)",font,Brushes.Black,17,39);}
                if(step==1){Point[] outline={new Point(16,66),new Point(16,40),new Point(42,40),new Point(42,24),new Point(76,24),new Point(76,66)};g.DrawPolygon(green,outline);g.DrawLine(blue,8,80,27,61);g.DrawLine(blue,27,61,19,63);g.DrawString("Original",font,Brushes.Black,21,3);}
                if(step==2){g.DrawLine(blue,26,78,26,16);g.DrawLine(blue,26,16,22,24);g.DrawString("Z",font,Brushes.Black,13,3);g.DrawArc(green,37,27,42,38,25,300);g.DrawString("360°",font,Brushes.Black,38,68);g.FillEllipse(Brushes.Black,23,75,6,6);}
                if(step==3){g.DrawEllipse(blue,14,21,66,27);g.DrawEllipse(blue,29,27,35,13);g.DrawArc(blue,14,40,66,27,0,180);g.DrawLine(blue,14,34,14,54);g.DrawLine(blue,80,34,80,54);g.DrawLines(green,new[]{new Point(34,76),new Point(43,84),new Point(63,67)});}
            }
        }
/* EXPLANATORY NOTE
Bounded native selection shared by the solid and surface prompts. Each message
uses a timeout, and cancellation is checked between controls. This avoids retaining
an indefinitely blocked UI Automation worker after closing the instruction form.
*/
        static bool SelectWireframeChain(int processId, IntPtr handle, Func<bool> canceled)
        {
            // Use the same bounded native path as the surface workflow. No CAD
            // COM references are captured by the worker, only process/window IDs.
            return NativePlaneChoice.Select(handle,processId,canceled,"Wireframe Chain");
        }


    }
/* EXPLANATORY NOTE
Apply Part Painter-style appearance via the style API after modeling, before
saving. Reuse existing named face styles ONLY: the user explicitly prohibited
creating or modifying color styles. Missing styles are skipped.

Match contained text ignoring case, spaces, hyphens and underscores. collet1 and
diaphragms match Yellow; part stop and partstop match Green. First rule wins if
multiple names match. Unmatched parts keep their appearance. Base/body/feature
styles control display appearance; they do not set the engineering material.
*/
    public static class PartColors
    {
        public static string StyleForName(string name)
        {
            string key=Regex.Replace((name??"").ToLowerInvariant(),@"[\s_-]+","");
            if(key.Contains("collet") || key.Contains("diaphragm"))return "Yellow";
            if(key.Contains("partstop"))return "Green";
            if(key.Contains("adapter"))return "Khaki";
            if(key.Contains("puller"))return "Orange";
            if(key.Contains("body"))return "Bronze";
            // Match singular and plural insert names using the same substring rule.
            // Apply() still skips a missing Blue style; no style is ever created.
            if(key.Contains("insert"))return "Blue";
            return null;
        }
        public static void Apply(dynamic part,string name)
        {
            string wanted=StyleForName(name);
            if(wanted==null)return;
            dynamic styles=part.FaceStyles;
            dynamic chosen=null;
            for(int i=1;i<=(int)styles.Count;i++) {
                dynamic candidate=styles.Item(i);
                if(String.Equals(Convert.ToString(candidate.StyleName),wanted,StringComparison.OrdinalIgnoreCase)){chosen=candidate;break;}
            }
            if(chosen==null) {
                try{File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SketchToModels-assist.txt"),
                    DateTime.Now.ToString("s")+" Part color skipped: "+name+"; existing style not found: "+wanted+"\r\n");}catch{}
                return;
            }
            part.SetBaseStyle(SolidEdge.Part.Interop.PartBaseStylesConstants.sePartBaseStyle,chosen);
            for(int i=1;i<=(int)part.Models.Count;i++) {
                dynamic model=part.Models.Item(i);
                model.Body.Style=chosen;
                for(int j=1;j<=(int)model.RevolvedProtrusions.Count;j++)
                    model.RevolvedProtrusions.Item(j).SetStyle(chosen);
            }
            try{File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SketchToModels-assist.txt"),
                DateTime.Now.ToString("s")+" Part Painter style: "+name+" -> "+wanted+"\r\n");}catch{}
        }
    }

/* EXPLANATORY NOTE
Main grid and batch controller. Either checkbox selects a row; Bushing Style
wins if both are checked. Require a saved active assembly outside in-place edit.
Disable this form during a batch to prevent competing processing requests.
*/
    public sealed class MainWindow : Form
    {
        bool comReferencesReleased;
        readonly DataGridView grid = new DataGridView();
        readonly TextBox template = new TextBox();
        readonly Label assemblyLabel = new Label(), status = new Label();
        readonly Button process = new Button(), refresh = new Button();
        dynamic app, assembly;
        string assemblyPath;
        public MainWindow()
        {
            Text = "Sketch to Models v2.31 | Solid Edge 2026";
            Size = new Size(840, 580); MinimumSize = new Size(700, 480);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 6 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            Controls.Add(layout);
            assemblyLabel.Dock = DockStyle.Fill; assemblyLabel.AutoEllipsis = true;
            layout.Controls.Add(assemblyLabel);
            var templateRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            templateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); templateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            templateRow.Controls.Add(new Label { Text = "Part template (normal.par)", AutoSize = true }, 0, 0);
            template.Text = Engine.DefaultTemplate(); template.Dock = DockStyle.Fill;
            templateRow.Controls.Add(template, 0, 1);
            var browse = new Button { Text = "Browse...", Dock = DockStyle.Fill };
            browse.Click += delegate { using (var dlg = new OpenFileDialog { Filter = "Normal part template (normal.par)|normal.par", CheckFileExists = true }) { if (dlg.ShowDialog(this) == DialogResult.OK) template.Text = dlg.FileName; } };
            templateRow.Controls.Add(browse, 1, 1); layout.Controls.Add(templateRow);
            grid.Dock = DockStyle.Fill; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false;
            grid.RowHeadersVisible = false; grid.BackgroundColor = Color.White; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Select", HeaderText = "Select", FillWeight = 18 });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name="Bushing",HeaderText="Bushing Style",FillWeight=28 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sketch", HeaderText = "Assembly sketch", ReadOnly = true, FillWeight = 65 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Part", HeaderText = "New part filename", ReadOnly = true, FillWeight = 100 });
            layout.Controls.Add(grid);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
            foreach (bool select in new[] { true, false })
            {
                bool value = select;
                var button = new Button { Text = select ? "Select all" : "Clear", AutoSize = true };
                button.Click += delegate { foreach (DataGridViewRow row in grid.Rows) { row.Cells[0].Value = value; if(!value)row.Cells[1].Value=false; } };
                actions.Controls.Add(button);
            }
            refresh.Text = "Refresh"; refresh.AutoSize = true; refresh.Click += delegate { LoadAssembly(); }; actions.Controls.Add(refresh);
            layout.Controls.Add(actions);
            layout.Controls.Add(new Label { Text = "Select = standard workflow. Bushing Style = coordinate placement, surface, then solid; overrides Select.", Dock = DockStyle.Fill });
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            status.Dock = DockStyle.Fill; status.AutoEllipsis = true; footer.Controls.Add(status);
            process.Text = "Process"; process.Dock = DockStyle.Fill; process.Click += delegate { ProcessSelection(); }; footer.Controls.Add(process);
            layout.Controls.Add(footer);
            Shown += delegate { LoadAssembly(); };
        }
        void LoadAssembly()
        {
            grid.Rows.Clear(); process.Enabled = false;
            try
            {
                app = ComSupport.ConnectRunning();
                assembly = app.ActiveDocument;
                assemblyPath = (string)assembly.FullName;
                if (!String.Equals(Path.GetExtension(assemblyPath), ".asm", StringComparison.OrdinalIgnoreCase) || !File.Exists(assemblyPath))
                    throw new InvalidOperationException("Open and save an assembly (.asm) in Solid Edge first.");
                if ((bool)assembly.InPlaceActivated) throw new InvalidOperationException("Finish editing the in-place component and return to the assembly first.");
                var items = Engine.Read(assembly);
                foreach (var item in items) { int n = grid.Rows.Add(false, false, item.Name, item.FileName); grid.Rows[n].Tag = item; }
                assemblyLabel.Text = "Assembly: " + Path.GetFileName(assemblyPath) + "\nSave folder: " + Path.GetDirectoryName(assemblyPath);
                int threeD = (int)assembly.Sketches3D.Count;
                status.Text = items.Count + " assembly sketches" + (threeD > 0 ? ". " + threeD + " 3D sketches are not supported by this version." : ". Select sketches to process.");
                process.Enabled = items.Count > 0;
            }
            catch (Exception e) { status.Text = "Open an assembly, then click Refresh."; MessageBox.Show(this, e.Message, "Unable to load sketches", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
/* EXPLANATORY NOTE
Commit checkbox edits, preflight the selected rows, then process in grid order.
Reconnect and resolve sketches again for each part because interactive work
changes active documents. Mark a row complete only after Engine.Create returns,
including both saves. An exception stops the batch; later rows do not run.
*/
        void ProcessSelection()
        {
            grid.EndEdit();
            var selected = grid.Rows.Cast<DataGridViewRow>().Where(r => (Convert.ToBoolean(r.Cells[0].Value) || Convert.ToBoolean(r.Cells[1].Value))).ToList();
            if (selected.Count == 0) { status.Text = "Select at least one sketch."; return; }
            try
            {
                ReconnectAssembly();
                List<SketchItem> current = Engine.Read(assembly);
                foreach (var row in selected) row.Tag = Engine.ResolveSketch(current, (SketchItem)row.Tag);
                Engine.Validate(Path.GetDirectoryName(assemblyPath), selected.Select(r => (SketchItem)r.Tag), template.Text.Trim());
                Enabled = false; UseWaitCursor = true;
                assembly.Save();
                int count = 0;
                foreach (var row in selected)
                {
                    ReconnectAssembly();
                    current = Engine.Read(assembly);
                    var item = Engine.ResolveSketch(current, (SketchItem)row.Tag);
                    item.BushingStyle=Convert.ToBoolean(row.Cells[1].Value);
                    status.Text = "Creating " + item.FileName + "..."; status.Refresh();
                    Engine.Create(app, assembly, item, template.Text.Trim());
                    item.Source=null;
                    ((SketchItem)row.Tag).Source=null;
                    row.Cells[0].Value = false; row.Cells[1].Value=false; row.DefaultCellStyle.BackColor = Color.Honeydew;
                    count++;
                }
                try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template.txt"), template.Text.Trim()); } catch { }
                status.Text = "Created and saved " + count + " linked, revolved parts.";
                MessageBox.Show(this, status.Text + "\n" + Path.GetDirectoryName(assemblyPath), "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                // Preflight errors previously bypassed the detailed error log entirely.
                try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SketchToModels-error.txt"), "\r\n" + DateTime.Now.ToString("s") + " v2.31 ProcessSelection\r\n" + e.ToString() + "\r\n"); } catch { }
                status.Text = "Stopped: " + e.Message;
                MessageBox.Show(this, e.Message + "\n\nPreviously completed parts remain saved. Existing filenames are blocked on retry.", "Processing stopped", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { Enabled = true; UseWaitCursor = false; }
        }
        /// <summary>
        /// Release the form's retained COM objects only on final disposal, after
        /// modal modeling prompts have returned. Do not force-release shared
        /// profile/feature aliases while CAD work is active.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if(disposing && !comReferencesReleased) {
                comReferencesReleased=true;
                var released=new List<object>();
                foreach(DataGridViewRow row in grid.Rows) {
                    var item=row.Tag as SketchItem;
                    if(item==null)continue;
                    object source=item.Source;item.Source=null;row.Tag=null;
                    if(source!=null && !released.Any(x=>Object.ReferenceEquals(x,source))) {
                        released.Add(source);
                        ComSupport.ReleaseOwned(ref source);
                    }
                }
                object document=assembly;assembly=null;
                ComSupport.ReleaseOwned(ref document);
                object application=app;app=null;
                ComSupport.ReleaseOwned(ref application);
            }
            base.Dispose(disposing);
        }
        void ReconnectAssembly()
        {
            // Never dereference the document or sketches retained by the selection window.
            // Closing/reopening the assembly invalidates those COM objects, even at the same path.
            app = ComSupport.ConnectRunning();
            dynamic active = app.ActiveDocument;
            if (!String.Equals((string)active.FullName, assemblyPath, StringComparison.OrdinalIgnoreCase) || (bool)active.InPlaceActivated)
                throw new InvalidOperationException("Return to the listed assembly or click Refresh before processing.");
            assembly = active;
        }
    }

    [ComImport, Guid("00000016-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMessageFilter
    {
        [PreserveSig] int HandleInComingCall(int type, IntPtr caller, int ticks, IntPtr info);
        [PreserveSig] int RetryRejectedCall(IntPtr callee, int ticks, int type);
        [PreserveSig] int MessagePending(IntPtr callee, int ticks, int type);
    }
/* EXPLANATORY NOTE
COM busy-call handling: retry the recognized retry-later rejection after 150 ms
for at most 15 seconds. Other cases fail instead of looping forever. This is
not a blanket retry of feature creation after an ambiguous error, which could
duplicate geometry. Program installs this filter on its COM/UI thread.
*/
    sealed class BusyFilter : IMessageFilter
    {
        public int HandleInComingCall(int type, IntPtr caller, int ticks, IntPtr info) { return 0; }
        public int RetryRejectedCall(IntPtr callee, int ticks, int type) { return type == 2 && ticks < 15000 ? 150 : -1; }
        public int MessagePending(IntPtr callee, int ticks, int type) { return 2; }
        [DllImport("ole32.dll")] internal static extern int CoRegisterMessageFilter(IMessageFilter filter, out IMessageFilter previous);
    }
/* EXPLANATORY NOTE
STAThread supports Windows Forms and COM apartment requirements. A local named
mutex prevents simultaneous macro instances racing over Solid Edge's active
document. Restore the previous COM message filter when the program exits.
*/
    static class Program
    {
        [STAThread] static void Main()
        {
            bool first;
            using (var mutex = new System.Threading.Mutex(true, "Local\\SolidEdgeSketchToModels", out first))
            {
                if (!first) { MessageBox.Show("Sketch to Models is already open."); return; }
                try {
                    using(var filter=new MessageFilterScope()) {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        using(var window=new MainWindow())Application.Run(window);
                    }
                } catch(Exception error) {
                    MessageBox.Show(error.Message,"Sketch to Models could not continue",MessageBoxButtons.OK,MessageBoxIcon.Error);
                }
            }
        }
    }
}
































