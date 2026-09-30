using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DraftDimensionStyles;

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
                string[] names = { "1 2 place", "1 3 place", "2 2 place m[i]", "2 3 place m[i]", "3 3 place (vert)", "4 3 place m[i] (vert)" };
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
                    int precision = name.Contains("2 place") ? 4 : 5; // SDK: .2 / .3
                    ((dynamic)definition).PrimaryDecimalRoundOff = precision;
                    ((dynamic)definition).SecondaryDecimalRoundOff = precision;
                }
                lines = ((dynamic)sheet).Lines2d;
                dimensions = ((dynamic)sheet).Dimensions;
                string[] sources = { "2 2 place m[i]", "2 3 place m[i]", "4 3 place m[i] (vert)" };
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
                string[] angularNames = { "2 3 place m[i]", "1 3 place" };
                for (int i = 0; i < angularNames.Length; i++)
                {
                    object line = ((dynamic)lines).AddBy2Points(0.02, 0.15 + i * 0.04, 0.08, 0.18 + i * 0.04); held.Add(line);
                    object angle = ((dynamic)dimensions).AddAngle(line); held.Add(angle);
                    Check((int)((dynamic)angle).DimensionType == 3, "Real angular dimension created");
                    object style = ((dynamic)angle).Style; held.Add(style); angularStyles.Add(style);
                    ((dynamic)style).Name = angularNames[i];
                    ((dynamic)style).AngularDecimalRoundOff = 4;
                }
                // A dimension on a second working sheet must remain unchanged.
                sheets = ((dynamic)scratch).Sheets;
                other = ((dynamic)sheets).AddSheet("Scope sentinel");
                object otherLines = ((dynamic)other).Lines2d; held.Add(otherLines);
                object otherDims = ((dynamic)other).Dimensions; held.Add(otherDims);
                object otherLine = ((dynamic)otherLines).AddBy2Points(0.02, 0.02, 0.12, 0.02); held.Add(otherLine);
                object otherDim = ((dynamic)otherDims).AddLength(otherLine); held.Add(otherDim);
                object otherStyle = ((dynamic)otherDim).Style; held.Add(otherStyle);
                ((dynamic)otherStyle).Name = sources[0];
                ((dynamic)sheet).Activate();

                ConversionReport report = Converter.ConvertDocument(scratch, false);
                Console.WriteLine(report);
                Check(report.Changed == 3 && report.Failed == 0, "Three actual dimensions must convert to inches");
                Check(report.AngularSkipped == 2, "Both angles skipped during inch conversion");
                for (int i = 0; i < angularStyles.Count; i++)
                {
                    Check((string)((dynamic)angularStyles[i]).Name == angularNames[i], "Angular name retained during inch conversion");
                    Check((int)((dynamic)angularStyles[i]).AngularDecimalRoundOff == 4, "Angular precision retained during inch conversion");
                }
                string[] targets = { "1 2 place", "1 3 place", "3 3 place (vert)" };
                for (int i = 0; i < 3; i++)
                {
                    dynamic style = dimStyles[i];
                    Check((string)style.Name == targets[i], "Inch style name " + i);
                    Check(!(bool)style.DualDisplay && (int)style.PrimaryUnits == 5, "Inch-only display " + i);
                    Check((int)style.PrimaryDecimalRoundOff == (i == 0 ? 4 : 5), "Inch decimal precision " + i);
                }
                Check((string)((dynamic)otherStyle).Name == sources[0], "Other sheet must be untouched");
                Check(Converter.ConvertDocument(scratch, false).Changed == 0, "Repeat must be idempotent");
                report = Converter.ConvertDocument(scratch, true);
                Console.WriteLine(report);
                Check(report.Changed == 3 && report.Failed == 0, "Three actual dimensions must convert to dual");
                Check(report.AngularSkipped == 2, "Both angles skipped during dual conversion");
                for (int i = 0; i < angularStyles.Count; i++)
                {
                    Check((string)((dynamic)angularStyles[i]).Name == angularNames[i], "Angular name retained during dual conversion");
                    Check((int)((dynamic)angularStyles[i]).AngularDecimalRoundOff == 4, "Angular precision retained during dual conversion");
                }
                for (int i = 0; i < 3; i++)
                {
                    dynamic style = dimStyles[i];
                    Check((string)style.Name == sources[i], "Dual style name " + i);
                    Check((bool)style.DualDisplay, "Dual display " + i);
                    Check((int)style.SecondaryDecimalRoundOff == (i == 0 ? 4 : 5), "Dual inch precision " + i);
                }
                Console.WriteLine("PASS: " + assertions + " live Solid Edge assertions in a disposable draft.");
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
