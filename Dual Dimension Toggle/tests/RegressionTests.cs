using System;
using System.Collections.Generic;
using DualDimensionToggle;

// Public fakes exercise the same late-bound boundary used for Solid Edge.
// A setter may reject or silently ignore a style change, both seen in COM apps.
public sealed class FakeStyle
{
    private string name;
    public bool Fail, Ignore;
    public int Writes;
    public FakeStyle(string value) { name = value; }
    public string Name
    {
        get { return name; }
        set { Writes++; if (Fail) throw new InvalidOperationException("Locked dimension"); if (!Ignore) name = value; }
    }
}
public sealed class FakeDimension
{
    private readonly FakeStyle style;
    public bool ThrowOnStyleRead, ThrowOnTypeRead;
    public int Kind = 1;
    public int DimensionType
    {
        get { if (ThrowOnTypeRead) throw new InvalidOperationException("Type unavailable"); return Kind; }
    }
    public FakeStyle Style
    {
        get { if (ThrowOnStyleRead) throw new Exception("Angular style must not be accessed"); return style; }
    }
    public FakeDimension(string name) { style = new FakeStyle(name); }
}
public sealed class FakeCollection
{
    private readonly object[] items;
    public FakeCollection(params object[] values) { items = values; }
    public int Count { get { return items.Length; } }
    public object Item(int index) { return items[index - 1]; }
}
public sealed class FakeSheet
{
    public string Name { get { return "Test sheet"; } }
    public FakeCollection Dimensions { get; set; }
    public FakeCollection FeatureControlFrames { get; set; }
    public FakeCollection Balloons { get; set; }
    public FakeSheet() { FeatureControlFrames = new FakeCollection(); Balloons = new FakeCollection(); }
}
public sealed class FakeDocument
{
    public int Type { get; set; }
    public FakeSheet ActiveSheet { get; set; }
    public FakeCollection DimensionStyles { get; set; }
    public object Sheets { get { throw new Exception("Other sheets must never be accessed"); } }
    public void Save() { throw new Exception("Must not save automatically"); }
}

internal static class RegressionTests
{
    private static int assertions;
    private static readonly string[] Catalog = { "1 2 place", "1 3 place", "2 2 place m[i]", "2 3 place m[i]",
        "3 3 place (vert)", "4 3 place m[i] (vert)", "1 fraction", "ANSI" };
    private static void Equal<T>(T expected, T actual, string name)
    {
        assertions++;
        if (!Object.Equals(expected, actual)) throw new Exception(name + ": expected " + expected + ", got " + actual);
    }
    private static FakeDocument Document(params FakeDimension[] dims)
    {
        List<object> styles = new List<object>();
        foreach (string name in Catalog) styles.Add(new FakeStyle(name));
        return new FakeDocument { Type = 2, ActiveSheet = new FakeSheet { Dimensions = new FakeCollection(dims) }, DimensionStyles = new FakeCollection(styles.ToArray()) };
    }
    private static void Match(StyleMap map, string source, bool dual, MatchStatus expected, string name)
    {
        string target;
        Equal(expected, map.Resolve(source, dual, out target), source + " status");
        Equal(name, target, source + " target");
    }
    public static int Main()
    {
        try
        {
            StyleMap map = new StyleMap(Catalog);
            Match(map, "2 3 place m[i]", false, MatchStatus.Convert, "1 3 place");
            Match(map, "2 2 place m[i]", false, MatchStatus.Convert, "1 2 place");
            Match(map, "1 3 place", true, MatchStatus.Convert, "2 3 place m[i]");
            Match(map, "1 2 place", true, MatchStatus.Convert, "2 2 place m[i]");
            Match(map, "4 3 place m[i] (vert)", false, MatchStatus.Convert, "3 3 place (vert)");
            Match(map, "3 3 place (vert)", true, MatchStatus.Convert, "4 3 place m[i] (vert)");
            Match(map, " 99  3 PLACE M[ I ] ", false, MatchStatus.Convert, "1 3 place");
            Match(map, "1 3 place", false, MatchStatus.AlreadyTarget, null);
            Match(map, "2 3 place m[i]", true, MatchStatus.AlreadyTarget, null);
            foreach (string name in new[] { "ANSI", "1 fraction", "1 fraction (vert)", "1 3 place custom", "3 place", "1 3 place i[m]", null, "1 999999999999999 place" })
                Match(map, name, true, MatchStatus.Unrecognized, null);
            Match(map, "2 5 place m[i]", false, MatchStatus.Missing, null);
            Match(map, "4 2 place m[i] (vert)", false, MatchStatus.Missing, null);
            Match(new StyleMap(new[] { "9 3 place" }), "2 3 place m[i]", false, MatchStatus.Convert, "9 3 place");
            Match(new StyleMap(new[] { "1 3 place", "9 3 place" }), "2 3 place m[i]", false, MatchStatus.Ambiguous, null);
            Match(new StyleMap(new string[0]), "1 3 place", true, MatchStatus.Missing, null);

            FakeDimension two = new FakeDimension("2 2 place m[i]");
            FakeDimension three = new FakeDimension("2 3 place m[i]");
            FakeDimension vertical = new FakeDimension("4 3 place m[i] (vert)");
            FakeDimension fraction = new FakeDimension("1 fraction");
            FakeDimension missing = new FakeDimension("2 5 place m[i]");
            FakeDocument doc = Document(two, three, vertical, fraction, missing);
            ConversionReport report = Converter.ConvertDocument(doc, false);
            Equal(3, report.Changed, "three conversions");
            Equal(5, report.Examined, "all dimensions examined");
            Equal(1, report.Unrecognized, "fraction unchanged");
            Equal(1, report.Missing, "missing style reported");
            Equal("1 2 place", two.Style.Name, "two decimal places preserved");
            Equal("1 3 place", three.Style.Name, "three decimal places preserved");
            Equal("3 3 place (vert)", vertical.Style.Name, "orientation preserved");
            Equal(0, fraction.Style.Writes, "unrecognized never written");
            Equal(0, missing.Style.Writes, "missing never written");
            Equal(0, Converter.ConvertDocument(doc, false).Changed, "repeat is idempotent");
            Equal(3, Converter.ConvertDocument(doc, true).Changed, "reverse all converted dimensions");
            Equal("2 2 place m[i]", two.Style.Name, "two-place round trip");
            Equal("2 3 place m[i]", three.Style.Name, "three-place round trip");
            Equal("4 3 place m[i] (vert)", vertical.Style.Name, "vertical round trip");
            for (int i = 1; i <= doc.DimensionStyles.Count; i++)
                Equal(0, ((FakeStyle)doc.DimensionStyles.Item(i)).Writes, "shared style untouched");

            FakeDimension failed = new FakeDimension("2 2 place m[i]");
            failed.Style.Fail = true;
            FakeDimension ignored = new FakeDimension("2 3 place m[i]");
            ignored.Style.Ignore = true;
            FakeDimension later = new FakeDimension("2 3 place m[i]");
            report = Converter.ConvertDocument(Document(failed, ignored, later), false);
            Equal(2, report.Failed, "rejected and ignored setters reported");
            Equal(1, report.Changed, "continues after failed dimension");
            Equal("2 3 place m[i]", ignored.Style.Name, "ignored write retains original");
            Equal(true, report.ToString().Contains("Could not restore"), "failed recovery visible");

            FakeDimension ambiguous = new FakeDimension("2 3 place m[i]");
            doc = Document(ambiguous);
            doc.DimensionStyles = new FakeCollection(new FakeStyle("1 3 place"), new FakeStyle("9 3 place"));
            Equal(1, Converter.ConvertDocument(doc, false).Ambiguous, "ambiguous reported");
            Equal(0, ambiguous.Style.Writes, "ambiguous never written");
            Equal(0, Converter.ConvertDocument(Document(), true).Examined, "empty sheet");

            // Every angular kind must be skipped in both directions even when
            // its style has a valid counterpart in the destination family.
            foreach (int kind in new[] { 3, 7, 11 })
            foreach (bool toDual in new[] { false, true })
            {
                string original = toDual ? "1 3 place" : "2 3 place m[i]";
                FakeDimension angle = new FakeDimension(original) { Kind = kind, ThrowOnStyleRead = true };
                FakeDimension length = new FakeDimension(original);
                report = Converter.ConvertDocument(Document(angle, length), toDual);
                Equal(1, report.AngularSkipped, "angular type " + kind + " skipped");
                Equal(1, report.Changed, "linear dimension still converted");
                Equal(0, report.Failed, "angular style never accessed");
                angle.ThrowOnStyleRead = false;
                Equal(original, angle.Style.Name, "angular style unchanged");
                Equal(0, angle.Style.Writes, "no angular style writes");
            }
            // Distinguish arc length from arc angle; keep other non-angular
            // dimension kinds within the macro's existing conversion scope.
            foreach (int kind in new[] { 1, 2, 4, 5, 6, 8, 9, 10 })
            {
                FakeDimension length = new FakeDimension("2 3 place m[i]") { Kind = kind };
                Equal(1, Converter.ConvertDocument(Document(length), false).Changed, "non-angular type " + kind + " converted");
            }
            FakeDimension unreadable = new FakeDimension("2 3 place m[i]") { ThrowOnTypeRead = true };
            report = Converter.ConvertDocument(Document(unreadable), false);
            Equal(1, report.Failed, "unreadable type reported");
            Equal(0, unreadable.Style.Writes, "unreadable type never changed");

            doc = Document(); doc.Type = 1;
            bool rejected = false;
            try { Converter.ConvertDocument(doc, false); } catch (InvalidOperationException) { rejected = true; }
            Equal(true, rejected, "non-draft rejected");
            assertions += FeatureFrameTests.Run();
            assertions += CalloutTests.Run();
            Console.WriteLine("PASS: " + assertions + " regression assertions.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
