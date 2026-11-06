using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Hammill.ORingFinder;

// Public fakes support the same late-bound paths as COM. No CAD process is
// started, and all fixture data and rendered previews stay in this project.
public sealed class Items
{
    public object[] Values;
    public Items(params object[] values) { Values = values; }
    public int Count { get { return Values.Length; } }
    public object Item(int i) { return Values[i - 1]; }
}
public sealed class Dimension { public string DisplayName; public decimal Value; }
public sealed class Profile { public Items Dimensions; }
public sealed class ProfileSet { public Items Profiles; }
public sealed class Occurrence { public string Name; public object OccurrenceDocument; }
public sealed class AssemblyDoc { public Items Occurrences; }
public sealed class Property
{
    private object value;
    public bool FailNextWrite;
    public object Value { get { return value; } set { if (FailNextWrite) { FailNextWrite = false; throw new IOException("Simulated write failure"); } this.value = value; } }
}
public sealed class CustomProperties
{
    public Dictionary<string, Property> Values = new Dictionary<string, Property>();
    public Property Item(string name) { return Values[name]; }
}
public sealed class PropertySets
{
    public CustomProperties Custom = new CustomProperties();
    public CustomProperties Item(string name) { if (name != "Custom") throw new Exception(); return Custom; }
}
public sealed class PartDoc
{
    public int Type = 1;
    public string FullName;
    public Items ProfileSets;
    public PropertySets Properties = new PropertySets();
    public int Saved;
    public bool FailSave;
    public void Save() { if (FailSave) throw new IOException("Simulated save failure"); Saved++; }
}

internal static class Tests
{
    static int count;
    static string root, work, path;
    const string SmallTable = "# editable inches\n2-003,1/16,.5\n2-004,1/16,.75\n2-005,1/16,.9\n2-102,3/32,.049\n";
    static void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL " + name); count++; Console.WriteLine("PASS " + name); }
    static void Reject(Action action, string name, string text = null)
    { try { action(); } catch (Exception error) { Check(text == null || error.Message.Contains(text), name); return; } throw new Exception("FAIL " + name + " (no exception)"); }
    static RingTable TextTable(string text) { File.WriteAllText(path,text); return RingTable.Load(path); }
    static PartDoc Doc(string name = "O_Test.par", decimal diameter = 1m)
    {
        var doc = new PartDoc { FullName = Path.Combine(work,name), ProfileSets = new Items(new ProfileSet { Profiles = new Items(new Profile { Dimensions = new Items(
            new Dimension { DisplayName = "ID", Value = diameter * .0254m }, new Dimension { DisplayName = "WD", Value = .09m * .0254m }) }) }) };
        doc.Properties.Custom.Values.Add("drawing number",new Property { Value = "old drawing" });
        doc.Properties.Custom.Values.Add("dave_b",new Property { Value = "old stock" });
        return doc;
    }
    static RingPart Part(PartDoc doc)
    { var part = new RingPart { Document = doc, Name = "O_Test:1", FileName = doc.FullName }; SolidEdgeSession.ReadDimensions(part); return part; }
    static List<RingPart> Scan(PartDoc doc)
    { return SolidEdgeSession.ReadAssembly(new AssemblyDoc { Occurrences = new Items(new Occurrence { Name = "O_Test:1", OccurrenceDocument = doc }) }); }

    [STAThread]
    static int Main()
    {
        try
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..",".."));
            work = Path.Combine(root,"tests",".work",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(work);
            path = Path.Combine(work,"Oring_table.txt");
            var actual = RingTable.Load(Path.Combine(root,"Oring_table.txt"));
            Check(actual.Sizes.Count == 349,"all 349 original external table rows parse");
            var match = actual.Match(1m,.09m);
            Check(match.Suggested.Number == "2-022" && match.Smaller.Number == "2-021", "legacy lookup selects expected neighboring sizes");
            Check(match.Difference(match.Suggested) == 1,"original whole-percent calculation preserved");
            Check(actual.Match(.989m,.09m).Suggested.Number == "2-022","exact ID match is retained");
            Check(actual.Match(.056m,.09m).Smaller == null,"first width-group row never borrows previous width");
            Reject(()=>actual.Match(.001m,.09m),"below-minimum ID is reported");
            Check(actual.Match(99m,.38m).Suggested.Number == "2-475","above-maximum ID uses final row without stale state");
            Reject(()=>actual.Match(0m,.09m),"zero ID is rejected");
            Reject(()=>actual.Match(1m,.2m),"unknown groove width is reported");
            var rules = new[]{new[]{.09m,.098m,.0955m},new[]{.12m,.145m,.1425m},new[]{.16m,.192m,.1895m},new[]{.24m,.286m,.2835m},new[]{.32m,.38m,.3775m}};
            var widths = new[]{"1/16","3/32","1/8","3/16","1/4"};
            Check(rules.Select((row,i)=>row.All(value=>RingTable.NominalWidth(value)==widths[i])).All(x=>x),"all fifteen original width rules preserved");
            var edited = TextTable(SmallTable);
            Check(edited.Match(1m,.09m).Suggested.InsideDiameter == .9m,"external table supplies result");
            File.WriteAllText(path,SmallTable.Replace(",.9",",.95"));
            Check(RingTable.Load(path).Match(1m,.09m).Suggested.InsideDiameter == .95m,"editing TXT changes next read without recompilation");
            Check(TextTable("2-005,.0625,.9\n2-003,1/16,.5\n2-004,1/16,.75").Match(.8m,.09m).Suggested.Number == "2-004","unordered rows and decimal widths are supported");
            var culture = CultureInfo.CurrentCulture;
            try { CultureInfo.CurrentCulture = new CultureInfo("de-DE"); Check(TextTable(SmallTable).Match(1m,.09m).Suggested.InsideDiameter == .9m,"inch decimals do not depend on Windows locale"); }
            finally { CultureInfo.CurrentCulture = culture; }
            Reject(()=>TextTable("2-003,1/16,.5\n2-003,1/8,.7"),"duplicate part number is rejected","line 2");
            Reject(()=>TextTable("2-003,1/16,.5\n2-004,.0625,.500"),"equivalent duplicate size is rejected","line 2");
            Reject(()=>TextTable("2-003,1/16,-.5"),"negative ID is rejected","line 1");
            Reject(()=>TextTable("2-003,1/0,.5"),"zero fraction denominator is rejected","line 1");
            Reject(()=>TextTable("2-003,1/16,NaN"),"non-numeric ID is rejected","line 1");
            Reject(()=>TextTable("# comment\n2-003,1/16"),"missing column reports actual line number","line 2");
            Reject(()=>TextTable("bad,1/16,.5"),"invalid BOM part number is rejected");
            Reject(()=>TextTable("# no data\n"),"empty table is rejected");
            File.Delete(path); Reject(()=>RingTable.Load(path),"missing external table never uses embedded data");
            TextTable(SmallTable);
            Check(SolidEdgeSession.IsRingName("o_one") && SolidEdgeSession.IsRingName("O-two") && !SolidEdgeSession.IsRingName("O") && !SolidEdgeSession.IsRingName(null),"prefix recognition handles short names safely");
            var doc = Doc(); var part = Part(doc);
            Check(part.Diameter == 1m && part.GrooveWidth == .09m,"CAD metres convert to exact inch dimensions");
            var two = SolidEdgeSession.ReadAssembly(new AssemblyDoc { Occurrences = new Items(
                new Occurrence { Name="O_Test:1",OccurrenceDocument=doc },new Occurrence { Name="o-Test:2",OccurrenceDocument=doc },new Occurrence { Name="X",OccurrenceDocument=null }) });
            Check(two.Count == 1,"duplicate occurrences of one part are processed once"); foreach(var item in two)item.Dispose();
            var bad = Doc(); bad.ProfileSets = new Items();
            Reject(()=>SolidEdgeSession.ReadDimensions(Part(bad)),"missing ID and WD block processing");
            var conflict = Doc();
            conflict.ProfileSets = new Items(new ProfileSet { Profiles = new Items(new Profile { Dimensions = new Items(
                new Dimension { DisplayName="ID",Value=.0254m },new Dimension { DisplayName="ID",Value=.0508m },new Dimension { DisplayName="WD",Value=.002286m }) }) });
            Reject(()=>Part(conflict),"conflicting named dimensions are reported");
            var withBad = SolidEdgeSession.ReadAssembly(new AssemblyDoc { Occurrences = new Items(new Occurrence {Name="O_Bad",OccurrenceDocument=bad},new Occurrence {Name="O_Good",OccurrenceDocument=Doc("O_Good.par")}) });
            Check(withBad.Count == 2 && withBad[0].Error != null && withBad[1].Error == null,"bad part does not prevent reviewing good part"); foreach(var item in withBad)item.Dispose();
            var size = actual.Match(1m,.09m).Suggested;
            SolidEdgeSession.Apply(part,size,false);
            Check((string)doc.Properties.Custom.Item("drawing number").Value == "PARKER# 2-022" && (string)doc.Properties.Custom.Item("dave_b").Value == "TORING022" && doc.Saved == 1,"low pressure writes both original BOM properties and saves once");
            SolidEdgeSession.Apply(part,size,true);
            Check((string)doc.Properties.Custom.Item("drawing number").Value == "PARKER# 2-022 P4300" && (string)doc.Properties.Custom.Item("dave_b").Value == "TORING022-P4300","high pressure preserves P4300 suffixes");
            var missing = Doc(); missing.Properties.Custom.Values.Remove("dave_b");
            Reject(()=>SolidEdgeSession.Apply(Part(missing),size,false),"missing property blocks apply");
            Check((string)missing.Properties.Custom.Item("drawing number").Value == "old drawing" && missing.Saved == 0,"missing second property leaves first untouched");
            var failing = Doc(); failing.Properties.Custom.Item("dave_b").FailNextWrite = true;
            Reject(()=>SolidEdgeSession.Apply(Part(failing),size,false),"failed second write is reported");
            Check((string)failing.Properties.Custom.Item("drawing number").Value == "old drawing" && (string)failing.Properties.Custom.Item("dave_b").Value == "old stock" && failing.Saved == 0,"failed write restores both old values without save");
            failing.FailSave = true; Reject(()=>SolidEdgeSession.Apply(Part(failing),size,false),"save failure is reported");
            Check((string)failing.Properties.Custom.Item("drawing number").Value == "old drawing","save failure restores previous value in memory");
            var renamed = Part(Doc()); ((PartDoc)renamed.Document).FullName += ".renamed";
            Reject(()=>SolidEdgeSession.Apply(renamed,size,false),"renamed document requires fresh scan");

            var uiDoc = Doc(); int scans = 0;
            using (var form = new FinderForm(path,()=>{scans++;return Scan(uiDoc);},SolidEdgeSession.Apply))
            {
                form.ReloadData(); Check(form.CanSave && uiDoc.Saved == 0,"opening and reviewing never writes properties");
                File.WriteAllText(path,SmallTable.Replace(",.9",",.95")); form.SaveSelected();
                Check(uiDoc.Saved == 0 && form.StatusText.Contains("changed"),"table change before apply requires refreshed review");
                form.SaveSelected(); Check(uiDoc.Saved == 1,"reviewed refreshed choice can be saved");
                File.WriteAllText(path,"invalid"); form.SaveSelected();
                Check(!form.CanSave && uiDoc.Saved == 1,"invalid table after review prevents save");
                int before = scans; form.ReloadData(); Check(scans == before && !form.CanSave,"invalid table prevents CAD scan and clears usable results");
                File.WriteAllText(path,SmallTable); form.ReloadData();
                Check(form.CanSave,"valid table repair and reload restores operation");
                // Create child HWNDs for a faithful Windows Forms layout render,
                // without putting a test window in the user's working area.
                form.ShowInTaskbar = false; form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-32000,-32000);
                form.Show(); form.PerformLayout();
                using(var image = new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size)); image.Save(Path.Combine(root,"tests",".work","preview.png")); }
                form.Hide();
            }
            Check(uiDoc.Saved == 1,"closing form does not save again");
            Console.WriteLine(count + " checks passed."); return 0;
        }
        catch(Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
