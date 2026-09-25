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

//test

namespace SketchToModels
{
    public sealed class SketchItem
    {
        public object Source;
        public string Name, FileName;
        public bool BushingStyle;
    }

    public static class Engine
    {
        // Solid Edge 2026 enum values, verified against the installed type library.
        const int IgRight = 2, IgProfileClosed = 1, IgFeatureOK = 1216476310;
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

        public static double[] PointOnProfile(dynamic profile, double u, double v)
        {
            double x = 0, y = 0, z = 0;
            profile.Convert2DCoordinate(u, v, ref x, ref y, ref z);
            return new[] { x, y, z };
        }
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
        public static void Revolve(dynamic part, int firstNewSketch)
        {
            if ((int)part.Models.Count != 0) throw new InvalidOperationException("The template already contains a model.");
            using(var prompt=new ProjectionPrompt((object)part,FindRightPlane(part),(int)part.Sketches.Count))
                if(prompt.ShowDialog()!=DialogResult.OK)
                    throw new OperationCanceledException("Revolve canceled. The linked part is preserved; the batch has stopped.");
        }
        public static void ValidateManualRevolve(dynamic part, int sketchCount) {ValidateManualRevolve(part,sketchCount,false);}
        public static void ValidateManualRevolve(dynamic part, int sketchCount, bool userAxis) {ValidateManualRevolve(part,sketchCount,userAxis,true);}
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
        public static string DrawingNumber(string assembly)
        {
            var matches = Regex.Matches(Path.GetFileNameWithoutExtension(assembly), @"(?<!\d)\d{5}(?!\d)");
            if (matches.Count != 1) throw new InvalidOperationException("The assembly filename must contain exactly one five-digit drawing number.");
            return matches[0].Value;
        }
        public static string PartName(string sketch, string number)
        {
            if (String.IsNullOrWhiteSpace(sketch) || sketch.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || sketch.EndsWith(".") || sketch.EndsWith(" "))
                throw new InvalidOperationException("Rename sketch '" + sketch + "' in Solid Edge: its name cannot be used safely as a filename.");
            return sketch + " " + number + ".par";
        }
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
        public static SketchItem ResolveSketch(IEnumerable<SketchItem> current, SketchItem displayed)
        {
            var matches = current.Where(s => String.Equals(s.Name, displayed.Name, StringComparison.Ordinal)).ToList();
            if (matches.Count != 1 || matches[0].FileName != displayed.FileName)
                throw new InvalidOperationException("Sketch '" + displayed.Name + "' changed or is ambiguous. Click Refresh and select it again.");
            return matches[0];
        }
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

    public static class BushingWorkflow
    {
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
                    int pid=(int)part.Application.ProcessID;IntPtr hwnd=new IntPtr((int)part.Application.hWnd);
                    planeJob=new ChainSelectionJob(stop=>PickFeaturePlane(pid,hwnd,stop));
                    status.Text="Selecting Feature's Plane...";
                }catch(Exception e){status.Text=e.Message;}
            };
            next.Click+=delegate{Check(true);};
            Shown+=delegate {
                try {
                    part.SelectSet.RemoveAll();part.Application.StartCommand(45037);
                    int pid=(int)part.Application.ProcessID;IntPtr hwnd=new IntPtr((int)part.Application.hWnd);
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
                int pid=(int)part.Application.ProcessID;IntPtr hwnd=new IntPtr((int)part.Application.hWnd);
                projectionJob=new ChainSelectionJob(stop=>NativePlaneChoice.Select(hwnd,pid,stop,"Wireframe Chain"));
            }catch(Exception e){projectionDone=true;status.Text="Projection assistance: "+e.Message;}
        }
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
                            int pid=(int)part.Application.ProcessID;IntPtr hwnd=new IntPtr((int)part.Application.hWnd);
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
        static bool SelectWireframeChain(int processId, IntPtr handle, Func<bool> canceled)
        {
            if(canceled())return false;
            AutomationElement window=AutomationElement.FromHandle(handle);
            if(window==null || window.Current.ProcessId!=processId)return false;
            var boxes=window.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.ComboBox));
            foreach(AutomationElement box in boxes)
            {
                if(canceled())return false;
                if(!box.Current.IsEnabled || box.Current.IsOffscreen)continue;
                object value;
                if(box.TryGetCurrentPattern(ValuePattern.Pattern,out value) &&
                    String.Equals(((ValuePattern)value).Current.Value,"Wireframe Chain",StringComparison.OrdinalIgnoreCase))return true;
                object expand;
                if(!box.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,out expand))continue;
                var pattern=(ExpandCollapsePattern)expand;
                bool wasCollapsed=pattern.Current.ExpandCollapseState==ExpandCollapseState.Collapsed;
                try {
                    if(canceled())return false;
                    pattern.Expand();
                    var item=window.FindFirst(TreeScope.Descendants,new AndCondition(
                        new PropertyCondition(AutomationElement.NameProperty,"Wireframe Chain"),
                        new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.ListItem)));
                    object select;
                    if(!canceled() && item!=null && item.Current.ProcessId==processId && item.TryGetCurrentPattern(SelectionItemPattern.Pattern,out select)) {
                        ((SelectionItemPattern)select).Select();
                        return ((SelectionItemPattern)select).Current.IsSelected;
                    }
                } finally {if(wasCollapsed && !canceled())try{pattern.Collapse();}catch{} }
            }
            return false;
        }


    }
    public static class PartColors
    {
        public static string StyleForName(string name)
        {
            string key=Regex.Replace((name??"").ToLowerInvariant(),@"[\s_-]+","");
            if(key.Contains("collet") || key.Contains("diaphragm"))return "Yellow";
            if(key.Contains("partstop"))return "Green";
            if(key.Contains("adapter"))return "Bronze";
            if(key.Contains("puller"))return "Orange";
            if(key.Contains("body"))return "Khaki";
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

    public sealed class MainWindow : Form
    {
        readonly DataGridView grid = new DataGridView();
        readonly TextBox template = new TextBox();
        readonly Label assemblyLabel = new Label(), status = new Label();
        readonly Button process = new Button(), refresh = new Button();
        dynamic app, assembly;
        string assemblyPath;
        public MainWindow()
        {
            Text = "Sketch to Models v2.29 | Solid Edge 2026";
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
                app = Marshal.GetActiveObject("SolidEdge.Application");
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
                try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SketchToModels-error.txt"), "\r\n" + DateTime.Now.ToString("s") + " v2.29 ProcessSelection\r\n" + e.ToString() + "\r\n"); } catch { }
                status.Text = "Stopped: " + e.Message;
                MessageBox.Show(this, e.Message + "\n\nPreviously completed parts remain saved. Existing filenames are blocked on retry.", "Processing stopped", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { Enabled = true; UseWaitCursor = false; }
        }
        void ReconnectAssembly()
        {
            // Never dereference the document or sketches retained by the selection window.
            // Closing/reopening the assembly invalidates those COM objects, even at the same path.
            app = Marshal.GetActiveObject("SolidEdge.Application");
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
    sealed class BusyFilter : IMessageFilter
    {
        public int HandleInComingCall(int type, IntPtr caller, int ticks, IntPtr info) { return 0; }
        public int RetryRejectedCall(IntPtr callee, int ticks, int type) { return type == 2 && ticks < 15000 ? 150 : -1; }
        public int MessagePending(IntPtr callee, int ticks, int type) { return 2; }
        [DllImport("ole32.dll")] internal static extern int CoRegisterMessageFilter(IMessageFilter filter, out IMessageFilter previous);
    }
    static class Program
    {
        [STAThread] static void Main()
        {
            bool first;
            using (var mutex = new System.Threading.Mutex(true, "Local\\SolidEdgeSketchToModels", out first))
            {
                if (!first) { MessageBox.Show("Sketch to Models is already open."); return; }
                IMessageFilter previous; BusyFilter.CoRegisterMessageFilter(new BusyFilter(), out previous);
                try { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainWindow()); }
                finally { IMessageFilter unused; BusyFilter.CoRegisterMessageFilter(previous, out unused); }
            }
        }
    }
}




























