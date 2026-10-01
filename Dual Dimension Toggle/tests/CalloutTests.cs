using System;
using System.Collections.Generic;
using DualDimensionToggle;

public sealed class FakeAttribute
{
    public string Name { get; set; }
    public object Value { get; set; }
}
public sealed class FakeAttributeSet
{
    private readonly List<FakeAttribute> entries = new List<FakeAttribute>();
    public string SetName { get; set; }
    public int Count { get { return entries.Count; } }
    public FakeAttribute Item(int index) { return entries[index - 1]; }
    public FakeAttribute Add(string name, int type)
    { if (type != 64) throw new Exception("Unicode attribute required"); FakeAttribute value = new FakeAttribute { Name = name }; entries.Add(value); return value; }
    public void Remove(string name) { entries.RemoveAll(delegate(FakeAttribute item) { return item.Name == name; }); }
}
public sealed class FakeAttributeSets
{
    private readonly List<FakeAttributeSet> entries = new List<FakeAttributeSet>();
    public bool FailAdd;
    public int Count { get { return entries.Count; } }
    public FakeAttributeSet Item(int index) { return entries[index - 1]; }
    public FakeAttributeSet Add(string name)
    { if (FailAdd) throw new Exception("Attribute write rejected"); FakeAttributeSet value = new FakeAttributeSet { SetName = name }; entries.Add(value); return value; }
    public void Remove(string name) { entries.RemoveAll(delegate(FakeAttributeSet item) { return item.SetName == name; }); }
}
public sealed class FakeCallout
{
    public int Callout { get; set; }
    public bool DisplayByItemNumber { get; set; }
    public bool LinkToPartsList { get; set; }
    public FakeStyle Style { get; set; }
    public FakeAttributeSets AttributeSets { get; set; }
    public readonly string[] Text = { "", "", "", "" };
    public Action<int, string> BeforeWrite;
    public bool IgnoreWrites;
    public int Writes;
    public FakeCallout(string style, string text)
    { Callout = 1; Style = new FakeStyle(style); Text[0] = text; AttributeSets = new FakeAttributeSets(); }
    private void Set(int field, string value)
    { Writes++; if (BeforeWrite != null) BeforeWrite(field, value); if (!IgnoreWrites) Text[field] = value; }
    public string BalloonText { get { return Text[0]; } set { Set(0, value); } }
    public string BalloonTextLower { get { return Text[1]; } set { Set(1, value); } }
    public string BalloonTextPrefix { get { return Text[2]; } set { Set(2, value); } }
    public string BalloonTextSuffix { get { return Text[3]; } set { Set(3, value); } }
    public string BalloonDisplayedText { get { throw new Exception("Do not replace linked fields with display text"); } }
}

internal static class CalloutTests
{
    private static int assertions;
    private static readonly StyleMap Map = new StyleMap(new[] { "1 2 place", "2 2 place m[i]", "1 3 place", "2 3 place m[i]" });
    private static void Equal<T>(T expected, T actual, string name)
    { assertions++; if (!Object.Equals(expected, actual)) throw new Exception(name + ": expected " + expected + ", got " + actual); }
    private static void Text(string source, bool dual, string expected, int count)
    {
        string result, reason; int changed;
        Equal(true, CalloutText.Convert(source, dual, out result, out changed, out reason), "Text accepted " + source);
        Equal(expected, result, "Text result"); Equal(count, changed, "Value count");
    }
    private static ConversionReport Run(bool dual, params FakeCallout[] notes)
    {
        ConversionReport report = new ConversionReport();
        CalloutConverter.ConvertSheet(new FakeSheet { Balloons = new FakeCollection(notes) }, Map, dual, report);
        return report;
    }
    internal static int Run()
    {
        assertions = 0;
        const string original = "%DI %{%HS/DU} DRILL %ZH\r%DI 6.50/6.35[.256/.250] DIA %{%BD/DU} DP.\r(1)PLC AS SHOWN\r(FOR 1/4 SPRING PIN)";
        string single = original.Replace("6.50/6.35[.256/.250]", ".256/.250");
        Text(original, false, single, 2);
        Text(original, true, original, 0);
        Text("DIA 3.18[.125]. DEPTH .03[.001]", false, "DIA .125. DEPTH .001", 2);
        Text("SIZE .99[.001]", true, "SIZE .03[.001]", 1);
        Text("%DI6.50/6.35[.256/.250]", false, "%DI.256/.250", 2);
        Text("SIZE -0.03[-0.001]", false, "SIZE -0.001", 1);
        Text("SIZE -.0254[-.001]", true, "SIZE -.03[-.001]", 1);
        Text("6.50 / 6.35 [ .256 / .250 ]", false, ".256 / .250", 2);
        foreach (string unchanged in new[] { ".125", ".256/.250", "45.0%DG", "M6x1.0", "1/4-20 UNC", "(3)PLC", "REV 1.2", "[REV 1.2]", "%{%HS/DU/@3/ST+.001^-.002}", "%{Custom 6.50[.256]|G}", "IDENTIFY\rAS REQ'D", "" })
        { Text(unchanged, false, unchanged, 0); Text(unchanged, true, unchanged, 0); }
        foreach (string malformed in new[] { "6.50/6.35[.256]", "6.50[.256/.250]", "6.50[.256", "[.256]", "6.50%DI[.256]", "6.50%{%HS}[.256]", "%{%HS/DU", "6.50[.256][.250]" })
        {
            string result, reason; int count;
            Equal(false, CalloutText.Convert(malformed, false, out result, out count, out reason), "Malformed rejected " + malformed);
            Equal(malformed, result, "Malformed text unchanged");
        }
        FakeCallout note = new FakeCallout("2 2 place m[i]", original);
        note.Text[1] = "LOWER .03[.001]"; note.Text[2] = "PREFIX .013[.0005]"; note.Text[3] = "SUFFIX .64[.025]";
        string[] originalFields = (string[])note.Text.Clone();
        ConversionReport report = Run(false, note);
        Equal(1, report.CalloutsChanged, "Combined callout changed");
        Equal(1, report.CalloutStylesChanged, "Style changed");
        Equal(5, report.CalloutValuesChanged, "All four fields changed");
        Equal(single, note.Text[0], "User sample raw links preserved");
        Equal("1 2 place", note.Style.Name, "Matching style selected");
        string saved = CalloutHistoryStore.Read(note);
        Equal(true, saved != null, "Conversion history written");
        Equal(0, Run(false, note).CalloutsChanged, "Repeated inch conversion idempotent");
        Equal(saved, CalloutHistoryStore.Read(note), "Repeat retains history");
        // A fresh object with only serialized annotation data simulates reopening
        // the document: reversal must not depend on a process-local dictionary.
        FakeCallout reopened = new FakeCallout(note.Style.Name, note.Text[0]);
        Array.Copy(note.Text, reopened.Text, 4);
        CalloutHistoryStore.Write(reopened, saved);
        report = Run(true, reopened);
        Equal(1, report.CalloutsChanged, "Saved conversion restores after reopen");
        Equal(5, report.CalloutValuesChanged, "Known values restored");
        for (int i = 0; i < 4; i++) Equal(originalFields[i], reopened.Text[i], "Roundtrip field " + i);
        Equal("2 2 place m[i]", reopened.Style.Name, "Dual style restored");
        Equal(null, CalloutHistoryStore.Read(reopened), "History cleared after reversal");
        Equal(0, Run(true, reopened).CalloutsChanged, "Repeated dual conversion idempotent");

        FakeCallout inch = new FakeCallout("1 3 place", "UNTRACKED .125 AND 1/4-20 UNC");
        report = Run(true, inch);
        Equal(1, report.CalloutStylesChanged, "Untracked note still changes style");
        Equal(0, report.CalloutValuesChanged, "No inferred numeric conversions");
        Equal("UNTRACKED .125 AND 1/4-20 UNC", inch.Text[0], "Untracked literals preserved");
        FakeCallout linked = new FakeCallout("2 2 place m[i]", "%TS THR'D %{%TD/DU} DP.");
        Equal(1, Run(false, linked).CalloutStylesChanged, "Property-only note changes style");
        Equal(0, linked.Writes, "Property text never rewritten");
        foreach (FakeCallout skip in new[] { new FakeCallout("2 3 place m[i]", "45%DG"), new FakeCallout("2 2 place m[i]", original) { DisplayByItemNumber = true }, new FakeCallout("2 2 place m[i]", original) { LinkToPartsList = true } })
        { Equal(1, Run(false, skip).CalloutsSkipped, "Protected note skipped"); Equal(0, skip.Style.Writes, "Protected style untouched"); }
        FakeCallout balloon = new FakeCallout("2 2 place m[i]", original) { Callout = 0 };
        Equal(0, Run(false, balloon).CalloutsExamined, "Ordinary balloon excluded");
        FakeCallout missing = new FakeCallout("2 5 place m[i]", original);
        Equal(1, Run(false, missing).CalloutsSkipped, "Missing style skips entire callout");
        Equal(0, missing.Writes, "No text-only half-conversion");
        FakeCallout custom = new FakeCallout("ANSI", original);
        Equal(1, Run(false, custom).CalloutsChanged, "Explicit pairs supported with custom style");
        Equal("ANSI", custom.Style.Name, "Custom style not guessed");
        note.Text[0] += " EDITED";
        Equal(1, Run(true, note).CalloutsSkipped, "Edited tracked text requires review");
        Equal("1 2 place", note.Style.Name, "Edited note not partly converted");
        Equal(true, note.Text[0].EndsWith("EDITED"), "User edit retained");

        FakeCallout fail = new FakeCallout("2 2 place m[i]", original);
        fail.BeforeWrite = delegate(int field, string text) { if (field == 0 && text == single) throw new Exception("Text write rejected"); };
        report = Run(false, fail);
        Equal(1, report.CalloutsFailed, "Rejected text reported");
        Equal("2 2 place m[i]", fail.Style.Name, "Style restored after text failure");
        Equal(original, fail.Text[0], "Text restored");
        Equal(null, CalloutHistoryStore.Read(fail), "History restored on failure");
        FakeCallout ignored = new FakeCallout("2 2 place m[i]", original) { IgnoreWrites = true };
        Equal(1, Run(false, ignored).CalloutsFailed, "Silent text setter failure detected");
        FakeCallout noStorage = new FakeCallout("2 2 place m[i]", original);
        noStorage.AttributeSets.FailAdd = true;
        Equal(1, Run(false, noStorage).CalloutsFailed, "Record failure prevents conversion");
        Equal(0, noStorage.Style.Writes, "No style edit before record saved");
        Equal(0, noStorage.Writes, "No text edit before record saved");
        FakeAttributeSet unrelated = reopened.AttributeSets.Add("OtherMacro"); unrelated.Add("Data", 64).Value = "keep";
        Run(false, reopened); Run(true, reopened);
        Equal(1, reopened.AttributeSets.Count, "Unrelated attributes retained");
        Equal("keep", reopened.AttributeSets.Item(1).Item(1).Value, "Unrelated value retained");
        CalloutHistory unicode = new CalloutHistory(); unicode.Inch[0] = "\u2300 .001\r\nline"; unicode.Dual[0] = "\u2300 .03[.001]\r\nline";
        Equal(unicode.Dual[0], CalloutHistory.Decode(unicode.Encode()).Dual[0], "Unicode and multiline history retained");
        return assertions;
    }
}
