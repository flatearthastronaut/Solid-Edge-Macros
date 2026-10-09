using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SE = SolidEdge.Framework.Interop;
using Part = SolidEdge.Part.Interop;
using Geometry = SolidEdge.Geometry.Interop;

namespace BoltCircleHoles
{
    public sealed class HoleSize
    {
        // Shared selection record for all five dropdowns. Linear dimensions are inches,
        // including metric screw rows; conversion to Solid Edge's meters happens in
        // HoleEngine/ThreadChart. Null Depth/Radius means the source omitted a value,
        // not zero. Unavailable explains why a visible chart entry cannot be created.
        // For threads, Drill is nominal size until the native thread table is resolved.
        public string Screw;
        public double Drill, Bore;
        public double? Depth, Radius;
        public string Adapter, Unavailable;
        public bool ButtonHole;
        public ThreadSpec Thread;
        public override string ToString() { return Adapter==null ? Screw : Adapter+ (ButtonHole ? " - Button Hole" : " - Counterbore"); }
    }

    public static class Chart
    {
        // Prefer the fingerprint-matched embedded chart so the distributed macro can
        // run without Excel/ACE. Only an edited workbook reaches LoadWorkbook; never
        // silently substitute old embedded sizes for an unrecognized workbook.
        public static List<HoleSize> Load(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Keep C'bore Chart.xls beside the macro.", path);
            var portable=PortableChart.TryLoad(path);
            if(portable!=null)return portable;
            try {return LoadWorkbook(path);}
            catch(InvalidOperationException ex)
            {
                throw new InvalidOperationException("This chart differs from the portable data included with the macro, and this computer could not read the workbook. Use the matching C'bore Chart.xls from the release ZIP, or rebuild the portable chart for your updated workbook. Details: "+ex.Message,ex);
            }
        }
        public static List<HoleSize> LoadWorkbook(string path)
        {
            // HDR=NO lets us validate the exact header ourselves. IMEX=1 accommodates
            // mixed screw labels and numbers; ReadOnly and scoped disposal avoid
            // changing the standard or leaving the workbook locked between runs.
            var builder = new OleDbConnectionStringBuilder();
            builder.Provider = "Microsoft.ACE.OLEDB.12.0";
            builder.DataSource = path;
            builder["Extended Properties"] = "Excel 8.0;HDR=NO;IMEX=1;ReadOnly=True";
            var result = new List<HoleSize>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var connection = new OleDbConnection(builder.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM [Sheet1$A1:D1000]";
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read() || Text(reader[0]) != "SCREW" || Text(reader[1]) != "DRILL" || Text(reader[2]) != "C'BORE" || Text(reader[3]) != "DEPTH")
                            throw new InvalidDataException("Expected SCREW, DRILL, C'BORE, DEPTH in Sheet1 cells A1:D1.");
                        int row = 1;
                        while (reader.Read())
                        {
                            row++;
                            string screw = Text(reader[0]);
                            if (screw.Length == 0) continue; // Blank rows and the dated footer are not sizes.
                            double drill = Positive(reader[1], row, "DRILL");
                            double bore = Positive(reader[2], row, "C'BORE");
                            double? depth = Text(reader[3]).Length == 0 ? (double?)null : Positive(reader[3], row, "DEPTH");
                            if (bore <= drill) throw new InvalidDataException("Counterbore must exceed drill diameter in chart row " + row + ".");
                            if (!names.Add(screw)) throw new InvalidDataException("Duplicate screw size in chart: " + screw);
                            result.Add(new HoleSize { Screw = screw, Drill = drill, Bore = bore, Depth = depth });
                        }
                    }
                }
            }
            if (result.Count == 0) throw new InvalidDataException("The chart contains no counterbore sizes.");
            return result;
        }
        static string Text(object value) { return Convert.ToString(value, CultureInfo.InvariantCulture).Trim(); }
        static double Positive(object value, int row, string column)
        {
            double number;
            if (!double.TryParse(Text(value), NumberStyles.Float, CultureInfo.InvariantCulture, out number) || double.IsNaN(number) || double.IsInfinity(number) || number <= 0)
                throw new InvalidDataException("Invalid " + column + " in chart row " + row + ".");
            return number;
        }
    }

    public sealed class MainWindow : Form
    {
        // Selection flow: choose size -> locate support -> locate center -> stop the
        // custom command -> build geometry. Retain the original document and support
        // throughout; a new active document must not redirect a pending placement.
        readonly ComboBox sizes = new ComboBox(), metricSizes = new ComboBox(), a2Sizes = new ComboBox();
        readonly ComboBox metricThreads=new ComboBox(), inchThreads=new ComboBox();
        readonly HoleSymbol a2Symbol=new HoleSymbol();
        bool updatingSelection;
        HoleSize SelectedSize {get {return (metricThreads.SelectedItem ?? inchThreads.SelectedItem ?? a2Sizes.SelectedItem ?? metricSizes.SelectedItem ?? sizes.SelectedItem) as HoleSize;}}
        readonly NumericUpDown holeCount = new NumericUpDown { Minimum=1, Maximum=999, Value=1, DecimalPlaces=0 };
        readonly Label spacing = new Label();
        readonly Label dimensions = new Label(), status = new Label(), partName = new Label();
        readonly Button pick = new Button(), back = new Button(), six = new Button();
        readonly CheckBox reverse = new CheckBox { Text = "Reverse cutting direction", Left=24, Top=352, Width=450, Height=26 };
        LogWindow logWindow;
        SE.Application edge;
        Part.PartDocument part;
        SE.Command command;
        SE.Mouse mouse;
        // Preserve the actual face or reference plane as the hole Create From input.
        object selectedSupport;
        bool picking, closing, awaitingCenter;
        Part.Model selectedModel;
        double[] supportRoot, supportNormal;
        int generation;
        // Every StopPick increments generation. Deferred callbacks carry the previous
        // value and are discarded after cancellation or a new selection session.
        readonly bool preview;
        public MainWindow(bool previewOnly)
        {
            preview = previewOnly;
            Text = "Bolt Circle Holes v0.20";
            // The window uses the executable's embedded icon, so copied macros do
            // not depend on an external ICO. Release the owned GDI object on close.
            Icon windowIcon;
            using(var stream=typeof(MainWindow).Assembly.GetManifestResourceStream("BoltCircleHoles.Icon"))
            using(var embedded=new Icon(stream))windowIcon=(Icon)embedded.Clone();
            Icon=windowIcon;
            Disposed+=delegate {if(windowIcon!=null)windowIcon.Dispose();};
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(510, 574);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            var title = new Label { Text = "Choose a hole", Font = new Font(Font.FontFamily, 14, FontStyle.Bold), Left = 22, Top = 10, Width = 465, Height = 36 };
            partName.SetBounds(24, 46, 460, 26);
            partName.AutoEllipsis = true;

            var sizeLabel = new Label { Text = "Inch", Left = 24, Top = 86, Width = 105, Height = 26 };
            sizes.SetBounds(155, 82, 278, 30);
            sizes.DropDownStyle = ComboBoxStyle.DropDownList;
                        sizes.SelectedIndexChanged += delegate { SelectSize(sizes); };
            metricSizes.SetBounds(155,116,278,30);metricSizes.DropDownStyle=ComboBoxStyle.DropDownList;
            a2Sizes.SetBounds(155,150,278,30);a2Sizes.DropDownStyle=ComboBoxStyle.DropDownList;
            metricSizes.SelectedIndexChanged+=delegate {SelectSize(metricSizes);};
            metricSizes.SelectionChangeCommitted+=delegate {SelectSize(metricSizes);};
            a2Sizes.SelectedIndexChanged+=delegate {SelectSize(a2Sizes);};
            Controls.AddRange(new Control[]{metricSizes,a2Sizes,
                new Label {Text="Metric",Left=24,Top=120,Width=105,Height=26},
                new Label {Text="A2 Holes",Left=24,Top=154,Width=110,Height=26}});
            foreach(var list in new[]{metricThreads,inchThreads})
            {
                list.DropDownStyle=ComboBoxStyle.DropDownList;
                var selectedList=list;
                list.SelectedIndexChanged+=delegate {SelectSize(selectedList);};
            }
            metricThreads.SetBounds(155,184,278,30);inchThreads.SetBounds(155,218,278,30);
            Controls.AddRange(new Control[]{metricThreads,inchThreads,
                new Label {Text="Metric Threads",Left=24,Top=188,Width=128,Height=26},
                new Label {Text="Inch Threads",Left=24,Top=222,Width=128,Height=26}});
            a2Symbol.SetBounds(441,150,42,28);a2Symbol.Kind="Counterbore";
            Controls.AddRange(new Control[]{a2Symbol,
                new HoleSymbol {Left=441,Top=82,Kind="Counterbore"},
                new HoleSymbol {Left=441,Top=116,Kind="Counterbore"},
                new HoleSymbol {Left=441,Top=184,Kind="Thread"},
                new HoleSymbol {Left=441,Top=218,Kind="Thread"}});
            dimensions.SetBounds(24, 260, 455, 86);
            var countLabel=new Label {Text="Number of holes",Left=24,Top=386,Width=155,Height=26};
            holeCount.SetBounds(185,383,100,30);
            six.SetBounds(295,383,42,28);six.Text="6";six.AccessibleName="Set quantity to six";
            six.Click+=delegate {holeCount.Value=6;};Controls.Add(six);
            spacing.SetBounds(24,418,455,28);
            holeCount.ValueChanged+=delegate {UpdateSpacing();};
            UpdateSpacing();
            Controls.AddRange(new Control[]{countLabel,holeCount,spacing});
            status.SetBounds(24, 450, 455, 62);
            pick.SetBounds(286, 522, 195, 36);
            pick.Text = "Select face / plane";
            pick.Click += delegate { BeginPick(); };
            back.SetBounds(24, 522, 126, 36);
            back.Text = "Close";
            back.Click += delegate { if (picking || selectedSupport != null) Reset(); else Close(); };
            var logButton=new Button {Text="View log",Left=165,Top=522,Width=105,Height=36};
            logButton.Click+=delegate
            {
                if(logWindow==null || logWindow.IsDisposed) logWindow=new LogWindow();
                logWindow.Show(this);logWindow.BringToFront();
            };
            Controls.Add(logButton);
            Controls.AddRange(new Control[] { title, partName, sizeLabel, sizes, dimensions, reverse, status, pick, back });
            Shown += delegate { Initialize(); };
            FormClosing += delegate { RunLog.Write("UI.close","Closing macro");closing = true; StopPick(); };
        }
        void UpdateSpacing()
        {
            int count=(int)holeCount.Value;
            spacing.Text=count==1 ? "1 hole (no pattern)" : count+" holes total, "+(360.0/count).ToString("0.###",CultureInfo.InvariantCulture)+" degrees apart around Z";
        }
        void Initialize()
        {
            try
            {
                if (!preview)
                {
                    edge = RunLog.Call("CONNECT.SolidEdge",delegate {return (SE.Application)Marshal.GetActiveObject("SolidEdge.Application");});
                    part = edge.ActiveDocument as Part.PartDocument;
                    if (part == null || part.Type != SE.DocumentTypeConstants.igPartDocument)
                        throw new InvalidOperationException("Open a part (.par) in Solid Edge, then run this macro.");
                    partName.Text = "Part: " + part.Name;
                    RunLog.Write("DOCUMENT","name="+part.Name+" path="+part.FullName+" type="+part.Type+" modelingMode="+part.ModelingMode+" models="+part.Models.Count+" command="+edge.GetActiveCommand());
                }
                else partName.Text = "Part: Example.par";
                string chartPath=Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "C'bore Chart.xls");
                RunLog.Write("CHART.path",chartPath);
                                var chart=RunLog.Call("CHART.load",delegate {return Chart.Load(chartPath);});
                foreach(var size in chart)
                    if(size.Screw.StartsWith("M",StringComparison.OrdinalIgnoreCase))metricSizes.Items.Add(size);else sizes.Items.Add(size);
                foreach(var size in A2Chart.Load(chart))a2Sizes.Items.Add(size);
                foreach(var size in ThreadChart.Load(true))metricThreads.Items.Add(size);
                foreach(var size in ThreadChart.Load(false))inchThreads.Items.Add(size);
                RunLog.Write("CHART.loaded","rows="+(sizes.Items.Count+metricSizes.Items.Count)+" last_write_utc="+File.GetLastWriteTimeUtc(chartPath).ToString("o"));
                sizes.SelectedIndex = -1;
                pick.Enabled = false;
                status.Text = "Select a screw size, then choose its Create From face or plane.";
                if(RunLog.WriteFailure!=null) status.Text="Run log could not be written: "+RunLog.WriteFailure;
            }
            catch (Exception ex) { Fail(ex); sizes.Enabled = metricSizes.Enabled = a2Sizes.Enabled = metricThreads.Enabled = inchThreads.Enabled = six.Enabled = pick.Enabled = false; }
        }
        void SelectSize(ComboBox changed)
        {
            // Clearing another dropdown raises its event synchronously. The guard
            // prevents those nested events from clearing the user's new selection.
            // A2 counterbores also display the corresponding metric screw; SelectedSize
            // prioritizes the A2 record so its preset radius is not lost.
            if(updatingSelection)return;
            updatingSelection=true;
            try
            {
                foreach(var list in new[]{sizes,metricSizes,a2Sizes,metricThreads,inchThreads})
                    if(list!=changed)list.SelectedIndex=-1;
                if(changed==a2Sizes)
                {
                    var a2=a2Sizes.SelectedItem as HoleSize;
                    if(a2!=null && !a2.ButtonHole)for(int i=0;i<metricSizes.Items.Count;i++)
                        if(((HoleSize)metricSizes.Items[i]).Screw==a2.Screw){metricSizes.SelectedIndex=i;break;}
                }
            }
            finally{updatingSelection=false;}
            UpdateSize();
        }
        void UpdateSize()
        {
            var size = SelectedSize;
            a2Symbol.Kind=size!=null && size.ButtonHole ? "Button" : "Counterbore";
            pick.Enabled = size != null && size.Depth.HasValue && size.Unavailable==null;
            if (size == null) {dimensions.Text="";return;}
            RunLog.Write("SIZE.selected","screw="+size.Screw+" drill_in="+RunLog.Value(size.Drill)+" cbore_in="+RunLog.Value(size.Bore)+" depth_in="+RunLog.Value(size.Depth));
            dimensions.Text = "Drill dia.  " + Format(size.Drill) + "     Counterbore dia.  " + Format(size.Bore) +
                "\r\nCounterbore depth  " + (size.Depth.HasValue ? Format(size.Depth.Value) : "Not specified in chart");
            dimensions.Text="Screw: "+size.Screw+"\r\n"+dimensions.Text+
                "\r\nRadius from Z axis: "+(size.Adapter==null ? "Set by your center click" : size.Radius.HasValue ? Format(size.Radius.Value)+" (A2 chart Z)" : "Not specified in A2 chart");
            if(size.Drill==0)dimensions.Text="Screw: "+size.Screw+"\r\nCounterbore dimensions are not listed.\r\nRadius: not specified in A2 chart";
            if(size.ButtonHole)dimensions.Text="Button Hole (blind, 120-degree V bottom)\r\nDiameter  "+(size.Drill>0 ? Format(size.Drill) : "Not specified")+
                "     Depth  "+(size.Depth.HasValue ? Format(size.Depth.Value) : "Not specified")+
                "\r\nRadius from Z axis: "+(size.Radius.HasValue ? Format(size.Radius.Value)+" (A2 chart Z)" : "Not specified in A2 chart");
            if(size.Thread!=null)dimensions.Text="Thread: "+size.Screw+"\r\nFull thread depth  "+Format(size.Thread.FullDepthInches)+
                "\r\nDrill shoulder depth  "+Format(size.Depth.Value)+" (120-degree point)\r\nTap drill: Solid Edge thread table";
            RunLog.Write("SIZE.mode","adapter="+size.Adapter+" radius_in="+RunLog.Value(size.Radius)+" unavailable="+size.Unavailable);
            status.ForeColor = SystemColors.ControlText;
            status.Text = size.Thread!=null ? "Click the first thread position. Quantity is equally spaced around Z in one Hole feature; no dimensions." : size.Unavailable ?? (size.Adapter!=null && size.Depth.HasValue ? "Select a face perpendicular to Z, then click to set the angle. The A2 chart fixes the radius." : size.Depth.HasValue ? "Select face / plane, then click a flat face or reference plane." : "This chart size has no counterbore depth. Add its depth to the chart and restart before creating a hole.");
        }
        static string Format(double n) { return n.ToString("0.000", CultureInfo.InvariantCulture) + " in"; }
        static bool Same(object a, object b)
        {
            // Different managed wrappers may represent the same COM object. Compare
            // IUnknown identity rather than wrapper references, releasing only the
            // temporary AddRef pointers acquired here, not the shared RCWs themselves.
            if (a == null || b == null) return false;
            IntPtr x = Marshal.GetIUnknownForObject(a), y = Marshal.GetIUnknownForObject(b);
            try { return x == y; } finally { Marshal.Release(x); Marshal.Release(y); }
        }
        void RequirePart()
        {
            if (!Same(edge.ActiveDocument, part)) throw new InvalidOperationException("Return to the original part before selecting a face or plane. Close and rerun the macro to use a different part.");
        }
        void BeginPick()
        {
            // This command gathers input only. Geometry is created after StopPick so
            // Solid Edge is not still executing the mouse command during feature edits.
            // Validate dimensions first and lock size/count until this attempt ends.
            if (preview) return;
            try
            {
                RequirePart();
                RunLog.Write("PICK.begin","activePart="+part.Name+" command="+edge.GetActiveCommand()+" reverse="+reverse.Checked);
                if (SelectedSize == null) return;
                HoleEngine.Dimensions(SelectedSize);
                StopPick();
                selectedSupport = null;
                command = edge.CreateCommand(2); // seNoDeactivate, verified in installed SE 2026 constants.
                mouse = command.Mouse;
                mouse.MouseClick += OnSupportClick;
                command.Terminate += CommandTerminated;
                picking = true;
                command.Start();
                RunLog.Write("PICK.command","Started custom selection command");
                mouse.LocateMode = 2; // seLocateQuickPick
                mouse.WindowTypes = 1; // Graphic windows
                mouse.ScaleMode = 1;
                mouse.InterDocumentLocate = false;
                mouse.PathfinderLocate = true;
                mouse.ClearLocateFilter();
                mouse.AddToLocateFilter(30); // seLocateRefPlane
                mouse.AddToLocateFilter(32); // seLocateFace; reject nonplanar geometry below.
                sizes.Enabled = metricSizes.Enabled = a2Sizes.Enabled = metricThreads.Enabled = inchThreads.Enabled = six.Enabled = holeCount.Enabled = false;
                pick.Enabled = false;
                back.Text = "Back";
                status.ForeColor = SystemColors.ControlText;
                status.Text = "Click a flat face or reference plane in Solid Edge for the hole's Create From support.\r\nRight-click to cancel selection.";
            }
            catch (Exception ex) { StopPick(); sizes.Enabled = metricSizes.Enabled = a2Sizes.Enabled = metricThreads.Enabled = inchThreads.Enabled = six.Enabled = holeCount.Enabled = true; pick.Enabled = SelectedSize != null; Fail(ex); }
        }
        void Queue(Action action)
        {
            // Leave Solid Edge's COM event callback before changing command state or
            // creating features. BeginInvoke returns work to the same WinForms STA;
            // a worker thread would violate the automation objects' apartment usage.
            if (!closing && IsHandleCreated && !IsDisposed)
                try { BeginInvoke(action); } catch (InvalidOperationException) { }
        }
        void OnSupportClick(short button, short shift, double x, double y, double z, object window, int keyPoint, object graphic)
        {
            // Capture transient locate information immediately, but defer modeling.
            // The queued stage/generation checks also reject an extra rapid click that
            // was received for the previous stage before the UI processed it.
            RunLog.Write("MOUSE.click","button="+button+" stage="+(awaitingCenter?"center":"support")+" xyz="+RunLog.Value(new[]{x,y,z})+" graphic="+(graphic is Geometry.Face?"Face":graphic is Part.RefPlane?"RefPlane":graphic==null?"null":graphic.GetType().FullName)+" keyPoint="+keyPoint);
            int current = generation;
            bool centerStage = awaitingCenter;
            double[] center = null;
            Exception locateError = null;
            // Capture the actual located point before later mouse events replace it.
            if (button == 1 && centerStage)
                try { center = LocateCenter(x,y,z,window,graphic); } catch (Exception ex) { locateError = ex; }
            Queue(delegate
            {
                if (!picking || current != generation || centerStage != awaitingCenter) return;
                if (button == 2) { Reset(); return; }
                if (button != 1) return;
                try
                {
                    RequirePart();
                    if (centerStage)
                    {
                        if (locateError != null) throw locateError;
                        RunLog.Write("CENTER.accepted","xyz_m="+RunLog.Value(center));
                        StopPick();
                        pick.Enabled = back.Enabled = reverse.Enabled = false;
                        try
                        {
                            status.Text = SelectedSize.Thread!=null ? "Creating equally spaced threaded holes..." : holeCount.Value==1 ? "Creating hole..." : "Creating hole and circular pattern...";
                            status.Refresh();
                            HoleEngine.Create(part,selectedModel,selectedSupport,center,SelectedSize,reverse.Checked,(int)holeCount.Value);
                            selectedSupport = null;
                            status.ForeColor = Color.DarkGreen;
                            status.Text = SelectedSize.Thread!=null ? holeCount.Value+" threaded holes created in one Hole feature. No dimensions.\r\nThe part has not been saved." : (holeCount.Value==1 ? "Hole created." : holeCount.Value+" holes created in a circular pattern.")+(SelectedSize.ButtonHole ? " Blind depth: "+Format(SelectedSize.Depth.Value) : " Drill: Through All.")+"\r\nThe part has not been saved.";
                            pick.Text = "Create another";
                            back.Text = "Close";
                        }
                        finally { pick.Enabled = back.Enabled = reverse.Enabled = sizes.Enabled = metricSizes.Enabled = a2Sizes.Enabled = metricThreads.Enabled = inchThreads.Enabled = six.Enabled = holeCount.Enabled = true; }
                        return;
                    }
                    var plane = graphic as Part.RefPlane;
                    var face = graphic as Geometry.Face;
                    Array normal = new double[3], root = new double[3];
                    string name;
                    bool belongs = false;
                    if (plane != null)
                    {
                        for (int i = 1; i <= part.RefPlanes.Count; i++)
                            if (Same(part.RefPlanes.Item(i), plane)) { belongs = true; break; }
                        if (!belongs) throw new InvalidOperationException("Select a reference plane belonging to this part.");
                        plane.GetNormal(ref normal); plane.GetRootPoint(ref root);
                        if(part.Models.Count!=1) throw new InvalidOperationException("For a part with multiple bodies, select a flat face on the body to drill.");
                        selectedModel=part.Models.Item(1);
                        name = plane.Name;
                        if (String.IsNullOrWhiteSpace(name)) name = "Reference plane";
                    }
                    else if (face != null)
                    {
                        var surface = face.Geometry as Geometry.Plane;
                        if (surface == null) throw new InvalidOperationException("That face is curved. Select a flat face or reference plane for Create From.");
                        object body = face.Body;
                        for (int i = 1; i <= part.Models.Count; i++)
                            if (Same(part.Models.Item(i).Body, body)) { belongs = true; selectedModel=part.Models.Item(i); break; }
                        if (!belongs) throw new InvalidOperationException("Select a flat face on a model in this part.");
                        surface.GetNormalVector(ref normal); surface.GetRootPoint(ref root);
                        name = "Flat face";
                    }
                    else throw new InvalidOperationException("Select a flat face or reference plane for Create From.");
                    double length = 0;
                    foreach (object value in normal) { double n = Convert.ToDouble(value); length += n * n; }
                    foreach (object value in root)
                    {
                        double n = Convert.ToDouble(value);
                        if (double.IsNaN(n) || double.IsInfinity(n)) throw new InvalidOperationException("The selected support has invalid geometry.");
                    }
                    if (double.IsNaN(length) || double.IsInfinity(length) || length < 1e-16) throw new InvalidOperationException("The selected support has invalid geometry.");
                    // Keep the original face, not its unbounded mathematical Plane geometry.
                    selectedSupport = graphic;
                    RunLog.Write("SUPPORT.accepted","name="+name+" root_m="+RunLog.Value(root)+" normal="+RunLog.Value(normal)+" body="+selectedModel.BodyName);
                    supportRoot=HoleEngine.Vector(root); supportNormal=HoleEngine.Vector(normal);
                    awaitingCenter=true;
                    mouse.PathfinderLocate=false;
                    status.ForeColor = SystemColors.ControlText;
                    status.Text = "Create From: " + name + "\r\nClick on this support to place the first hole (A2: click sets angle; chart sets radius). Right-click to cancel.";
                    back.Text = "Cancel";
                }
                catch (Exception ex) { Fail(ex); }
            });
        }
        double[] LocateCenter(double x,double y,double z,object window,object graphic)
        {
            // A face pick must hit that exact bounded face: generic click XYZ can lie
            // on the view plane instead. A reference plane has no bounded face hit, so
            // construct a view ray and intersect it with the chosen mathematical plane.
            // Distances here are meters; the face round-trip tolerance is one micron.
            if(selectedSupport is Geometry.Face)
            {
                if(!Same(graphic,selectedSupport)) throw new InvalidOperationException("Click the center on the selected flat face.");
                int valid=0; double px=0,py=0,pz=0;
                mouse.PointOnGraphic(out valid,out px,out py,out pz);
                RunLog.Write("CENTER.PointOnGraphic","valid="+valid+" xyz_m="+RunLog.Value(new[]{px,py,pz}));
                if(valid==0) throw new InvalidOperationException("Solid Edge could not locate a point on the face. Click the flat face again.");
                var point=new[] {px,py,pz};
                double distance=HoleEngine.Dot(HoleEngine.Difference(point,supportRoot),supportNormal)/Math.Sqrt(HoleEngine.Dot(supportNormal,supportNormal));
                if(double.IsNaN(distance) || double.IsInfinity(distance) || Math.Abs(distance)>1e-6) throw new InvalidOperationException("The located point is not on the Create From face.");
                return point;
            }
            var viewWindow=window as SE.Window;
            if(viewWindow==null) throw new InvalidOperationException("Click the center in the graphics view.");
            double vx=0,vy=0,vz=0;
            viewWindow.View.TransformModelToView(x,y,z,out vx,out vy,out vz);
            double ax=0,ay=0,az=0,bx=0,by=0,bz=0;
            viewWindow.View.TransformViewToModel(vx,vy,0,out ax,out ay,out az);
            viewWindow.View.TransformViewToModel(vx,vy,1,out bx,out by,out bz);
            return HoleEngine.Intersect(new[] {ax,ay,az},new[] {bx-ax,by-ay,bz-az},supportRoot,supportNormal);
        }
        void CommandTerminated()
        {
            RunLog.Write("PICK.terminated","generation="+generation+" picking="+picking);
            int current = generation;
            Queue(delegate { if (picking && current == generation) { Reset(); status.Text = "Face / plane selection ended. Click Select face / plane to try again."; } });
        }
        void StopPick()
        {
            // Invalidate queued work before disconnecting event handlers. Completing
            // a command can itself trigger termination, so unsubscribe before Done.
            // Keep local references alive until teardown finishes, then let the CLR
            // manage wrapper lifetimes; force-releasing shared COM objects is unsafe.
            RunLog.Write("PICK.stop","hadCommand="+(command!=null)+" centerStage="+awaitingCenter);
            picking = false; awaitingCenter=false;
            generation++;
            var oldMouse = mouse; var oldCommand = command;
            mouse = null; command = null;
            if (oldMouse != null) try { oldMouse.MouseClick -= OnSupportClick; } catch (Exception ex) { Program.Log(ex); }
            if (oldCommand != null)
            {
                try { oldCommand.Terminate -= CommandTerminated; } catch (Exception ex) { Program.Log(ex); }
                try { RunLog.Call("PICK.command.Done",delegate {oldCommand.Done = true;}); } catch (Exception ex) { Program.Log(ex); }
            }
        }
        void Reset()
        {
            StopPick(); selectedSupport = null; selectedModel=null;
            sizes.Enabled = metricSizes.Enabled = a2Sizes.Enabled = metricThreads.Enabled = inchThreads.Enabled = six.Enabled = holeCount.Enabled = true;
            pick.Text = "Select face / plane";
            back.Text = "Close";
            UpdateSize();
        }
        void Fail(Exception ex)
        {
            Program.Log(ex);
            status.ForeColor = Color.Firebrick;
            status.Text = ex is COMException && edge == null ? "Start Solid Edge and open a part, then run the macro again." : ex.Message;
            // Full errors remain visible and copyable even when the compact status area is too small.
            if(!preview) MessageBox.Show(this,status.Text+"\r\n\r\nDetails: "+RunLog.PathName,"Bolt Circle Holes",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
        public void RenderPreview(string path)
        {
            Show();
            sizes.SelectedIndex = 3;
            using (var bitmap = new Bitmap(Width, Height)) { DrawToBitmap(bitmap, new Rectangle(0, 0, Width, Height)); bitmap.Save(path); }
            Close();
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
        // Following the SDK OleMessageFilter pattern, retry only SERVERCALL_RETRYLATER
        // (2), at 150 ms intervals for up to five seconds. Other rejections and a busy
        // application beyond that window propagate as errors instead of hanging forever.
        public int HandleInComingCall(int type, IntPtr caller, int ticks, IntPtr info) { return 0; }
        public int RetryRejectedCall(IntPtr callee, int ticks, int type)
        {
            int wait = type == 2 && ticks < 5000 ? 150 : -1;
            RunLog.Write("COM.busy","reject_type="+type+" elapsed_ms="+ticks+" retry_delay_ms="+wait);
            return wait;
        }
        public int MessagePending(IntPtr callee, int ticks, int type) { return 2; }
        [DllImport("ole32.dll")] internal static extern int CoRegisterMessageFilter(IMessageFilter filter, out IMessageFilter previous);
    }
    static class Program
    {
        internal static void Log(Exception ex)
        {
            RunLog.Error("ERROR",ex);
            try { File.AppendAllText(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "BoltCircleHoles-error.txt"), DateTime.Now.ToString("s") + " " + ex + Environment.NewLine); } catch { }
        }
        [STAThread] static int Main(string[] args)
        {
            // All UI, event handling, and Solid Edge automation share this STA thread.
            // The local-session mutex prevents two macro windows from competing for
            // selection events. Restore the previous OLE filter on every exit path;
            // the macro owns neither the running Solid Edge process nor its document.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException+=delegate(object sender,System.Threading.ThreadExceptionEventArgs e){Log(e.Exception);MessageBox.Show(e.Exception.Message+"\r\nSee BoltCircleHoles-run.log.","Bolt Circle Holes");};
            AppDomain.CurrentDomain.UnhandledException+=delegate(object sender,UnhandledExceptionEventArgs e){RunLog.Write("UNHANDLED",Convert.ToString(e.ExceptionObject));};
            if (args.Length == 2 && args[0] == "--preview") { using (var form = new MainWindow(true)) form.RenderPreview(args[1]); return 0; }
            bool first;
            using (var mutex = new System.Threading.Mutex(true, "Local\\SolidEdgeBoltCircleHoles", out first))
            {
                if (!first) { MessageBox.Show("Bolt Circle Holes is already open."); return 1; }
                RunLog.Write("SESSION.start","version=0.20 exe="+typeof(Program).Assembly.Location+" 64bit="+Environment.Is64BitProcess+" CLR="+Environment.Version);
                IMessageFilter previous;
                var filter = new BusyFilter();
                BusyFilter.CoRegisterMessageFilter(filter, out previous);
                try { Application.Run(new MainWindow(false)); return 0; }
                catch (Exception ex) { Log(ex); MessageBox.Show(ex.Message, "Bolt Circle Holes"); return 1; }
                finally { IMessageFilter unused; BusyFilter.CoRegisterMessageFilter(previous, out unused); GC.KeepAlive(filter); RunLog.Write("SESSION.end","Macro exiting"); }
            }
        }
    }
}
