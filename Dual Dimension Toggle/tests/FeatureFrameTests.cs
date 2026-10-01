using System;
using System.Globalization;
using System.Threading;
using DualDimensionToggle;

public sealed class FakeFeatureFrame
{
    public readonly string[] Rows = { "", "", "", "" };
    public int Writes;
    public bool IgnoreWrites;
    public Action<int, string> BeforeWrite;
    private void Set(int row, string value)
    {
        Writes++;
        if (BeforeWrite != null) BeforeWrite(row, value);
        if (!IgnoreWrites) Rows[row] = value;
    }
    public string PrimaryFrame { get { return Rows[0]; } set { Set(0, value); } }
    public string SecondaryFrame { get { return Rows[1]; } set { Set(1, value); } }
    public string TertiaryFrame { get { return Rows[2]; } set { Set(2, value); } }
    public string QuaternaryFrame { get { return Rows[3]; } set { Set(3, value); } }
    public string ProjectedToleranceFrame { get { throw new Exception("Projection text must not be touched"); } }
    public object Style { get { throw new Exception("Frame style must not be touched"); } }
}

internal static class FeatureFrameTests
{
    private static int assertions;
    private static void Equal<T>(T expected, T actual, string message)
    {
        assertions++;
        if (!Object.Equals(expected, actual)) throw new Exception(message + ": expected " + expected + ", got " + actual);
    }
    private static void Text(string input, bool toDual, FrameTextStatus status, string expected)
    {
        string actual, reason;
        Equal(status, FeatureFrameText.Convert(input, toDual, out actual, out reason), "Frame text status: " + input);
        Equal(expected, actual, "Frame text: " + input);
        if (status == FrameTextStatus.Unsupported) Equal(false, String.IsNullOrEmpty(reason), "Unsupported reason");
    }
    private static string Frame(string tolerance) { return "%PO%VB" + tolerance + "%VBA1%VBB2%MC"; }
    private static ConversionReport RunFrames(bool toDual, params FakeFeatureFrame[] frames)
    {
        FakeSheet sheet = new FakeSheet { FeatureControlFrames = new FakeCollection(frames) };
        ConversionReport result = new ConversionReport();
        FeatureFrameConverter.ConvertSheet(sheet, toDual, result);
        return result;
    }
    internal static int Run()
    {
        assertions = 0;
        string[,] pairs = {
            { ".001", ".03[.001]" }, { ".0005", ".013[.0005]" },
            { ".025", ".64[.025]" }, { ".005", ".13[.005]" },
            { ".0010", ".025[.0010]" }, { "0.0010", "0.025[0.0010]" },
            { ".0100", ".254[.0100]" }, { ".000", ".00[.000]" },
            { ".100", "2.54[.100]" }, { "1.000", "25.40[1.000]" },
            { "1", "25[1]" }, { "0.1", "3[0.1]" }
        };
        for (int i = 0; i < pairs.GetLength(0); i++)
        {
            Text(Frame(pairs[i, 0]), true, FrameTextStatus.Changed, Frame(pairs[i, 1]));
            Text(Frame(pairs[i, 1]), false, FrameTextStatus.Changed, Frame(pairs[i, 0]));
        }
        Text(Frame(".03[.001]"), true, FrameTextStatus.AlreadyTarget, Frame(".03[.001]"));
        Text(Frame(".001"), false, FrameTextStatus.AlreadyTarget, Frame(".001"));
        Text(Frame(".99[.001]"), false, FrameTextStatus.Changed, Frame(".001")); // Inch is authoritative.
        Text("%PO%VB %DI.03 [ .0010 ]%MC %VBA1%VBB2", false, FrameTextStatus.Changed,
            "%PO%VB %DI.0010%MC %VBA1%VBB2");
        Text("%PO%VB %DI.0005%MC %VBA1%VBB2", true, FrameTextStatus.Changed,
            "%PO%VB %DI.013[.0005]%MC %VBA1%VBB2");
        Text("%PO|.001|A1|B2", true, FrameTextStatus.Changed, "%PO|.03[.001]|A1|B2");
        Text("FLVB0.001MCVBA", true, FrameTextStatus.Changed, "FLVB0.03[0.001]MCVBA");
        Text("%VB.001%VBA", true, FrameTextStatus.Changed, "%VB.03[.001]%VBA"); // Composite row.
        Text("%PO%VB\u2300.001\u24c2%VBA", true, FrameTextStatus.Changed, "%PO%VB\u2300.03[.001]\u24c2%VBA");
        Text(null, true, FrameTextStatus.Empty, null);
        Text(" ", false, FrameTextStatus.Empty, " ");
        Text("%PT.5", true, FrameTextStatus.Preserved, "%PT.5");
        Text("PT0.5", false, FrameTextStatus.Preserved, "PT0.5");
        foreach (string tolerance in new[] { "1/64", "TYP .001", ".001 per 1.0", ".001%PT.5", "-.001", "1e-3", ".001[", "[.001]", ".03[.001][.002]", "0,001", ".001-.002", ".001A", "", ".00000000000000000000000000001", "79228162514264337593543950335" })
            Text(Frame(tolerance), true, FrameTextStatus.Unsupported, Frame(tolerance));
        Text(".001", true, FrameTextStatus.Unsupported, ".001");
        CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            Text(Frame(".025"), true, FrameTextStatus.Changed, Frame(".64[.025]"));
        }
        finally { Thread.CurrentThread.CurrentCulture = originalCulture; }

        FakeFeatureFrame stacked = new FakeFeatureFrame();
        for (int i = 0; i < 4; i++) stacked.Rows[i] = Frame(".001");
        ConversionReport report = RunFrames(true, stacked);
        Equal(1, report.FramesChanged, "One stacked frame changed");
        Equal(4, report.FrameRowsChanged, "All four frame rows converted");
        Equal(0, report.FramesFailed, "Style and projection text untouched");
        for (int i = 0; i < 4; i++) Equal(Frame(".03[.001]"), stacked.Rows[i], "Stacked row output");
        Equal(1, RunFrames(true, stacked).FramesAlreadyTarget, "Dual run idempotent");
        Equal(4, RunFrames(false, stacked).FrameRowsChanged, "All four rows reversed");
        for (int i = 0; i < 4; i++) Equal(Frame(".001"), stacked.Rows[i], "Roundtrip preserves original row");

        FakeFeatureFrame unsupported = new FakeFeatureFrame();
        unsupported.Rows[0] = Frame(".001"); unsupported.Rows[1] = Frame("TYP .001");
        Equal(1, RunFrames(true, unsupported).FramesSkipped, "Unsupported row skips whole frame");
        Equal(0, unsupported.Writes, "No partial edits to unsupported frame");
        Equal(1, RunFrames(true, new FakeFeatureFrame()).FramesSkipped, "Empty frame skipped");
        FakeFeatureFrame projected = new FakeFeatureFrame();
        projected.Rows[0] = Frame(".001"); projected.Rows[2] = "%PT.5";
        projected.BeforeWrite = delegate(int row, string value) { if (row == 0) projected.Rows[2] = ""; };
        Equal(1, RunFrames(true, projected).FramesChanged, "Projected frame tolerance converts");
        Equal("%PT.5", projected.Rows[2], "Projected height retained in third row");

        FakeFeatureFrame fail = new FakeFeatureFrame();
        fail.Rows[0] = fail.Rows[1] = Frame(".001");
        fail.BeforeWrite = delegate(int row, string value) { if (row == 1 && value.Contains("[")) throw new Exception("Setter rejected value"); };
        FakeFeatureFrame later = new FakeFeatureFrame(); later.Rows[0] = Frame(".001");
        report = RunFrames(true, fail, later);
        Equal(1, report.FramesFailed, "Rejected row counted as failed frame");
        Equal(1, report.FramesChanged, "Next frame converted after failure");
        Equal(Frame(".001"), fail.Rows[0], "Earlier row rolled back");
        Equal(Frame(".001"), fail.Rows[1], "Rejected row remains original");
        Equal(true, report.Details[0].Contains("Original frame text restored"), "Restoration reported");

        FakeFeatureFrame ignored = new FakeFeatureFrame { IgnoreWrites = true };
        ignored.Rows[0] = Frame(".001");
        Equal(1, RunFrames(true, ignored).FramesFailed, "Silent setter failure detected");
        FakeFeatureFrame failedRollback = new FakeFeatureFrame();
        failedRollback.Rows[0] = failedRollback.Rows[1] = Frame(".001");
        failedRollback.BeforeWrite = delegate(int row, string value)
        {
            if (row == 1 || (row == 0 && value == Frame(".001"))) throw new Exception("Read-only frame");
        };
        report = RunFrames(true, failedRollback);
        Equal(1, report.FramesFailed, "Failed rollback reported");
        Equal(true, report.Details[0].Contains("Could not restore all"), "Failed rollback warning visible");

        // Exercise the actual document entry point with dimensions AND frames.
        FakeDimension dimension = new FakeDimension("1 3 place");
        FakeFeatureFrame frame = new FakeFeatureFrame(); frame.Rows[0] = Frame(".001");
        FakeDocument document = new FakeDocument {
            Type = 2,
            DimensionStyles = new FakeCollection(new FakeStyle("1 3 place"), new FakeStyle("2 3 place m[i]")),
            ActiveSheet = new FakeSheet { Dimensions = new FakeCollection(dimension), FeatureControlFrames = new FakeCollection(frame) }
        };
        report = Converter.ConvertDocument(document, true);
        Equal(1, report.Changed, "Document converts dimension");
        Equal(1, report.FramesChanged, "Document converts frame");
        Equal(Frame(".03[.001]"), frame.Rows[0], "Document frame output");
        Equal(true, report.ToString().Contains("Feature Control Frames: 1 changed"), "Frame summary visible");
        return assertions;
    }
}
