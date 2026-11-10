using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
            return direction == 0 ? (int?)2 : direction == 2 ? (int?)0 : null;
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
                // All released versions assign this private GUID style. Limit
                // rerun repairs to our notes; keep other existing notes alone.
                style = ((dynamic)note).Style;
                string name = (string)((dynamic)style).Name;
                const string prefix = "Turbo Callout "; Guid id;
                return name != null && name.StartsWith(prefix, StringComparison.Ordinal) &&
                    Guid.TryParseExact(name.Substring(prefix.Length), "N", out id);
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
            { double vx, vy, vz; ((dynamic)note).GetVertex(i, out vx, out vy, out vz); vertices.Add(new[] { vx, vy, vz }); }
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
                ((dynamic)note).GetVertex(i, out ax, out ay, out az); double[] v = vertices[i];
                if (!Property.Equal(v[0], ax) || !Property.Equal(v[1], ay) || !Property.Equal(v[2], az)) ((dynamic)note).SetVertex(i, v[0], v[1], v[2]);
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
                ((dynamic)note).GetVertex(i, out ax, out ay, out az); double[] v = vertices[i];
                if (!Property.Equal(v[0], ax) || !Property.Equal(v[1], ay) || !Property.Equal(v[2], az)) throw new InvalidOperationException("Leader position changed.");
            }
        }
    }

    internal sealed class UnitStyle
    {
        readonly Dictionary<string, object> before = new Dictionary<string, object>();
        readonly Dictionary<string, object> after = new Dictionary<string, object>();
        internal UnitStyle(object style, NotePlan plan)
        {
            int metric = Convert.ToInt32(Property.Get(style, "PrimaryUnits")) == 3 ? Convert.ToInt32(Property.Get(style, "PrimaryDecimalRoundOff")) :
                Convert.ToInt32(Property.Get(style, "SecondaryUnits")) == 3 ? Convert.ToInt32(Property.Get(style, "SecondaryDecimalRoundOff")) : 4;
            // Enum decimal precision is digits+2. Preserve the existing metric
            // precision independently; use the audited two-place metric default
            // only if the blank annotation has no metric unit configured.
            Add(style, "PrimaryUnits", plan.Dual ? 3 : 5);
            Add(style, "RoundOffType", 1);
            Add(style, "PrimaryDecimalRoundOff", plan.Dual ? metric : plan.InchPlaces + 2);
            Add(style, "PrimaryTrailingZeros", true);
            Add(style, "DualDisplay", plan.Dual);
            Add(style, "PrimaryLinearToleranceUnits", plan.Dual ? 3 : 5);
            Add(style, "PrimaryLinearToleranceRoundOffType", 1);
            Add(style, "PrimaryLinearToleranceDecimalRoundOff", plan.Dual ? metric : plan.InchPlaces + 2);
            if (plan.Dual)
            {
                Add(style, "SecondaryUnits", 5); Add(style, "SecondaryUnitRoundOff", 1);
                Add(style, "SecondaryDecimalRoundOff", plan.InchPlaces + 2); Add(style, "SecondaryTrailingZeros", true);
                Add(style, "SecondaryLinearToleranceUnits", 5); Add(style, "SecondaryLinearToleranceRoundOffType", 1);
                Add(style, "SecondaryLinearToleranceDecimalRoundOff", plan.InchPlaces + 2);
                Add(style, "SecondaryToleranceEquivalent", true); Add(style, "SecondaryUnitSeparator", 2);
            }
            Add(style, "SmartDepthHoleThru", "THRU"); Add(style, "SmartDepthThreadThru", "THRU");
            Add(style, "SmartDepthHoleFinite", plan.Dual ? "%{%HD/DU} DP." : "%HD DP.");
            Add(style, "SmartDepthThreadFinite", plan.Dual ? "%{%TD/DU} DP." : "%TD DP.");
        }
        void Add(object style, string name, object value) { before.Add(name, Property.Get(style, name)); after.Add(name, value); }
        internal void Apply(object style) { Write(style, after); }
        internal void Restore(object style) { Write(style, before); }
        static void Write(object style, Dictionary<string, object> values)
        {
            foreach (var item in values) if (!Property.Equal(Property.Get(style, item.Key), item.Value)) Property.Set(style, item.Key, item.Value);
            foreach (var item in values) if (!Property.Equal(Property.Get(style, item.Key), item.Value)) throw new InvalidOperationException("Unit setting did not persist: " + item.Key);
        }
    }

    internal static class NoteStyles
    {
        // Balloon.Style exposes some format setters that Solid Edge rejects.
        // Make a private document style, then assign its name to the annotation.
        // The source style and every other annotation remain untouched. Copy
        // local presentation overrides before changing units on the new style.
        static readonly string[] Presentation = { "Height", "TextWeight", "Font", "FontStyle", "TextOrientation", "TextPosition",
            "LimitTextArrangement", "ToleranceHeightRatio", "TerminatorType", "TerminatorDisplay", "OriginTerminatorType",
            "FreeSpaceTerminatorType", "TerminatorSize", "TerminatorInsideLimit", "LeaderLineLength", "TextClearanceGap",
            "DrivingColor", "DrivenColor", "ErrorColor", "TextAutoFillBackground", "SymbolFont", "PrimaryLeadingZeros",
            "PrimaryTrailingZeros", "SecondaryLeadingZeros", "SecondaryTrailingZeros", "PrimaryUnits", "SecondaryUnits",
            "PrimaryDecimalRoundOff", "SecondaryDecimalRoundOff" };
        internal static object Create(object note, object local, NotePlan plan, out object styles, out string name)
        {
            object doc = null, created = null; styles = null; name = "Turbo Callout " + Guid.NewGuid().ToString("N");
            try
            {
                doc = ((dynamic)note).Document; styles = ((dynamic)doc).DimensionStyles;
                created = ((dynamic)styles).Add(name, (string)((dynamic)local).Name);
                foreach (string property in Presentation) Property.Set(created, property, Property.Get(local, property));
                new UnitStyle(created, plan).Apply(created);
                return created;
            }
            catch
            {
                Com.Release(created);
                if (styles != null) { try { ((dynamic)styles).Remove(name); } catch { } }
                throw;
            }
            finally { Com.Release(doc); }
        }
    }

    internal sealed class RunReport
    {
        internal int Blank, Filled, AlignedExisting, Review, Errors;
        internal readonly List<string> Details = new List<string>();
        public override string ToString()
        {
            return String.Format("{0} blank callouts found.\r\n{1} filled; {4} existing notes aligned; {2} need review; {3} errors.\r\n\r\nReview the drawing, then save it in Solid Edge.\r\n", Blank, Filled, Review, Errors, AlignedExisting) +
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
                                    if (NoteAlignment.IsExistingMacroNote(note) && NoteAlignment.Apply(note))
                                    { report.AlignedExisting++; report.Details.Add(label + ": aligned existing Turbo Callout note."); }
                                    continue;
                                }
                                report.Blank++; if (progress != null) progress("Checking " + label + "...");
                                NotePlan plan;
                                try { plan = Notes.Build(rules, reader.Read(note), dual); }
                                catch (Exception error) { report.Review++; report.Details.Add(label + " unchanged: " + error.Message); continue; }
                                Fill(note, plan);
                                report.Filled++; report.Details.Add(label + ": " + plan.Category);
                            }
                            catch (Exception error) { report.Errors++; report.Details.Add(label + ": " + error.Message); }
                            finally { Com.Release(note); }
                        }
                    }
                    catch (Exception error) { report.Errors++; report.Details.Add("Sheet " + si + ": " + error.Message); }
                    finally { Com.Release(notes); Com.Release(sheet); }
                }
            }
            finally { Com.Release(sheets); }
            return report;
        }

        internal static void Fill(object note, NotePlan plan)
        {
            if (!IsBlank(note)) throw new InvalidOperationException("Callout is no longer blank; left unchanged.");
            object style = null, styles = null, created = null;
            string createdName = null;
            try
            {
                var placement = new Placement(note);
                int? alignment = NoteAlignment.Target(note);
                style = ((dynamic)note).Style;
                string originalName = (string)((dynamic)style).Name;
                string before = (string)((dynamic)note).BalloonText;
                try
                {
                    created = NoteStyles.Create(note, style, plan, out styles, out createdName);
                    ((dynamic)style).Name = createdName;
                    ((dynamic)note).BalloonText = plan.Text;
                    placement.Restore(note, alignment);
                    if ((string)((dynamic)note).BalloonText != plan.Text) throw new InvalidOperationException("Callout text was not retained.");
                    string displayed = (string)((dynamic)note).BalloonDisplayedText;
                    if (String.IsNullOrWhiteSpace(displayed) || displayed.Contains("%{") || displayed.Contains("?"))
                        throw new InvalidOperationException("Model-linked callout fields did not evaluate.");
                    if (!(bool)((dynamic)note).IsTerminatorAttachedToEntity) throw new InvalidOperationException("Leader attachment was lost.");
                }
                catch (Exception error)
                {
                    try
                    {
                        ((dynamic)note).BalloonText = before; ((dynamic)style).Name = originalName; placement.Restore(note);
                        if ((string)((dynamic)note).BalloonText != before) throw new InvalidOperationException("Blank text could not be restored.");
                        if (created != null) ((dynamic)styles).Remove(createdName);
                    }
                    catch (Exception rollback) { throw new InvalidOperationException("Fill failed: " + error.Message + " Restoration needs review: " + rollback.Message, error); }
                    throw new InvalidOperationException("Left blank after failed verification: " + error.Message, error);
                }
            }
            finally { Com.Release(created); Com.Release(styles); Com.Release(style); }
        }
    }
}
