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
        // The literal parser preserves raw fields. The complete conversion also
        // removes DU from linked lengths in the same atomic note transaction.
        single = single.Replace("%{%HS/DU}", "%HS").Replace("%{%BD/DU}", "%BD");
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
        Equal(1, linked.Writes, "Property text unit format rewritten");
        Equal("%TS THR'D %TD DP.", linked.Text[0], "Thread designation retained and depth converted");
        Equal(null, CalloutHistoryStore.Read(linked), "Linked-only conversion needs no history");
        Run(true, linked);
        Equal("%TS THR'D %{%TD/DU} DP.", linked.Text[0], "Thread depth wraps on reversal");
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
        FieldTests(original, single);
        EmptyHistoryTests();
        return assertions;
    }

    private static void EmptyHistoryTests()
    {
        // Exact raw text and empty TextPairs attribute read from the user's
        // failed opposite-side callout. An empty record contains no prior pairs.
        const string dual = "FROM OPP. SIDE\r%DI %{%HS/DU}  DRILL %ZH\r%DI 6.50/6.35[.256/.250] DIA  %{%BD/DU}  DP.\r(1)PLC AS SHOWN\r(FOR 1/4 SPRING PIN)";
        const string inch = "FROM OPP. SIDE\r%DI %HS  DRILL %ZH\r%DI .256/.250 DIA  %BD  DP.\r(1)PLC AS SHOWN\r(FOR 1/4 SPRING PIN)";
        FakeCallout note = new FakeCallout("2 2 place m[i]", dual);
        CalloutHistoryStore.Write(note, "");
        Equal("", CalloutHistoryStore.Read(note), "Reproduce empty stored record");
        ConversionReport report = Run(false, note);
        Equal(0, report.CalloutsFailed, "Empty record does not fail the callout");
        Equal(1, report.CalloutsChanged, "Opposite-side note converted");
        Equal(2, report.CalloutValuesChanged, "Both explicit limits converted");
        Equal(inch, note.Text[0], "Opposite-side words, spacing and model links retained");
        Equal("1 2 place", note.Style.Name, "Opposite-side note inch style");
        string saved = CalloutHistoryStore.Read(note);
        Equal(true, !String.IsNullOrEmpty(saved), "Empty record replaced by valid pair history");
        Equal(0, Run(false, note).CalloutsChanged, "Repaired note repeat unchanged");
        report = Run(true, note);
        Equal(0, report.CalloutsFailed, "Repaired record reverses successfully");
        Equal(dual, note.Text[0], "Exact opposite-side note restored");
        Equal(null, CalloutHistoryStore.Read(note), "Repaired record cleared after reversal");

        note = new FakeCallout("1 2 place", "%HS .125 UNTRACKED");
        CalloutHistoryStore.Write(note, "");
        report = Run(true, note);
        Equal(0, report.CalloutsFailed, "Empty record also permits inch-to-dual");
        Equal("%{%HS/DU} .125 UNTRACKED", note.Text[0], "Empty record does not infer bare values");
        Equal(0, report.CalloutValuesChanged, "No saved values invented");
        Equal(null, CalloutHistory.Decode(""), "Empty record decodes as no history");

        // Only the genuinely empty record is absent. Nonempty damaged or
        // unknown versions could contain known pairs and must remain protected.
        foreach (string invalid in new[] { " ", "1", "2.bad", "1.!.~.~.~.~.~.~.~" })
        {
            note = new FakeCallout("2 2 place m[i]", dual);
            CalloutHistoryStore.Write(note, invalid);
            Equal(1, Run(false, note).CalloutsFailed, "Nonempty invalid record still reported");
            Equal(dual, note.Text[0], "Invalid record does not change text");
            Equal("2 2 place m[i]", note.Style.Name, "Invalid record does not change style");
            Equal(invalid, CalloutHistoryStore.Read(note), "Nonempty record retained for review");
        }
        note = new FakeCallout("2 2 place m[i]", dual) { IgnoreWrites = true };
        CalloutHistoryStore.Write(note, "");
        Equal(1, Run(false, note).CalloutsFailed, "Rejected conversion still reported after empty record");
        Equal("", CalloutHistoryStore.Read(note), "Rollback restores original empty record");
        Equal("2 2 place m[i]", note.Style.Name, "Rollback restores style with empty prior record");
    }

    private static void FieldTests(string original, string single)
    {
        const string counterbore = "%DI %HS DRILL %ZH\r%DI %BS C'BORE %BD DP.\r(%QC)PLC'S EQ. SP. AS SHOWN\r(FOR X S.H.C.S.)";
        const string counterboreDual = "%DI %{%HS/DU} DRILL %ZH\r%DI %{%BS/DU} C'BORE %{%BD/DU} DP.\r(%QC)PLC'S EQ. SP. AS SHOWN\r(FOR X S.H.C.S.)";
        Equal(counterboreDual, CalloutFields.Convert(counterbore, true), "Exact user dual counterbore sample");
        Equal(counterbore, CalloutFields.Convert(counterboreDual, false), "Exact user inch counterbore sample");
        foreach (string code in new[] { "%HS", "%HD", "%BS", "%BD", "%SS", "%TD", "%BR" })
        {
            string wrapped = "%{" + code + "/DU}";
            Equal(wrapped, CalloutFields.Convert(code, true), "Length wraps " + code);
            Equal(code, CalloutFields.Convert(wrapped, false), "Length unwraps " + code);
            Equal(wrapped, CalloutFields.Convert(wrapped, true), "No duplicate DU " + code);
            Equal(code, CalloutFields.Convert(code, false), "Inch already correct " + code);
        }
        foreach (string text in new[] { "%DI %DG %CS %DP %QC %TS %ZH %ZT %HC %SA %BA %BN %BQ",
            "%{Custom %HS/DU|G}", "%{Custom %{Other %BD/DU}|G}", "%{%HS/DU|G}", "%{%QC/DU}", "%{%SA/DU}", "3.18[.125] AND .125", "" })
        {
            Equal(text, CalloutFields.Convert(text, true), "Non-length content not wrapped");
            Equal(text, CalloutFields.Convert(text, false), "Unrelated expression untouched");
        }
        Equal(null, CalloutFields.Convert(null, true), "Null supported");
        Equal("%DI%{%HS/DU}%RT%{%BD/DU}DEEP", CalloutFields.Convert("%DI%HS%RT%BDDEEP", true), "Adjacent codes and words");
        Equal("100% %{%HS/DU}", CalloutFields.Convert("100% %HS", true), "Literal percent does not consume next field");
        Equal("%{%HS/@3/ST+.001^-.002}", CalloutFields.Convert("%{%HS/DU/@3/ST+.001^-.002}", false), "Precision and tolerance retained");
        Equal("%{%HS/DU/@3/ST+.001^-.002}", CalloutFields.Convert("%{%HS/@3/ST+.001^-.002}", true), "Options retained when adding DU");
        Equal("%{%HS/@3/ST+.001^-.002}", CalloutFields.Convert("%{%HS/@3/du/ST+.001^-.002}", false), "DU option independent of case and order");
        Equal("%{%HS/DUMMY}", CalloutFields.Convert("%{%HS/DUMMY}", false), "DU requires whole option");
        Equal("%{%HS/DU}", CalloutFields.Convert("%{%HS}", true), "Existing wrapper not nested");
        Equal("%HS", CalloutFields.Convert("%{%HS/DU/DU}", false), "Repeated DU removal");

        // Unit-format changes must run even after v1.5 already changed the style.
        FakeCallout note = new FakeCallout("1 2 place", counterboreDual);
        ConversionReport report = Run(false, note);
        Equal(1, report.CalloutsChanged, "Already-inch style still repairs field format");
        Equal(0, report.CalloutStylesChanged, "Style already target");
        Equal(1, report.CalloutFieldsChanged, "Unit formatting count");
        Equal(counterbore, note.Text[0], "All counterbore lengths changed");
        Equal(0, Run(false, note).CalloutsChanged, "Field-only repeated run idempotent");
        note = new FakeCallout("2 2 place m[i]", counterbore);
        note.Text[1] = "%HD"; note.Text[2] = "%SS"; note.Text[3] = "%TD";
        report = Run(true, note);
        Equal(4, report.CalloutFieldsChanged, "All four raw fields formatted");
        Equal(counterboreDual, note.Text[0], "Already-dual style adds fields");
        Equal("%{%HD/DU}", note.Text[1], "Lower field");
        Equal("%{%SS/DU}", note.Text[2], "Prefix field");
        Equal("%{%TD/DU}", note.Text[3], "Suffix field");

        // A persisted v1.5 record used dual field wrappers in BOTH snapshots.
        CalloutHistory old = new CalloutHistory();
        old.Dual[0] = original;
        old.Inch[0] = original.Replace("6.50/6.35[.256/.250]", ".256/.250");
        note = new FakeCallout("1 2 place", old.Inch[0]);
        CalloutHistoryStore.Write(note, old.Encode());
        Run(false, note);
        Equal(single, note.Text[0], "v1.5 inch text repaired");
        report = Run(true, note);
        Equal(0, report.CalloutsSkipped, "Old record accepts unit-only edit");
        Equal(original, note.Text[0], "v1.5 literals and linked values restore together");
        Equal(2, report.CalloutValuesChanged, "Known v1.5 literals counted");
        Equal(null, CalloutHistoryStore.Read(note), "Old record cleared");
        note = new FakeCallout("1 2 place", old.Inch[0]);
        CalloutHistoryStore.Write(note, old.Encode());
        Run(true, note);
        Equal(original, note.Text[0], "Direct reversal of v1.5 record");
        note = new FakeCallout("1 2 place", single.Replace("%HS", "%HD"));
        CalloutHistoryStore.Write(note, old.Encode());
        Equal(1, Run(true, note).CalloutsSkipped, "Changed field identity remains a stale record");

        note = new FakeCallout("2 2 place m[i]", counterboreDual) { IgnoreWrites = true };
        report = Run(false, note);
        Equal(1, report.CalloutsFailed, "Field-only ignored write detected");
        Equal("2 2 place m[i]", note.Style.Name, "Field-only failure rolls style back");
        Equal(counterboreDual, note.Text[0], "Field-only failure retains raw links");
    }
}
