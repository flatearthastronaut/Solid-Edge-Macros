using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using SketchToModels;

// These fakes deliberately expose no revolve-command API: the integration
// check fails if an O-ring part accidentally enters either assisted workflow.
public class History
{
    public List<CadFeature> Values = new List<CadFeature>();
    public int Count { get { return Values.Count; } }
    public CadFeature Item(int index) { return Values[index - 1]; }
}
public class CadFeature
{
    public string Name, EdgebarName, SystemName;
    public int Status = 1216476310, Calls;
    public bool IgnoreReorder, FailReorder, CancelReorder;
    public History History;
    public object Target;
    public object Style;
    public void SetStyle(object style) { Style = style; }
    public bool Before;
    public void Reorder(object target, bool before)
    {
        Calls++; Target = target; Before = before;
        if (CancelReorder) throw new OperationCanceledException("Test cancellation");
        if (FailReorder) throw new InvalidOperationException("CAD dependency prevents reorder");
        if (IgnoreReorder) return;
        History.Values.Remove(this);
        History.Values.Insert(History.Values.IndexOf((CadFeature)target) + (before ? 0 : 1), this);
    }
}
public class Part
{
    public int Type = 1; // Framework DocumentTypeConstants.igPartDocument.
    public FaceStyles FaceStyles = new FaceStyles();
    public EmptyModels Models = new EmptyModels();
    public object BaseStyle;
    public void SetBaseStyle(object kind, object style) { BaseStyle = style; }
    public History DesignEdgebarFeatures = new History(), Sketches = new History();
    public bool ReadOnly = false, Closed, Linked;
    public int Saves, CloseCalls, FailSaveAt;
    public bool FailFinalClose, FinalCloseSaves;
    public List<string> Events = new List<string>();
    public string FullName;
    public void SaveAs(string path) { FullName = path; }
    public void Close() { CloseCalls++; Events.Add("initial-close"); Closed = true; }
    public void Close(bool saveChanges)
    {
        CloseCalls++; FinalCloseSaves = saveChanges; Events.Add("final-close");
        if (FailFinalClose) throw new InvalidOperationException("Close failed");
        Closed = true;
    }
    public void Activate() { }
    public void Save() { Saves++; Events.Add("part-save"); if(Saves==FailSaveAt)throw new InvalidOperationException("Part save failed"); }
    public void HasInterpartLinks(ref bool linked) { linked = Linked; }
}
// Minimal color objects let the Engine.Create test verify that the selected
// O-ring workflow, rather than a conflicting sketch name, reaches PartColors.
public class FaceStyle { public string StyleName = "Black"; }
public class FaceStyles
{
    public FaceStyle Black = new FaceStyle();
    public int Count { get { return 1; } }
    public FaceStyle Item(int index) { return Black; }
}
public class EmptyModels { public int Count { get { return 0; } } }
public class Occurrence
{
    public Part OccurrenceDocument;
    public bool IdentityPlaced;
    public void PutMatrix(ref Array matrix, bool update)
    {
        var values = (double[])matrix;
        IdentityPlaced = values.Length == 16 && values[0] == 1 && values[5] == 1 && values[10] == 1 && values[15] == 1;
    }
}
public class Occurrences
{
    public Occurrence Value;
    public Occurrence AddByFilename(string path)
    {
        if (!Value.OccurrenceDocument.Closed) throw new Exception("Part must close before insertion.");
        return Value;
    }
}
public class AssemblyDoc
{
    public string FullName;
    public bool ReadOnly = false;
    public int Saves, FailSaveAt;
    public List<string> Events = new List<string>();
    public Occurrences Occurrences;
    public CadFeature Copied;
    public object Source;
    public void Activate() { }
    public void Save() { Saves++; Events.Add("assembly-save"); if(Saves==FailSaveAt)throw new InvalidOperationException("Assembly save failed"); }
    public void CopySketch(object source, object occurrence, bool link, ref object status)
    {
        if (!link) throw new Exception("Copy must remain linked.");
        Source = source;
        var part = ((Occurrence)occurrence).OccurrenceDocument;
        part.Sketches.Values.Add(Copied); part.DesignEdgebarFeatures.Values.Add(Copied);
        part.Linked = true; status = 0;
    }
}
public class Documents
{
    public Part Part;
    public string Template;
    public Part Add(string kind, string template) { Template = template; return Part; }
    public Part Open(string path) { Part.Closed = false; return Part; }
}
public class CadApp
{
    public bool DisplayAlerts = true;
    public Documents Documents;
}

class ORingWorkflowTests
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Reject(Action operation, string expected)
    {
        try { operation(); } catch (InvalidOperationException error) {
            Check(error.Message.Contains(expected), "Unexpected rejection: " + error.Message); return;
        }
        throw new Exception("Missing rejection: " + expected);
    }
    static CadFeature Feature(string name, History history)
    {
        return new CadFeature { Name = name, EdgebarName = name, SystemName = Guid.NewGuid().ToString(), History = history };
    }
    // Exercise the common completion path via O-ring's noninteractive modeling
    // branch. These are full Engine.Create calls, not a mocked save/close helper.
    static void CheckFailureKeepsPart(string folder, string template, string failure)
    {
        var part = new Part();
        var feature = Feature(ORingWorkflow.FeatureName,part.DesignEdgebarFeatures);
        part.DesignEdgebarFeatures.Values.Add(feature);
        var assembly = new AssemblyDoc { FullName=Path.Combine(folder,"assembly_12345.asm"),
            Occurrences=new Occurrences{Value=new Occurrence{OccurrenceDocument=part}},
            Copied=Feature("Copied sketch",part.DesignEdgebarFeatures), Events=part.Events };
        var app = new CadApp { Documents=new Documents{Part=part} };
        if(failure=="part")part.FailSaveAt=2;
        if(failure=="assembly")assembly.FailSaveAt=2;
        if(failure=="model")feature.FailReorder=true;
        if(failure=="cancel")feature.CancelReorder=true;
        if(failure=="close")part.FailFinalClose=true;
        var item = new SketchItem{Name="oring",FileName="failure-"+failure+" 12345.par",ORingStyle=true,Source=new object()};
        Reject(()=>Engine.Create(app,assembly,item,template),failure=="close"?"closing the completed part":"Stopped while");
        Check(!part.Closed,"Failed/canceled part must remain open: "+failure);
        Check(part.CloseCalls==(failure=="close"?2:1),"Part was closed after a failed operation: "+failure);
    }
    [STAThread] static void Main()
    {
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oring-fixtures");
        Directory.CreateDirectory(folder);
        string normal = Path.Combine(folder, "normal.par"), ring = Path.Combine(folder, ORingWorkflow.TemplateName);
        File.WriteAllText(normal, "Test path only; not a CAD template.");
        File.WriteAllText(ring, "Test path only; not a CAD template.");
        var item = new SketchItem { Name = "oring", FileName = "oring 12345.par", ORingStyle = true, Source = new object() };
        Check(ORingWorkflow.TemplateFor(item, normal, "") == ring, "Sibling template fallback.");
        Check(ORingWorkflow.TemplateFor(item, normal, " custom ") == "custom", "Explicit template override.");
        Engine.Validate(folder, new[] { item }, "missing normal.par", ring);
        Reject(() => Engine.Validate(folder, new[] { item }, normal, normal), "O-ring Template.par");
        Reject(() => Engine.Validate(folder, new[] { item }, normal, Path.Combine(folder, "missing", ORingWorkflow.TemplateName)), "accessible");
        item.BushingStyle = true;
        Reject(() => ORingWorkflow.TemplateFor(item, normal, ring), "either"); item.BushingStyle = false;
        Check(ORingWorkflow.TemplateFor(new SketchItem(), normal, ring) == normal, "Standard template changed.");
        Check(ORingWorkflow.TemplateFor(new SketchItem { BushingStyle = true }, normal, ring) == normal, "Bushing template changed.");
        Reject(() => Engine.Validate(folder, new[] { item, item }, normal, ring), "same filename");

        var part = new Part();
        var feature = Feature(ORingWorkflow.FeatureName, part.DesignEdgebarFeatures);
        var oldSketch = Feature("Template sketch", part.DesignEdgebarFeatures);
        var copied = Feature("Copied assembly sketch", part.DesignEdgebarFeatures);
        part.DesignEdgebarFeatures.Values.Add(oldSketch); part.DesignEdgebarFeatures.Values.Add(feature);
        part.Sketches.Values.Add(oldSketch);
        var occurrence = new Occurrence { OccurrenceDocument = part };
        var assembly = new AssemblyDoc { FullName = Path.Combine(folder, "assembly_12345.asm"),
            Occurrences = new Occurrences { Value = occurrence }, Copied = copied, Events=part.Events };
        var app = new CadApp { Documents = new Documents { Part = part } };
        // Exercise the actual Engine.Create path: selected template, close/copy/
        // reopen, reorder the new sketch (not the template sketch), then save.
        item.Name = "oring-collet";
        Engine.Create(app, assembly, item, ring);
        Check(app.Documents.Template == ring && assembly.Source == item.Source, "Wrong template/source.");
        Check(occurrence.IdentityPlaced && app.DisplayAlerts, "Placement or alerts changed.");
        Check(feature.Calls == 1 && feature.Target == copied && !feature.Before, "Wrong reorder target/direction.");
        Check(part.Saves == 2 && assembly.Saves == 2 && part.Linked, "Linked checkpoint/final saves missing.");
        Check(part.Closed && part.CloseCalls==2 && part.FinalCloseSaves,"Completed part must close with saving enabled.");
        Check(String.Join(",",part.Events.ToArray())=="initial-close,part-save,assembly-save,part-save,assembly-save,final-close",
            "Final close must follow both successful final saves.");
        foreach(string failure in new[]{"part","assembly","model","cancel","close"})CheckFailureKeepsPart(folder,ring,failure);
        Check(part.BaseStyle == part.FaceStyles.Black && feature.Style == part.FaceStyles.Black, "O-ring workflow did not apply Black over the collet name rule.");

        feature.IgnoreReorder = true;
        part.DesignEdgebarFeatures.Values.Remove(feature); part.DesignEdgebarFeatures.Values.Insert(0, feature);
        Reject(() => ORingWorkflow.Finish(part, 1), "immediately after"); feature.IgnoreReorder = false;
        feature.FailReorder = true; Reject(() => ORingWorkflow.Finish(part, 1), "dependency"); feature.FailReorder = false;
        feature.Status = 0; Reject(() => ORingWorkflow.Finish(part, 1), "feature error"); feature.Status = 1216476310;
        Reject(() => ORingWorkflow.Finish(part, 0), "exactly one");
        feature.Name = "Internal name"; // PathFinder display-name lookup.
        Check(ORingWorkflow.FindProtrusion(part) == feature, "Display-name lookup failed.");
        var duplicate = Feature(ORingWorkflow.FeatureName, part.DesignEdgebarFeatures);
        part.DesignEdgebarFeatures.Values.Add(duplicate);
        Reject(() => ORingWorkflow.FindProtrusion(part), "More than one");
        part.DesignEdgebarFeatures.Values.Remove(duplicate); part.DesignEdgebarFeatures.Values.Remove(feature);
        Reject(() => ORingWorkflow.FindProtrusion(part), "must contain");

        // Construct without showing the form: Shown connects to real Solid Edge.
        // Verify alternate checkboxes cannot remain selected together and the
        // new column does not displace the sketch/filename into wrong cells.
        using (var form = new MainWindow()) {
            var grid = (DataGridView)typeof(MainWindow).GetField("grid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
            grid.Rows.Add(false, false, false, "oring", "oring 12345.par");
            grid.Rows[0].Cells["Bushing"].Value = true;
            grid.Rows[0].Cells["ORing"].Value = true;
            Check(!Convert.ToBoolean(grid.Rows[0].Cells["Bushing"].Value), "O-ring must clear Bushing.");
            grid.Rows[0].Cells["Bushing"].Value = true;
            Check(!Convert.ToBoolean(grid.Rows[0].Cells["ORing"].Value), "Bushing must clear O-ring.");
            Check((string)grid.Rows[0].Cells["Sketch"].Value == "oring" && grid.Columns["ORing"].HeaderText == "O-ring", "Grid layout mismatch.");
        }
        Console.WriteLine("PASS: O-ring workflow and grid; completed parts close after both saves, while modeling/cancel/save failures preserve open parts and close failures stop processing.");
    }
}
