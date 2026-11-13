using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using TYPEATTR = System.Runtime.InteropServices.ComTypes.TYPEATTR;
using FUNCDESC = System.Runtime.InteropServices.ComTypes.FUNCDESC;
using INVOKEKIND = System.Runtime.InteropServices.ComTypes.INVOKEKIND;
using System.Text;
using HoleTableFormatter;

namespace TurboCallout
{
    internal static class Property
    {
        internal static object Get(object item, string name)
        {
            try { return item.GetType().InvokeMember(name, BindingFlags.GetProperty, null, item, null); }
            catch (Exception error) { throw new InvalidOperationException("Cannot read " + name + ": " + error.GetBaseException().Message, error); }
        }
        internal static void Set(object item, string name, object value)
        {
            try { item.GetType().InvokeMember(name, Marshal.IsComObject(item) ? BindingFlags.PutDispProperty : BindingFlags.SetProperty, null, item, new[] { value }); }
            catch (Exception error) { throw new InvalidOperationException("Cannot set " + name + ": " + error.GetBaseException().Message, error); }
        }
        internal static bool Equal(object a, object b)
        {
            if (a is double || b is double) return Math.Abs(Convert.ToDouble(a) - Convert.ToDouble(b)) < 1e-10;
            if (a != null && a.GetType().IsEnum) a = Convert.ToInt32(a);
            if (b != null && b.GetType().IsEnum) b = Convert.ToInt32(b);
            return Object.Equals(a, b);
        }
    }

    internal static class NoteAlignment
    {
        // SDK: West=0, East=2; horizontal justification Left=0, Right=2.
        // The older Boolean BreakLineDirection can remain false for an East
        // callout in current drafts. Use the four-direction native property.
        internal static int? ForBreakline(bool leader, bool breakLine, int direction)
        {
            if (!leader || !breakLine) return null;
            return direction == 0 ? (int?)0 : direction == 2 ? (int?)2 : null;
        }
        internal static int? Target(object note)
        {
            return ForBreakline((bool)((dynamic)note).Leader, (bool)((dynamic)note).BreakLine,
                (int)((dynamic)note).CalloutBalloonBreaklineDirection);
        }
        internal static bool IsExistingMacroNote(object note)
        {
            object style = null;
            try
            {
                style = ((dynamic)note).Style;
                string name = (string)((dynamic)style).Name;
                return NoteStyles.IsManaged(name);
            }
            finally { Com.Release(style); }
        }
        internal static bool Apply(object note)
        {
            int? target = Target(note);
            if (!target.HasValue || (int)((dynamic)note).CalloutHorizontalAlignment == target.Value) return false;
            var before = new Placement(note);
            try { before.Restore(note, target); return true; }
            catch (Exception error)
            {
                try { before.Restore(note); }
                catch (Exception rollback) { throw new InvalidOperationException("Alignment failed: " + error.Message + " Restoration needs review: " + rollback.Message, error); }
                throw new InvalidOperationException("Original alignment restored: " + error.Message, error);
            }
        }
    }

    // Snapshot placement independently of the dimension style. The one allowed
    // adjustment is the requested horizontal justification. An unmodified
    // snapshot is retained for rollback; leader geometry stays unchanged.
    internal sealed class Placement
    {
        static readonly string[] Names = { "CalloutHorizontalAlignment", "CalloutLeaderTextConnectionPoint", "CalloutBalloonBreaklineDirection",
            "BreakLine", "BreakLineDirection", "BreakLineDistance", "Leader", "RotationAngle", "TextScale",
            "CalloutTextWidth", "CalloutTextWidthMode", "CalloutTextAspectRatio" };
        readonly Dictionary<string, object> properties = new Dictionary<string, object>();
        readonly List<double[]> vertices = new List<double[]>();
        double x, y, z;
        internal Placement(object note)
        {
            foreach (string name in Names)
            {
                // Fit-to-content width is a result of the text, not a placement
                // setting. A blank's zero width must grow with its new note.
                if (name == "CalloutTextWidth" && (int)((dynamic)note).CalloutTextWidthMode == 1) continue;
                properties.Add(name, Property.Get(note, name));
            }
            ((dynamic)note).GetFreeSpacePoint(out x, out y, out z);
            int count = (int)((dynamic)note).VertexCount;
            for (int i = 0; i < count; i++)
            { double vx = 0, vy = 0, vz = 0; ((dynamic)note).GetVertex(i, out vx, out vy, out vz); vertices.Add(new[] { vx, vy, vz }); }
        }
        internal void Restore(object note, int? horizontalAlignment = null)
        {
            foreach (var item in properties)
            {
                object wanted = item.Key == "CalloutHorizontalAlignment" && horizontalAlignment.HasValue ? horizontalAlignment.Value : item.Value;
                if (!Property.Equal(Property.Get(note, item.Key), wanted)) Property.Set(note, item.Key, wanted);
            }
            double ax, ay, az; ((dynamic)note).GetFreeSpacePoint(out ax, out ay, out az);
            if (!Property.Equal(x, ax) || !Property.Equal(y, ay) || !Property.Equal(z, az)) ((dynamic)note).SetFreeSpacePoint(x, y, z);
            if ((int)((dynamic)note).VertexCount != vertices.Count) throw new InvalidOperationException("Leader vertex count changed.");
            for (int i = 0; i < vertices.Count; i++)
            {
                // An unattached template note can report one vertex while
                // GetVertex leaves its ByRef outputs untouched. Match capture's
                // fresh buffers; never reuse the free-space point as a vertex.
                double vx = 0, vy = 0, vz = 0;
                ((dynamic)note).GetVertex(i, out vx, out vy, out vz); double[] v = vertices[i];
                if (!Property.Equal(v[0], vx) || !Property.Equal(v[1], vy) || !Property.Equal(v[2], vz)) ((dynamic)note).SetVertex(i, v[0], v[1], v[2]);
            }
            Verify(note, horizontalAlignment);
        }
        internal void Verify(object note, int? horizontalAlignment = null)
        {
            foreach (var item in properties)
            {
                object wanted = item.Key == "CalloutHorizontalAlignment" && horizontalAlignment.HasValue ? horizontalAlignment.Value : item.Value;
                if (!Property.Equal(Property.Get(note, item.Key), wanted)) throw new InvalidOperationException("Callout alignment changed: " + item.Key);
            }
            double ax, ay, az; ((dynamic)note).GetFreeSpacePoint(out ax, out ay, out az);
            if (!Property.Equal(x, ax) || !Property.Equal(y, ay) || !Property.Equal(z, az)) throw new InvalidOperationException("Callout position changed.");
            if ((int)((dynamic)note).VertexCount != vertices.Count) throw new InvalidOperationException("Leader vertex count changed.");
            for (int i = 0; i < vertices.Count; i++)
            {
                // Use the same initialized buffers as capture/restore. The
                // separate free-space-point check above still detects movement
                // when an unattached callout exposes no individual vertex.
                double vx = 0, vy = 0, vz = 0;
                ((dynamic)note).GetVertex(i, out vx, out vy, out vz); double[] v = vertices[i];
                if (!Property.Equal(v[0], vx) || !Property.Equal(v[1], vy) || !Property.Equal(v[2], vz)) throw new InvalidOperationException("Leader position changed.");
            }
        }
    }

    internal sealed class UnitStyle
    {
        readonly Dictionary<string, object> before = new Dictionary<string, object>();
        readonly Dictionary<string, object> after = new Dictionary<string, object>();
        internal UnitStyle(object style, NotePlan plan)
        {
            // Enum decimal precision is digits+2. The current rule requires
            // one fewer metric digit than inch; typed higher-precision limits
            // are formatted individually by Notes.ApplyPrecision.
            int metric = plan.InchPlaces + 1;
            Add(style, "PrimaryUnits", plan.Dual ? 3 : 5);
            Add(style, "RoundOffType", 1);
            Add(style, "PrimaryDecimalRoundOff", plan.Dual ? metric : plan.InchPlaces + 2);
            Add(style, "PrimaryTrailingZeros", true);
            Add(style, "DualDisplay", plan.Dual);
            Add(style, "PrimaryLinearToleranceUnits", plan.Dual ? 3 : 5);
            Add(style, "PrimaryLinearToleranceRoundOffType", 1);
            Add(style, "PrimaryLinearToleranceDecimalRoundOff", plan.Dual ? metric : plan.InchPlaces + 2);
            // Normalize inactive secondary settings too. Otherwise converting
            // old 2/3/4-place dual styles into inch-only notes produces three
            // visually identical named styles differing only in hidden units.
            Add(style, "SecondaryUnits", plan.Dual ? 5 : 3); Add(style, "SecondaryUnitRoundOff", 1);
            Add(style, "SecondaryDecimalRoundOff", plan.Dual ? plan.InchPlaces + 2 : metric); Add(style, "SecondaryTrailingZeros", true);
            Add(style, "SecondaryLinearToleranceUnits", plan.Dual ? 5 : 3); Add(style, "SecondaryLinearToleranceRoundOffType", 1);
            Add(style, "SecondaryLinearToleranceDecimalRoundOff", plan.Dual ? plan.InchPlaces + 2 : metric);
            Add(style, "SecondaryToleranceEquivalent", true); Add(style, "SecondaryUnitSeparator", 2);
            Add(style, "SmartDepthHoleThru", "THRU"); Add(style, "SmartDepthThreadThru", "THRU");
            Add(style, "SmartDepthHoleFinite", plan.Dual ? "%{%HD/DU/@1} DP." : "%{%HD/@2} DP.");
            Add(style, "SmartDepthThreadFinite", plan.Dual ? "%{%TD/DU/@1} DP." : "%{%TD/@2} DP.");
            // Literal /ST limits use the tolerance-height ratio, not the
            // fraction-size setting. Match the surrounding note (normally
            // .125 inch) without changing its base height or fixed values.
            if (plan.FullSizeStacks) Add(style, "ToleranceHeightRatio", 1.0);
        }
        void Add(object style, string name, object value) { before.Add(name, Property.Get(style, name)); after.Add(name, value); }
        internal void Apply(object style) { Write(style, after); }
        internal void Restore(object style) { Write(style, before); }
        internal void ApplyToSnapshot(StyleValues snapshot)
        { foreach (var item in after) snapshot.Values[item.Key] = item.Value; }
        static void Write(object style, Dictionary<string, object> values)
        {
            foreach (var item in values) if (!Property.Equal(Property.Get(style, item.Key), item.Value)) Property.Set(style, item.Key, item.Value);
            foreach (var item in values) if (!Property.Equal(Property.Get(style, item.Key), item.Value)) throw new InvalidOperationException("Unit setting did not persist: " + item.Key);
        }
    }

    // Read the running Solid Edge version's scalar formatting properties. Using
    // its type information avoids a partial comparison that could merge styles
    // with different fonts, tolerances, Smart Depth definitions or later-added
    // settings. Name and Parent identify a style, not its effective formatting.
    [ComImport, Guid("00020400-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IStyleDispatch
    {
        void GetTypeInfoCount(out uint count);
        void GetTypeInfo(uint index, uint locale, out ITypeInfo info);
    }
    internal sealed class StyleValues
    {
        internal readonly Dictionary<string, object> Values = new Dictionary<string, object>(StringComparer.Ordinal);
        static readonly Dictionary<Guid, string[]> names = new Dictionary<Guid, string[]>();
        static string[] Names(object style)
        {
            ITypeInfo info = null; IntPtr attr = IntPtr.Zero;
            try
            {
                ((IStyleDispatch)style).GetTypeInfo(0, 0, out info); info.GetTypeAttr(out attr);
                TYPEATTR type = (TYPEATTR)Marshal.PtrToStructure(attr, typeof(TYPEATTR));
                string[] result; if (names.TryGetValue(type.guid, out result)) return result;
                var gets = new HashSet<string>(); var puts = new HashSet<string>();
                for (int i = 0; i < type.cFuncs; i++)
                {
                    IntPtr pointer = IntPtr.Zero;
                    try
                    {
                        info.GetFuncDesc(i, out pointer);
                        FUNCDESC func = (FUNCDESC)Marshal.PtrToStructure(pointer, typeof(FUNCDESC));
                        string name, doc, help; int context;
                        info.GetDocumentation(func.memid, out name, out doc, out context, out help);
                        if (func.invkind == INVOKEKIND.INVOKE_PROPERTYGET && func.cParams == 0) gets.Add(name);
                        if (func.invkind == INVOKEKIND.INVOKE_PROPERTYPUT && func.cParams == 1) puts.Add(name);
                    }
                    finally { if (pointer != IntPtr.Zero) info.ReleaseFuncDesc(pointer); }
                }
                result = gets.Intersect(puts).Where(n => n != "Name" && n != "Parent").OrderBy(n => n, StringComparer.Ordinal).ToArray();
                if (result.Length < 100) throw new InvalidOperationException("Cannot inspect the complete dimension-style format.");
                names.Add(type.guid, result); return result;
            }
            finally { if (attr != IntPtr.Zero) info.ReleaseTypeAttr(attr); Com.Release(info); }
        }
        internal StyleValues(object style)
        {
            foreach (string name in Names(style))
            {
                object value;
                try { value = Property.Get(style, name); }
                catch (InvalidOperationException error)
                {
                    // The type library still advertises removed members such
                    // as AutoCenterLine. Omit only explicitly unavailable
                    // properties, on both sides of the comparison; transient
                    // and other errors must fail instead of allowing a match.
                    var com = error.GetBaseException() as COMException;
                    if (com != null && (com.ErrorCode == unchecked((int)0x80020003) || com.ErrorCode == unchecked((int)0x80004002))) continue;
                    throw;
                }
                if (value != null && !(value is string) && !value.GetType().IsValueType)
                { Com.Release(value); throw new InvalidOperationException("Unsupported style setting: " + name); }
                Values.Add(name, value);
            }
        }
        internal bool Matches(StyleValues other)
        { return Values.Count == other.Values.Count && Values.All(p => other.Values.ContainsKey(p.Key) && Property.Equal(p.Value, other.Values[p.Key])); }
        internal void Restore(object style)
        {
            foreach (var p in Values) if (!Property.Equal(Property.Get(style, p.Key), p.Value)) Property.Set(style, p.Key, p.Value);
            if (!Matches(new StyleValues(style))) throw new InvalidOperationException("Callout formatting changed.");
        }
    }

    internal sealed class NoteStyles : IDisposable
    {
        readonly object styles;
        readonly Dictionary<string, StyleValues> reusable = new Dictionary<string, StyleValues>(StringComparer.Ordinal);
        readonly HashSet<string> known = new HashSet<string>(StringComparer.Ordinal);
        readonly List<string> legacy = new List<string>();
        internal static bool IsLegacy(string name)
        { const string prefix = "Turbo Callout "; Guid id; return name != null && name.StartsWith(prefix, StringComparison.Ordinal) && Guid.TryParseExact(name.Substring(prefix.Length), "N", out id); }
        internal static bool IsManaged(string name)
        { return IsLegacy(name) || System.Text.RegularExpressions.Regex.IsMatch(name ?? "", @"^Turbo Callout - (Inch|Dual) \d+dp(?: \(\d+\))?$"); }
        internal NoteStyles(object draft)
        {
            styles = ((dynamic)draft).DimensionStyles;
            try
            {
                for (int i = 1; i <= (int)((dynamic)styles).Count; i++)
                {
                    object style = null;
                    try
                    {
                        style = ((dynamic)styles).Item(i); string name = (string)((dynamic)style).Name; known.Add(name);
                        if (IsLegacy(name)) legacy.Add(name);
                        else if (IsManaged(name)) reusable.Add(name, new StyleValues(style));
                    }
                    finally { Com.Release(style); }
                }
            }
            catch { Com.Release(styles); throw; }
        }
        // Native local setters reject some unit/precision changes. Configure a
        // temporary document style, then reuse a fully matching named style.
        // Only distinct complete formats remain; a GUID style never survives a
        // successful fill. Existing standard styles are never modified.
        static readonly string[] Presentation = { "Height", "TextWeight", "Font", "FontStyle", "TextOrientation", "TextPosition",
            "LimitTextArrangement", "ToleranceHeightRatio", "TerminatorType", "TerminatorDisplay", "OriginTerminatorType",
            "FreeSpaceTerminatorType", "TerminatorSize", "TerminatorInsideLimit", "LeaderLineLength", "TextClearanceGap",
            "DrivingColor", "DrivenColor", "ErrorColor", "TextAutoFillBackground", "SymbolFont", "PrimaryLeadingZeros",
            "PrimaryTrailingZeros", "SecondaryLeadingZeros", "SecondaryTrailingZeros", "PrimaryUnits", "SecondaryUnits",
            "PrimaryDecimalRoundOff", "SecondaryDecimalRoundOff" };
        object Find(string name)
        {
            // DimensionStyles.Item accepts a VARIANT but current Solid Edge
            // rejects style-name keys. Enumerate its supported numeric indices.
            for (int i = 1; i <= (int)((dynamic)styles).Count; i++)
            {
                object value = ((dynamic)styles).Item(i); bool found = false;
                try { found = (string)((dynamic)value).Name == name; if (found) return value; }
                finally { if (!found) Com.Release(value); }
            }
            throw new InvalidOperationException("Source dimension style not found: " + name);
        }
        internal string Acquire(object local, NotePlan plan, out bool created)
        {
            object candidate = null, source = null; string scratch = "Turbo Callout Temporary " + Guid.NewGuid().ToString("N");
            bool temporary = false; created = false; string step = "read source";
            try
            {
                string original = (string)((dynamic)local).Name;
                if (plan == null && reusable.ContainsKey(original)) return original;
                source = Find(original);
                var sourceFormat = new StyleValues(source);
                step = "create temporary style";
                candidate = ((dynamic)styles).Add(scratch, original); temporary = true;
                // Retain the original inheritance instead of making obsolete
                // GUID styles parents of the new shared styles.
                step = "retain original parent";
                try { Property.Set(candidate, "Parent", Property.Get(source, "Parent")); }
                catch (InvalidOperationException error)
                {
                    var com = error.GetBaseException() as COMException;
                    if (com == null || com.ErrorCode != unchecked((int)0x80040B54)) throw; // root style has no parent
                }
                // Assigning Parent can reset overrides to inherited values.
                // Restore the source's complete effective format before any
                // requested unit edits or pooling comparisons.
                sourceFormat.Restore(candidate);
                if (plan != null)
                {
                    step = "apply presentation and units";
                    // Existing local overrides are restored onto the note by
                    // Maintain. Keep them out of the shared style so one local
                    // height/font adjustment does not add another named style.
                    if (!plan.PreserveFormat)
                        foreach (string property in Presentation) Property.Set(candidate, property, Property.Get(local, property));
                    new UnitStyle(candidate, plan).Apply(candidate);
                }
                step = "compare complete style"; var format = new StyleValues(candidate);
                foreach (var entry in reusable) if (entry.Value.Matches(format)) return entry.Key;
                bool dual = (bool)Property.Get(candidate, "DualDisplay");
                int digits = Convert.ToInt32(Property.Get(candidate, dual ? "SecondaryDecimalRoundOff" : "PrimaryDecimalRoundOff")) - 2;
                string label = "Turbo Callout - " + (dual ? "Dual" : "Inch") + " " + digits + "dp", name = label; int suffix = 2;
                while (known.Contains(name)) name = label + " (" + suffix++ + ")";
                step = "name shared style"; ((dynamic)candidate).Name = name; temporary = false;
                known.Add(name); reusable.Add(name, format); created = true; return name;
            }
            catch (Exception error) { throw new InvalidOperationException("Cannot " + step + ": " + error.Message, error); }
            finally
            {
                try { if (temporary) ((dynamic)styles).Remove(scratch); }
                finally { Com.Release(source); Com.Release(candidate); }
            }
        }
        internal void RemoveCreated(string name)
        { ((dynamic)styles).Remove(name); reusable.Remove(name); known.Remove(name); }
        internal void Cleanup(RunReport report)
        {
            // Solid Edge refuses to remove an in-use dimension style. That
            // safeguard also protects dimensions, other annotation types,
            // background sheets, and style inheritance outside this scan.
            foreach (string name in known.Where(IsManaged).ToArray())
                try { ((dynamic)styles).Remove(name); report.StylesRemoved++; }
                catch (COMException) { if (legacy.Contains(name)) report.StylesKept++; }
            if (report.StylesKept > 0) report.Details.Add(report.StylesKept + " old styles are still in use and were retained.");
        }
        public void Dispose() { Com.Release(styles); }
    }

    internal sealed class RunReport
    {
        internal int Blank, Filled, AlignedExisting, UpdatedExisting, StylesRemoved, StylesKept, Review, Errors;
        internal int BoltCirclesCreated, BoltCirclesExisting, BoltCirclesSkipped, BoltCirclesReview;
        internal int ProjectionMarksCreated, ProjectionMarksUpdated;
        internal readonly List<string> Details = new List<string>();
        public override string ToString()
        {
            return String.Format("{0} blank callouts found.\r\n{1} filled; {4} existing notes aligned; {5} existing notes updated; {6} old styles removed; {2} need review; {3} errors.\r\n\r\nReview the drawing, then save it in Solid Edge.\r\n", Blank, Filled, Review, Errors, AlignedExisting, UpdatedExisting, StylesRemoved) +
                String.Format("Bolt circles: {0} created; {1} already present; {2} need review.\r\n",BoltCirclesCreated,BoltCirclesExisting,BoltCirclesReview)+
                String.Format("Projection marks: {0} added; {1} restored.\r\n",ProjectionMarksCreated,ProjectionMarksUpdated)+
                String.Join("\r\n", Details);
        }
    }

    internal static class Automation
    {
        internal static bool IsCallout(object note)
        { return (int)((dynamic)note).Callout != 0 && !(bool)((dynamic)note).DisplayByItemNumber && !(bool)((dynamic)note).LinkToPartsList; }
        internal static bool IsBlank(object note)
        {
            return IsCallout(note) &&
                new[] { "BalloonText", "BalloonTextLower", "BalloonTextPrefix", "BalloonTextSuffix" }
                    .All(name => String.IsNullOrWhiteSpace(Convert.ToString(Property.Get(note, name))));
        }

        internal static RunReport Run(bool dual, Action<string> progress)
        {
            object app = null, draft = null;
            try
            {
                try { app = Marshal.GetActiveObject("SolidEdge.Application"); }
                catch (COMException error) { throw new InvalidOperationException("Open Solid Edge and activate a draft before running Turbo Callout.", error); }
                draft = ((dynamic)app).ActiveDocument;
                if (draft == null || (int)((dynamic)draft).Type != 2) throw new InvalidOperationException("Activate a Solid Edge draft (.dft).");
                object setting; ((dynamic)app).GetGlobalParameter(63, out setting);
                string path = Convert.ToString(setting);
                if (String.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Configure Solid Edge's Custom Settings File first.");
                return FillDocument(draft, RuleBook.Load(path), dual, progress);
            }
            finally { Com.Release(draft); Com.Release(app); }
        }

        internal static RunReport FillDocument(object draft, RuleBook rules, bool dual, Action<string> progress)
        {
            if (draft == null || (int)((dynamic)draft).Type != 2) throw new InvalidOperationException("Activate a Solid Edge draft (.dft).");
            var report = new RunReport(); object sheets = null;
            using (var catalog = new NoteStyles(draft))
            try
            {
                sheets = ((dynamic)draft).Sheets;
                using (var reader = new NativeHole(draft))
                for (int si = 1; si <= (int)((dynamic)sheets).Count; si++)
                {
                    object sheet = null, notes = null;
                    try
                    {
                        sheet = ((dynamic)sheets).Item(si);
                        if ((int)((dynamic)sheet).SectionType != 0) continue; // only working sheets
                        string sheetName = (string)((dynamic)sheet).Name;
                        notes = ((dynamic)sheet).Balloons;
                        for (int ni = 1; ni <= (int)((dynamic)notes).Count; ni++)
                        {
                            object note = null; string label = sheetName + ", callout " + ni;
                            try
                            {
                                note = ((dynamic)notes).Item(ni); if (!IsCallout(note)) continue;
                                if (!IsBlank(note))
                                {
                                    if (NoteAlignment.IsExistingMacroNote(note))
                                    {
                                        string review;
                                        if (Maintain(note, catalog, rules, reader, out review)) { report.UpdatedExisting++; report.Details.Add(label + ": updated existing Turbo Callout note."); }
                                        if (review != null) { report.Review++; report.Details.Add(label + ": precision unchanged; " + review); }
                                        if (NoteAlignment.Apply(note)) { report.AlignedExisting++; report.Details.Add(label + ": aligned existing Turbo Callout note."); }
                                    }
                                    continue;
                                }
                                report.Blank++; if (progress != null) progress("Checking " + label + "...");
                                NotePlan plan; int? quantity;
                                try { plan = Notes.Build(rules, reader.Read(note, out quantity), dual); }
                                catch (Exception error) { report.Review++; report.Details.Add(label + " unchanged: " + error.Message); continue; }
                                Fill(note, plan, catalog, quantity);
                                report.Filled++; report.Details.Add(label + ": " + plan.Category);
                            }
                            catch (Exception error) { report.Errors++; report.Details.Add(label + ": " + error.Message); }
                            finally { Com.Release(note); }
                        }
                    }
                    catch (Exception error) { report.Errors++; report.Details.Add("Sheet " + si + ": " + error.Message); }
                    finally { Com.Release(notes); Com.Release(sheet); }
                }
                catalog.Cleanup(report);
            }
            finally { Com.Release(sheets); }
            AddBoltCircles(draft,report,progress);
            return report;
        }

        static void AddBoltCircles(object draft,RunReport report,Action<string> progress)
        {
            object sheets=null;
            using(var circles=new BoltCircles(draft))try
            {
                sheets=((dynamic)draft).Sheets;
                for(int si=1;si<=(int)((dynamic)sheets).Count;si++)
                {
                    object sheet=null,notes=null;
                    try
                    {
                        sheet=((dynamic)sheets).Item(si);if((int)((dynamic)sheet).SectionType!=0)continue;
                        notes=((dynamic)sheet).Balloons;
                        for(int ni=1;ni<=(int)((dynamic)notes).Count;ni++)
                        {
                            object note=null;string label=(string)((dynamic)sheet).Name+", callout "+ni;
                            try
                            {
                                note=((dynamic)notes).Item(ni);if(!IsCallout(note)||!(bool)((dynamic)note).IsTerminatorAttachedToEntity)continue;
                                if(progress!=null)progress("Checking bolt circle: "+label+"...");
                                string detail;BoltCircleResult result=circles.Ensure(note,sheet,out detail);
                                if(result==BoltCircleResult.Created)report.BoltCirclesCreated++;
                                else if(result==BoltCircleResult.Existing)report.BoltCirclesExisting++;
                                else if(result==BoltCircleResult.Review){report.BoltCirclesReview++;report.Review++;}
                                else report.BoltCirclesSkipped++;
                                if(detail!=null)report.Details.Add(label+": "+detail);
                            }
                            catch(Exception error){report.Errors++;report.Details.Add(label+": bolt circle unchanged; "+error.Message);}
                            finally{Com.Release(note);}
                        }
                    }
                    catch(Exception error){report.Errors++;report.Details.Add("Bolt-circle sheet "+si+": "+error.Message);}
                    finally{Com.Release(notes);Com.Release(sheet);}
                }
            }
            finally{report.ProjectionMarksCreated=circles.ProjectionMarksCreated;report.ProjectionMarksUpdated=circles.ProjectionMarksUpdated;Com.Release(sheets);}
        }

        internal static void Fill(object note, NotePlan plan)
        {
            object draft = null;
            try
            {
                draft = ((dynamic)note).Document; int? quantity;
                using (var reader = new NativeHole(draft)) reader.Read(note, out quantity);
                using (var catalog = new NoteStyles(draft)) Fill(note, plan, catalog, quantity);
            }
            finally { Com.Release(draft); }
        }

        static bool Maintain(object note, NoteStyles catalog, RuleBook rules, NativeHole reader, out string review)
        {
            review = null;
            object style = null; string name = null; bool created = false;
            try
            {
                style = ((dynamic)note).Style; string original = (string)((dynamic)style).Name;
                var format = new StyleValues(style); var position = new Placement(note);
                string[] fields = { "BalloonText", "BalloonTextLower", "BalloonTextPrefix", "BalloonTextSuffix" };
                var before = fields.ToDictionary(p => p, p => Convert.ToString(Property.Get(note, p)));
                var updated = before.ToDictionary(p => p.Key, p => Notes.ApplyWordSpacing(Notes.OmitSingleTypical(p.Value, null)));
                bool attached = (bool)((dynamic)note).IsTerminatorAttachedToEntity;
                // Existing unattached notes can still consolidate their styles,
                // but cannot acquire a quantity field with no model to count.
                NotePlan plan = null;
                var expected = new StyleValues(style);
                if (attached)
                {
                    // Read/recognize and prepare all four fields before changing
                    // the original note. Keep user wording and literal values;
                    // only native machining fields receive the new precision.
                    try
                    {
                        bool dual = (bool)Property.Get(style, "DualDisplay");
                        if ((int)Property.Get(style, "PrimaryUnits") != (dual ? 3 : 5) ||
                            (dual && (int)Property.Get(style, "SecondaryUnits") != 5))
                            throw new InvalidOperationException("Custom unit order needs review.");
                        int? quantity; HoleFacts hole = reader.Read(note, out quantity); Decision choice = Notes.Recognize(rules, hole);
                        string[] text = Notes.ApplyQuantity(fields.Select(field => Notes.ApplyPrecision(before[field], choice, hole, dual)).ToArray(), choice, quantity);
                        for (int i = 0; i < fields.Length; i++) updated[fields[i]] = Notes.ApplyWordSpacing(text[i]);
                        plan = new NotePlan { Dual = dual, InchPlaces = 2, PreserveFormat = true, FullSizeStacks = updated.Values.Any(Notes.HasFixedStacks) };
                        new UnitStyle(style, plan).ApplyToSnapshot(expected);
                    }
                    catch (Exception error) { review = error.Message; return false; }
                }
                try
                {
                    name = catalog.Acquire(style, plan, out created);
                    if (name == original && fields.All(p => updated[p] == before[p]) && format.Matches(expected)) return false;
                    ((dynamic)style).Name = name; expected.Restore(style);
                    foreach (string field in fields) if (updated[field] != before[field]) Property.Set(note, field, updated[field]);
                    position.Restore(note);
                    if (fields.Any(p => Convert.ToString(Property.Get(note, p)) != updated[p]))
                        throw new InvalidOperationException("Updated callout text did not persist.");
                    if (attached) VerifyEvaluation(note);
                    return true;
                }
                catch (Exception error)
                {
                    try
                    {
                        ((dynamic)style).Name = original; format.Restore(style);
                        foreach (string field in fields) Property.Set(note, field, before[field]);
                        position.Restore(note);
                        if (created) catalog.RemoveCreated(name);
                    }
                    catch (Exception rollback) { throw new InvalidOperationException("Update failed: " + error.Message + " Restoration needs review: " + rollback.Message, error); }
                    throw new InvalidOperationException("Existing note restored: " + error.Message, error);
                }
            }
            finally { Com.Release(style); }
        }

        internal static bool IsEvaluatedText(string text)
        {
            // BalloonDisplayedText leaves plain-text stack codes intact even
            // though GetDisplayData renders them correctly. Accept only the
            // numeric stacks we generate, never an unresolved property field.
            string plain = System.Text.RegularExpressions.Regex.Replace(text ?? "",
                @"%\{/ST[+\-]?(?:\d*\.)?\d+\^[+\-]?(?:\d*\.)?\d+\}", "0");
            return !String.IsNullOrWhiteSpace(plain) && !plain.Contains("%{") && !plain.Contains("?");
        }

        static void VerifyEvaluation(object note)
        {
            string displayed = (string)((dynamic)note).BalloonDisplayedText;
            if (!IsEvaluatedText(displayed))
                throw new InvalidOperationException("Model-linked callout fields did not evaluate.");
            if (!(bool)((dynamic)note).IsTerminatorAttachedToEntity) throw new InvalidOperationException("Leader attachment was lost.");
        }

        static void Fill(object note, NotePlan plan, NoteStyles catalog, int? quantity)
        {
            if (!IsBlank(note)) throw new InvalidOperationException("Callout is no longer blank; left unchanged.");
            object style = null; bool created = false; string createdName = null;
            try
            {
                var placement = new Placement(note);
                int? alignment = NoteAlignment.Target(note);
                style = ((dynamic)note).Style;
                var originalFormat = new StyleValues(style);
                string originalName = (string)((dynamic)style).Name;
                string before = (string)((dynamic)note).BalloonText;
                try
                {
                    createdName = catalog.Acquire(style, plan, out created);
                    ((dynamic)style).Name = createdName;
                    string text = Notes.OmitSingleTypical(plan.Text, quantity);
                    ((dynamic)note).BalloonText = text;
                    placement.Restore(note, alignment);
                    if ((string)((dynamic)note).BalloonText != text) throw new InvalidOperationException("Callout text was not retained.");
                    VerifyEvaluation(note);
                }
                catch (Exception error)
                {
                    try
                    {
                        ((dynamic)note).BalloonText = before; ((dynamic)style).Name = originalName; originalFormat.Restore(style); placement.Restore(note);
                        if ((string)((dynamic)note).BalloonText != before) throw new InvalidOperationException("Blank text could not be restored.");
                        if (created) catalog.RemoveCreated(createdName);
                    }
                    catch (Exception rollback) { throw new InvalidOperationException("Fill failed: " + error.Message + " Restoration needs review: " + rollback.Message, error); }
                    throw new InvalidOperationException("Left blank after failed verification: " + error.Message, error);
                }
            }
            finally { Com.Release(style); }
        }
    }
}
