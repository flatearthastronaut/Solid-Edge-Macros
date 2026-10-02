using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DualDimensionToggle;

// Runs only when explicitly invoked. Creates its own unsaved scratch draft,
// changes no existing document, closes the scratch and restores the active one.
internal static class LiveSmokeTests
{
    private static int assertions;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        assertions++;
    }
    [STAThread]
    public static int Main()
    {
        object app = null, original = null, docs = null, scratch = null, sheet = null;
        object styles = null, lines = null, dimensions = null, sheets = null, other = null;
        List<object> held = new List<object>();
        using (new OleMessageFilter())
        {
            try
            {
                app = Marshal.GetActiveObject("SolidEdge.Application");
                docs = ((dynamic)app).Documents;
                if ((int)((dynamic)docs).Count > 0) original = ((dynamic)app).ActiveDocument;
                scratch = ((dynamic)docs).Add("SolidEdge.DraftDocument");
                sheet = ((dynamic)scratch).ActiveSheet;
                styles = ((dynamic)scratch).DimensionStyles;
                object first = ((dynamic)styles).Item(1); held.Add(first);
                string parent = (string)((dynamic)first).Name;
                string[] names = { "1 2 place", "1 3 place", "2 2 place m[i]", "2 3 place m[i]", "3 3 place (vert)", "4 3 place m[i] (vert)", "Vert- 2 PLC", "Vert - 2PLC m[i]", "Vert- 3 PLC", "Vert - 3PLC m[i]" };
                HashSet<string> existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 1; i <= (int)((dynamic)styles).Count; i++)
                {
                    object entry = ((dynamic)styles).Item(i);
                    try { existing.Add((string)((dynamic)entry).Name); } finally { Com.Release(ref entry); }
                }
                foreach (string name in names)
                {
                    object definition = existing.Contains(name) ? ((dynamic)styles).Item(name) : ((dynamic)styles).Add(name, parent);
                    held.Add(definition);
                    bool dual = name.Contains("m[i]");
                    ((dynamic)definition).DualDisplay = dual;
                    ((dynamic)definition).PrimaryUnits = dual ? 3 : 5; // SDK: mm / inches
                    ((dynamic)definition).SecondaryUnits = 5;
                    int precision = StyleName.Parse(name).Places == 2 ? 4 : 5; // SDK: .2 / .3
                    ((dynamic)definition).PrimaryDecimalRoundOff = precision;
                    ((dynamic)definition).SecondaryDecimalRoundOff = precision;
                }
                lines = ((dynamic)sheet).Lines2d;
                dimensions = ((dynamic)sheet).Dimensions;
                string[] sources = { "2 2 place m[i]", "2 3 place m[i]", "4 3 place m[i] (vert)", "Vert - 2PLC m[i]", "Vert - 3PLC m[i]" };
                List<object> dimStyles = new List<object>();
                for (int i = 0; i < sources.Length; i++)
                {
                    object line = ((dynamic)lines).AddBy2Points(0.02, 0.02 + i * 0.04, 0.123456, 0.02 + i * 0.04); held.Add(line);
                    object dim = ((dynamic)dimensions).AddLength(line); held.Add(dim);
                    object style = ((dynamic)dim).Style; held.Add(style); dimStyles.Add(style);
                    ((dynamic)style).Name = sources[i];
                }
                // Real angular dimensions sharing convertible style names must
                // retain their names and local angular precision in both modes.
                List<object> angularStyles = new List<object>();
                string[] angularNames = { "Vert - 3PLC m[i]", "1 3 place" };
                for (int i = 0; i < angularNames.Length; i++)
                {
                    object line = ((dynamic)lines).AddBy2Points(0.02, 0.15 + i * 0.04, 0.08, 0.18 + i * 0.04); held.Add(line);
                    object angle = ((dynamic)dimensions).AddAngle(line); held.Add(angle);
                    Check((int)((dynamic)angle).DimensionType == 3, "Real angular dimension created");
                    object style = ((dynamic)angle).Style; held.Add(style); angularStyles.Add(style);
                    ((dynamic)style).Name = angularNames[i];
                    ((dynamic)style).AngularDecimalRoundOff = 4;
                }
                // Real Feature Control Frames verify symbol serialization and
                // row setters, in addition to the pure parser regression suite.
                object frames = ((dynamic)sheet).FeatureControlFrames; held.Add(frames);
                object stacked = ((dynamic)frames).Add(0.03, 0.26, 0.0); held.Add(stacked);
                string[] dualRows = { "%PO%VB.03[.001]%VBA%VBB", "%PO%VB%DI.013[.0005]%MC%VBA1",
                    "%PO%VB.025[.0010]%VBA", "%PO%VB.64[.025]%VBB" };
                string[] singleRows = { "%PO%VB.001%VBA%VBB", "%PO%VB%DI.0005%MC%VBA1",
                    "%PO%VB.0010%VBA", "%PO%VB.025%VBB" };
                ((dynamic)stacked).PrimaryFrame = dualRows[0];
                ((dynamic)stacked).SecondaryFrame = dualRows[1];
                ((dynamic)stacked).TertiaryFrame = dualRows[2];
                ((dynamic)stacked).QuaternaryFrame = dualRows[3];
                // The projected-height property aliases TertiaryFrame, so use
                // a separate object to test preservation of that optional text.
                object projectedFrame = ((dynamic)frames).Add(0.15, 0.22, 0.0); held.Add(projectedFrame);
                ((dynamic)projectedFrame).PrimaryFrame = dualRows[0];
                ((dynamic)projectedFrame).ProjectedToleranceFrame = "%PT.5";
                string projected = (string)((dynamic)projectedFrame).ProjectedToleranceFrame;
                bool composite = (bool)((dynamic)stacked).CompositeFrame;
                object singleFrame = ((dynamic)frames).Add(0.15, 0.26, 0.0); held.Add(singleFrame);
                ((dynamic)singleFrame).PrimaryFrame = "%PO%VB.001%VBA%VBB";
                object notes = ((dynamic)sheet).Balloons; held.Add(notes);
                object note = ((dynamic)notes).Add(0.10, 0.16, 0.0); held.Add(note);
                ((dynamic)note).Callout = 1;
                string noteDual = "%DI %{%HS/DU} DRILL %ZH\r%DI 6.50/6.35[.256/.250] DIA %{%BD/DU} DP.\r(1)PLC AS SHOWN\r(FOR 1/4 SPRING PIN)";
                string noteInch = noteDual.Replace("6.50/6.35[.256/.250]", ".256/.250").Replace("%{%HS/DU}", "%HS").Replace("%{%BD/DU}", "%BD");
                ((dynamic)note).BalloonText = noteDual;
                object noteStyle = ((dynamic)note).Style; held.Add(noteStyle);
                ((dynamic)noteStyle).Name = "2 2 place m[i]";
                object styleNote = ((dynamic)notes).Add(0.12, 0.13, 0.0); held.Add(styleNote);
                ((dynamic)styleNote).Callout = 1;
                ((dynamic)styleNote).BalloonText = "%TS THR'D %{%TD/DU/@3/ST+.001^-.002} DP.";
                object styleNoteStyle = ((dynamic)styleNote).Style; held.Add(styleNoteStyle);
                ((dynamic)styleNoteStyle).Name = "2 2 place m[i]";
                object counterbore = ((dynamic)notes).Add(0.12, 0.11, 0.0); held.Add(counterbore);
                ((dynamic)counterbore).Callout = 1;
                const string cbDual = "%DI %{%HS/DU} DRILL %ZH\r%DI %{%BS/DU} C'BORE %{%BD/DU} DP.\r(%QC)PLC'S EQ. SP. AS SHOWN\r(FOR X S.H.C.S.)";
                const string cbInch = "%DI %HS DRILL %ZH\r%DI %BS C'BORE %BD DP.\r(%QC)PLC'S EQ. SP. AS SHOWN\r(FOR X S.H.C.S.)";
                ((dynamic)counterbore).BalloonText = cbDual;
                object cbStyle = ((dynamic)counterbore).Style; held.Add(cbStyle);
                ((dynamic)cbStyle).Name = "2 2 place m[i]";
                object angleNote = ((dynamic)notes).Add(0.15, 0.10, 0.0); held.Add(angleNote);
                ((dynamic)angleNote).Callout = 1; ((dynamic)angleNote).BalloonText = "45%DG";
                object angleNoteStyle = ((dynamic)angleNote).Style; held.Add(angleNoteStyle);
                ((dynamic)angleNoteStyle).Name = "2 3 place m[i]";
                // A dimension on a second working sheet must remain unchanged.
                sheets = ((dynamic)scratch).Sheets;
                other = ((dynamic)sheets).AddSheet("Scope sentinel");
                object otherLines = ((dynamic)other).Lines2d; held.Add(otherLines);
                object otherDims = ((dynamic)other).Dimensions; held.Add(otherDims);
                object otherLine = ((dynamic)otherLines).AddBy2Points(0.02, 0.02, 0.12, 0.02); held.Add(otherLine);
                object otherDim = ((dynamic)otherDims).AddLength(otherLine); held.Add(otherDim);
                object otherStyle = ((dynamic)otherDim).Style; held.Add(otherStyle);
                ((dynamic)otherStyle).Name = sources[0];
                object otherFrames = ((dynamic)other).FeatureControlFrames; held.Add(otherFrames);
                object otherFrame = ((dynamic)otherFrames).Add(0.03, 0.16, 0.0); held.Add(otherFrame);
                ((dynamic)otherFrame).PrimaryFrame = dualRows[0];
                object otherNotes = ((dynamic)other).Balloons; held.Add(otherNotes);
                object otherNote = ((dynamic)otherNotes).Add(0.12, 0.10, 0.0); held.Add(otherNote);
                ((dynamic)otherNote).Callout = 1; ((dynamic)otherNote).BalloonText = noteDual;
                ((dynamic)sheet).Activate();

                ConversionReport report = Converter.ConvertDocument(scratch, false);
                Console.WriteLine(report);
                Check(report.CalloutsChanged == 3 && report.CalloutStylesChanged == 3 && report.CalloutValuesChanged == 2 && report.CalloutsFailed == 0, "Callout style and explicit range convert to inch");
                Check((string)((dynamic)note).BalloonText == noteInch, "Callout linked fields and surrounding text retained");
                Check((string)((dynamic)noteStyle).Name == "1 2 place", "Callout inch style");
                Check(CalloutHistoryStore.Read(note) != null, "Known pair record stored on actual annotation");
                Check((string)((dynamic)styleNote).BalloonText == "%TS THR'D %{%TD/@3/ST+.001^-.002} DP.", "Thread depth unit format changed");
                Check((string)((dynamic)counterbore).BalloonText == cbInch, "Exact user inch counterbore sample");
                Check(CalloutHistoryStore.Read(counterbore) == null, "Linked-only note needs no metadata");
                Check((string)((dynamic)angleNoteStyle).Name == "2 3 place m[i]", "Angle callout unchanged");
                Check((string)((dynamic)otherNote).BalloonText == noteDual, "Other sheet callout unchanged");
                Check(report.Changed == sources.Length && report.Failed == 0, "All actual dimensions must convert to inches");
                Check(report.FramesChanged == 2 && report.FrameRowsChanged == 5 && report.FramesFailed == 0, "Stacked and projected FCF tolerances convert to inches");
                Check((string)((dynamic)stacked).PrimaryFrame == singleRows[0], "Primary FCF inch text");
                Check((string)((dynamic)stacked).SecondaryFrame == singleRows[1], "Secondary FCF symbols and datum preserved");
                Check((string)((dynamic)stacked).TertiaryFrame == singleRows[2], "Tertiary FCF trailing zero preserved");
                Check((string)((dynamic)stacked).QuaternaryFrame == singleRows[3], "Quaternary FCF inch text");
                Check((string)((dynamic)projectedFrame).ProjectedToleranceFrame == projected, "Projection text unchanged");
                Check((bool)((dynamic)stacked).CompositeFrame == composite, "Composite setting unchanged");
                Check((string)((dynamic)singleFrame).PrimaryFrame == singleRows[0], "Single FCF already in inch format");
                Check((string)((dynamic)otherFrame).PrimaryFrame == dualRows[0], "Other sheet FCF unchanged");
                Check(report.AngularSkipped == 2, "Both angles skipped during inch conversion");
                for (int i = 0; i < angularStyles.Count; i++)
                {
                    Check((string)((dynamic)angularStyles[i]).Name == angularNames[i], "Angular name retained during inch conversion");
                    Check((int)((dynamic)angularStyles[i]).AngularDecimalRoundOff == 4, "Angular precision retained during inch conversion");
                }
                string[] targets = { "1 2 place", "1 3 place", "3 3 place (vert)", "Vert- 2 PLC", "Vert- 3 PLC" };
                for (int i = 0; i < sources.Length; i++)
                {
                    dynamic style = dimStyles[i];
                    Check((string)style.Name == targets[i], "Inch style name " + i);
                    Check(!(bool)style.DualDisplay && (int)style.PrimaryUnits == 5, "Inch-only display " + i);
                    Check((int)style.PrimaryDecimalRoundOff == (StyleName.Parse(sources[i]).Places == 2 ? 4 : 5), "Inch decimal precision " + i);
                }
                Check((string)((dynamic)otherStyle).Name == sources[0], "Other sheet must be untouched");
                Check(Converter.ConvertDocument(scratch, false).Changed == 0, "Repeat must be idempotent");
                ((dynamic)counterbore).BalloonText = cbDual;
                report = Converter.ConvertDocument(scratch, false);
                Check(report.CalloutsChanged == 1 && report.CalloutStylesChanged == 0 && report.CalloutsFailed == 0, "Repair DU fields even when style already inch");
                Check((string)((dynamic)counterbore).BalloonText == cbInch, "Field-only repair retained");
                report = Converter.ConvertDocument(scratch, true);
                Console.WriteLine(report);
                Check(report.CalloutsChanged == 3 && report.CalloutValuesChanged == 2 && report.CalloutsFailed == 0, "Known callout pairs restored to dual");
                Check((string)((dynamic)counterbore).BalloonText == cbDual, "Exact user dual counterbore sample");
                Check((string)((dynamic)styleNote).BalloonText == "%TS THR'D %{%TD/DU/@3/ST+.001^-.002} DP.", "Thread depth dual format restored");
                Check((string)((dynamic)note).BalloonText == noteDual, "Callout range roundtrip");
                Check((string)((dynamic)noteStyle).Name == "2 2 place m[i]", "Callout dual style");
                Check(CalloutHistoryStore.Read(note) == null, "Completed pair record removed");
                Check(Converter.ConvertDocument(scratch, true).CalloutsChanged == 0, "Repeated callout dual conversion idempotent");
                Check(report.Changed == sources.Length && report.Failed == 0, "All actual dimensions must convert to dual");
                Check(report.FramesChanged == 3 && report.FrameRowsChanged == 6 && report.FramesFailed == 0, "Single, stacked and projected FCFs convert to dual");
                Check((string)((dynamic)stacked).PrimaryFrame == dualRows[0], "Primary FCF metric rounding");
                Check((string)((dynamic)stacked).SecondaryFrame == dualRows[1], "Secondary FCF metric rounding");
                Check((string)((dynamic)stacked).TertiaryFrame == dualRows[2], "Tertiary FCF metric precision");
                Check((string)((dynamic)stacked).QuaternaryFrame == dualRows[3], "Quaternary FCF midpoint rounding");
                Check((string)((dynamic)singleFrame).PrimaryFrame == dualRows[0], "Single FCF produces user example");
                Check((string)((dynamic)projectedFrame).ProjectedToleranceFrame == projected, "Projection text unchanged after reverse conversion");
                Check((bool)((dynamic)stacked).CompositeFrame == composite, "Composite setting unchanged after reverse conversion");
                Check(Converter.ConvertDocument(scratch, true).FramesChanged == 0, "Repeated dual FCF conversion is idempotent");
                Check(report.AngularSkipped == 2, "Both angles skipped during dual conversion");
                for (int i = 0; i < angularStyles.Count; i++)
                {
                    Check((string)((dynamic)angularStyles[i]).Name == angularNames[i], "Angular name retained during dual conversion");
                    Check((int)((dynamic)angularStyles[i]).AngularDecimalRoundOff == 4, "Angular precision retained during dual conversion");
                }
                for (int i = 0; i < sources.Length; i++)
                {
                    dynamic style = dimStyles[i];
                    Check((string)style.Name == sources[i], "Dual style name " + i);
                    Check((bool)style.DualDisplay, "Dual display " + i);
                    Check((int)style.SecondaryDecimalRoundOff == (StyleName.Parse(sources[i]).Places == 2 ? 4 : 5), "Dual inch precision " + i);
                }
                assertions += LiveCalloutHistory.Run(app);
                Console.WriteLine("PASS: " + assertions + " live Solid Edge assertions in disposable drafts.");
                return 0;
            }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
            finally
            {
                for (int i = held.Count - 1; i >= 0; i--) { object acquired = held[i]; Com.Release(ref acquired); }
                Com.Release(ref other); Com.Release(ref sheets); Com.Release(ref dimensions);
                Com.Release(ref lines); Com.Release(ref styles); Com.Release(ref sheet);
                try { if (scratch != null) ((dynamic)scratch).Close(false); }
                finally
                {
                    Com.Release(ref scratch);
                    try { if (original != null) ((dynamic)original).Activate(); }
                    finally { Com.Release(ref original); Com.Release(ref docs); Com.Release(ref app); }
                }
            }
        }
    }
}
