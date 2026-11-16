using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Hammill.DimensionStyleCycle;
using Hammill.MacroLauncher;

// Public fakes expose the same dispatch properties as native COM objects.
public sealed class CycleTestStyle
{
    private string name;
    public string Reject, Ignore;
    public Action Changing;
    public int Writes;
    public CycleTestStyle(string value) { name = value; PrimaryUnits = 5; SecondaryUnits = 5; PrimaryLinearToleranceUnits = 5; PrimaryDecimalRoundOff = 4; SecondaryDecimalRoundOff = 4; RoundOffType = 1; SecondaryLinearRoundOffType = 1; }
    public string Name { get { return name; } set { Writes++; if (value == Ignore) return; name = value; if (Changing != null) Changing(); if (value == Reject) throw new InvalidOperationException("Simulated failure after mutation"); } }
    public bool DualDisplay { get; set; }
    public int PrimaryUnits { get; set; }
    public int SecondaryUnits { get; set; }
    public int TextOrientation { get; set; }
    public int PrimaryLinearToleranceUnits { get; set; }
    public bool SecondaryToleranceEquivalent { get; set; }
    public int PrimaryDecimalRoundOff { get; set; }
    public int SecondaryDecimalRoundOff { get; set; }
    public int RoundOffType { get; set; }
    public int SecondaryLinearRoundOffType { get; set; }
}
public sealed class CycleTestStyles
{
    public readonly Dictionary<string, CycleTestStyle> Values = new Dictionary<string, CycleTestStyle>(StringComparer.OrdinalIgnoreCase);
    public int ItemReads;
    public object Item(int index) { ItemReads++; return Values.Values.ElementAt(index - 1); }
    public int Count { get { return Values.Count; } }
}
public sealed class CycleTestSelection
{
    public object[] Values;
    public int Count { get { return Values.Length; } }
    public object Item(int index) { return Values[index - 1]; }
}
public sealed class CycleTestDimension
{
    public CycleTestDimension() { Type = CycleEngine.DimensionObjectType; DimensionType = 1; PrefixString = "PREFIX"; SuffixString = " B.C"; SubfixString = "TYP(4)"; SuperfixString = "TOP"; SubfixString2 = "BOTTOM"; OverrideString = ""; PrimaryUpperTolerance = "-.002"; PrimaryLowerTolerance = "-.006"; PrimaryToleranceSymbol = ""; SecondaryUpperTolerance = "+.01"; SecondaryLowerTolerance = "-.02"; SecondaryToleranceSymbol = ""; DisplayType = 2; TrackDistance = .023; TrackAngle = 1.5; BreakPosition = 1; BreakDistance = .007; }
    public int Type { get; set; }
    public int DimensionType { get; set; }
    public double Value { get { return .0127; } }
    public CycleTestStyle Style { get; set; }
    public string PrefixString { get; set; }
    public string SuffixString { get; set; }
    public string SubfixString { get; set; }
    public string SuperfixString { get; set; }
    public string SubfixString2 { get; set; }
    public string OverrideString { get; set; }
    private string upper, lower;
    public Action ToleranceChanging;
    public bool IgnoreUpper;
    public decimal NumericUpper, NumericLower;
    // SE only recalculates these numeric offsets when text is assigned in
    // UnitTolerance mode. A test that only checked strings would miss this bug.
    public string PrimaryUpperTolerance { get { return upper; } set { if (IgnoreUpper) return; upper = value; if (DisplayType == 15) LimitToleranceCycle.TryInches(value, Style.PrimaryLinearToleranceUnits, out NumericUpper); } }
    public string PrimaryLowerTolerance { get { return lower; } set { lower = value; if (DisplayType == 15) LimitToleranceCycle.TryInches(value, Style.PrimaryLinearToleranceUnits, out NumericLower); if (ToleranceChanging != null) ToleranceChanging(); } }
    public bool UseStandardLimitAndFitTolerance { get; set; }
    public string PrimaryToleranceSymbol { get; set; }
    public string SecondaryUpperTolerance { get; set; }
    public string SecondaryLowerTolerance { get; set; }
    public string SecondaryToleranceSymbol { get; set; }
    public int DisplayType { get; set; }
    public double TrackDistance { get; set; }
    public double TrackAngle { get; set; }
    public int BreakPosition { get; set; }
    public double BreakDistance { get; set; }
}
public sealed class CycleTestDocument
{
    public CycleTestDocument() { Type = 2; }
    public int Type { get; set; }
    public CycleTestSelection SelectSet { get; set; }
    public CycleTestStyles DimensionStyles { get; set; }
}
public sealed class CycleTestApplication { public object ActiveDocument { get; set; } }

internal static class StyleCycleTests
{
    private const string Configuration = "# Order is intentional\n[Inch]\nI2\nI4\nI3\n[Inch-Metric]\nD2\nD3\n[Vertical Inch]\nV2\nV3\n[Vertical Inch-Metric]\nVD2\nVD3\n";
    private static CycleTestDocument Document(string source = "I2")
    {
        var styles = new CycleTestStyles();
        foreach (string name in new[] { "I2", "I3", "I4", "D2", "D3", "V2", "V3", "VD2", "VD3" })
            styles.Values.Add(name, new CycleTestStyle(name));
        return new CycleTestDocument { DimensionStyles = styles,
            SelectSet = new CycleTestSelection { Values = new object[] { new CycleTestDimension { Style = new CycleTestStyle(source) } } } };
    }
    private static void Fails(Action action, string message, Action<bool, string> check)
    {
        bool failed = false; try { action(); } catch (InvalidOperationException) { failed = true; } catch (InvalidDataException) { failed = true; } catch (IOException) { failed = true; }
        check(failed, message);
    }
    internal static void Run(string run, Action<bool, string> check)
    {
        var sequence = StyleSequence.Parse(Configuration);
        var index = new StyleIndex();
        check(sequence.Next("I2") == "I4" && sequence.Next("I4") == "I3" && sequence.Next("I3") == "I2", "style order follows the text file and wraps");
        foreach (string prefix in new[] { "D", "V", "VD" })
            check(sequence.Next(prefix + "2") == prefix + "3" && sequence.Next(prefix + "3") == prefix + "2", "cycle stays inside family " + prefix);
        check(sequence.Next("i2") == "I4", "configured style matching is case-insensitive");
        check(sequence.Next("I2 ") == "I4", "trailing spaces in native style names do not break matching");
        check(StyleSequence.Parse("; comment\n[Inch]\n Custom name \n Next name \n").Next("Custom name") == "Next name", "custom names and comments work without naming-pattern assumptions");
        Fails(() => sequence.Next("Unknown"), "unlisted source style stops before editing", check);
        Fails(() => StyleSequence.Parse("[Inch]\nI2\ni2"), "duplicate styles in a family are rejected", check);
        Fails(() => StyleSequence.Parse("[Inch]\nI2\n[Inch-Metric]\nI2"), "one style cannot belong to multiple families", check);
        Fails(() => StyleSequence.Parse("[Inch]\nI2\n[Inch]"), "duplicate family headings are rejected", check);
        Fails(() => StyleSequence.Parse("[Unexpected]\nI2"), "unknown family headings are rejected", check);
        Fails(() => StyleSequence.Parse("I2"), "style names require a family heading", check);
        Fails(() => StyleSequence.Parse("[Inch\nI2"), "malformed heading is rejected", check);
        Fails(() => StyleSequence.Parse("[Inch]\nI2").Next("I2"), "a one-entry family explains how to add another style", check);

        var document = Document(); var dimension = (CycleTestDimension)document.SelectSet.Values[0];
        check(CycleEngine.Run(document, sequence, index) == "I4" && CycleEngine.Run(document, sequence, index) == "I3" && CycleEngine.Run(document, sequence, index) == "I2", "successive native-command calls advance from the dimension's current style");
        check(document.SelectSet.Count == 1 && Object.ReferenceEquals(document.SelectSet.Item(1), dimension), "selection stays on the same dimension across clicks");
        check(document.DimensionStyles.Values.Values.All(s => s.Writes == 0), "shared style definitions are never edited");
        int reads = document.DimensionStyles.ItemReads;
        CycleEngine.Run(document, sequence, index);
        check(document.DimensionStyles.ItemReads == reads + 1, "warm clicks use one indexed style lookup instead of rescanning the collection");
        CycleEngine.Run(document, sequence, index); CycleEngine.Run(document, sequence, index);
        dimension.Style.Changing = () => { dimension.PrimaryUpperTolerance = "bad"; dimension.SuffixString = "bad"; dimension.TrackDistance = 999; dimension.BreakDistance = 999; };
        CycleEngine.Run(document, sequence, index);
        check(dimension.PrimaryUpperTolerance == "-.002" && dimension.PrimaryLowerTolerance == "-.006" && dimension.SuffixString == " B.C" && dimension.TrackDistance == .023 && dimension.BreakDistance == .007 && dimension.Value == .0127,
            "style assignment preserves engineering text, tolerance values, measured size and placement");
        dimension.Style.Changing = null;
        document.DimensionStyles.Values.Remove("I3");
        Fails(() => CycleEngine.Run(document, sequence, index), "missing next style is reported instead of skipped", check);
        check(dimension.Style.Name == "I4", "missing target leaves the dimension unchanged");
        document = Document(); dimension = (CycleTestDimension)document.SelectSet.Values[0];
        document.DimensionStyles.Values["I4"].DualDisplay = true;
        Fails(() => CycleEngine.Run(document, sequence, index), "configuration cannot silently switch single to dual units", check);
        document.DimensionStyles.Values["I4"].DualDisplay = false; document.DimensionStyles.Values["I4"].PrimaryUnits = 3;
        Fails(() => CycleEngine.Run(document, sequence, index), "configuration cannot silently switch inch to metric units", check);
        check(dimension.Style.Writes == 0, "invalid unit target is rejected before any dimension write");
        document.DimensionStyles.Values["I4"].PrimaryUnits = 5; document.DimensionStyles.Values["I4"].TextOrientation = 2;
        Fails(() => CycleEngine.Run(document, sequence, index), "configuration cannot silently switch horizontal to vertical text", check);
        var differentDocument = Document();
        differentDocument.DimensionStyles.Values["I2"].Name = "I4 ";
        differentDocument.DimensionStyles.Values["I4"].Name = "I2";
        check(CycleEngine.Run(differentDocument, sequence, index) == "I4 ", "switching documents with reordered styles rebuilds stale indexes and uses exact native spelling");
        var ambiguousDocument = Document();
        ambiguousDocument.DimensionStyles.Values.Add("duplicate", new CycleTestStyle("I4 "));
        Fails(() => CycleEngine.Run(ambiguousDocument, sequence, index), "ambiguous normalized native style names are rejected", check);
        document = Document(); dimension = (CycleTestDimension)document.SelectSet.Values[0];
        dimension.Style.Reject = "I4";
        Fails(() => CycleEngine.Run(document, sequence, index), "setter failures after mutation are reported", check);
        check(dimension.Style.Name == "I2", "setter failure restores the original style");
        dimension.Style.Reject = null; dimension.Style.Ignore = "I4";
        Fails(() => CycleEngine.Run(document, sequence, index), "silently ignored style assignment is detected", check);
        document = Document(); dimension = (CycleTestDimension)document.SelectSet.Values[0]; dimension.DimensionType = 8;
        check(CycleEngine.Run(document, sequence, index) == "I4", "coordinate dimensions can cycle styles");
        foreach (int type in new[] { 3, 7, 11 }) { dimension.DimensionType = type; Fails(() => CycleEngine.Run(document, sequence, index), "angular dimension type " + type + " is unchanged", check); }
        dimension.DimensionType = 1; dimension.Type = 123;
        Fails(() => CycleEngine.Run(document, sequence, index), "non-dimension selections are rejected", check);
        document.SelectSet.Values = new object[0]; Fails(() => CycleEngine.Run(document, sequence, index), "empty selection is explained", check);
        document.SelectSet.Values = new object[] { dimension, dimension }; Fails(() => CycleEngine.Run(document, sequence, index), "multiple selections are rejected", check);
        document.Type = 3; Fails(() => CycleEngine.Run(document, sequence, index), "other document types remain unchanged", check);

        string user = Path.Combine(run, "Hammill.DimensionStyles.txt");
        File.WriteAllText(user, Configuration);
        var file = new StyleSequenceFile(user); var first = file.Get();
        check(Object.ReferenceEquals(first, file.Get()), "unchanged clicks reuse the parsed configuration");
        File.WriteAllText(user, Configuration.Replace("I2\nI4\nI3", "I2\nI3\nI4") + "# edited\n");
        check(file.Get().Next("I2") == "I3", "editing order reloads without restart");
        File.WriteAllText(user, "[Inch]\nI2\nI2");
        Fails(() => file.Get(), "invalid edit never falls back to stale cached order", check);
        File.WriteAllText(user, Configuration); file.Get();
        File.Delete(user); Fails(() => file.Get(), "deleted configuration is reported rather than silently reused", check);
        File.WriteAllText(user, Configuration);
        var application = new CycleTestApplication { ActiveDocument = Document() };
        var addin = new Addin();
        typeof(Addin).GetField("hostApplication", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(addin, application);
        typeof(Addin).GetField("styleSequence", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(addin, file);
        var command = typeof(Addin).GetMethod("OnCommand", BindingFlags.NonPublic | BindingFlags.Instance);
        for (int i = 0; i < 4; i++) command.Invoke(addin, new object[] { Addin.StyleCycleCommand });
        dimension = (CycleTestDimension)((CycleTestDocument)application.ActiveDocument).SelectSet.Item(1);
        check(dimension.Style.Name == "I4", "the actual add-in command handler cycles directly with no macro catalog or process launch");
        bool repeat = true;
        dimension.Style.Changing = () => { if (repeat) { repeat = false; command.Invoke(addin, new object[] { Addin.StyleCycleCommand }); } };
        command.Invoke(addin, new object[] { Addin.StyleCycleCommand });
        check(dimension.Style.Name == "I2", "a second click dispatched during a style change is queued instead of lost");
        dimension.Style.Changing = () => { dimension.Style.Changing = null; command.Invoke(addin, new object[] { Addin.LimitToleranceCommand }); command.Invoke(addin, new object[] { Addin.LimitToleranceCommand }); };
        command.Invoke(addin, new object[] { Addin.StyleCycleCommand });
        check(dimension.Style.Name == "I4" && dimension.NumericUpper == .001m && dimension.NumericLower == -.001m && dimension.Style.PrimaryDecimalRoundOff == 5,
            "mixed style and tolerance clicks run in order and finish at the requested precision");
        var ribbon = new RibbonTarget();
        addin.RegisterButton(ribbon, "{08244193-B78D-11D2-9216-00C04F79BE98}", Addin.StyleCycleCommand, "dimension-style-cycle", "Cycle Dimension Style", "Drafting", "Cycle", IconResources.StyleCycleImage);
        check(ribbon.CommandId == 2 && ribbon.SmallImage == 10 && ribbon.LargeImage == 11 && ribbon.Style == (int)SolidEdgeFramework.SeButtonStyle.seButtonIconAndCaptionBelow,
            "built-in cycle button has a stable command ID and large alpha artwork");
    }
}
