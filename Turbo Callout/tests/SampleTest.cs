using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HoleTableFormatter;
using TurboCallout;

internal static class SampleTest
{
    static bool includeBoltCircles=true;
    static readonly List<object> held=new List<object>();
    static object Keep(object obj){held.Add(obj);return obj;}
    sealed class Snapshot
    {
        internal string[] Text;
        internal Placement Position;
        internal StyleValues Format;
        internal string[] ExpectedText;
        internal StyleValues ExpectedFormat;
        internal bool Macro, Attached;
    }
    static Dictionary<string,Snapshot> Capture(object doc)
    {
        var result=new Dictionary<string,Snapshot>();object sheets=Keep(((dynamic)doc).Sheets);
        for(int s=1;s<=(int)((dynamic)sheets).Count;s++)
        {
            object sheet=Keep(((dynamic)sheets).Item(s));if((int)((dynamic)sheet).SectionType!=0)continue;
            object notes=Keep(((dynamic)sheet).Balloons);
            for(int n=1;n<=(int)((dynamic)notes).Count;n++)
            {
                object note=Keep(((dynamic)notes).Item(n));if((int)((dynamic)note).Callout==0)continue;
                object style=Keep(((dynamic)note).Style);
                result.Add(s+"/"+n,new Snapshot{Text=new[]{(string)((dynamic)note).BalloonText,(string)((dynamic)note).BalloonTextLower,
                    (string)((dynamic)note).BalloonTextPrefix,(string)((dynamic)note).BalloonTextSuffix},Position=new Placement(note),
                    Format=new StyleValues(style),Macro=NoteAlignment.IsExistingMacroNote(note),Attached=(bool)((dynamic)note).IsTerminatorAttachedToEntity});
            }
        }
        return result;
    }
    static void Verify(object doc,Dictionary<string,Snapshot> before,bool unchanged)
    {
        object sheets=Keep(((dynamic)doc).Sheets);
        foreach(var item in before)
        {
            string[] key=item.Key.Split('/');object sheet=Keep(((dynamic)sheets).Item(Int32.Parse(key[0])));
            object notes=Keep(((dynamic)sheet).Balloons),note=Keep(((dynamic)notes).Item(Int32.Parse(key[1])));
            int? alignment = !unchanged && !TurboCallout.Automation.IsBlank(note) && NoteAlignment.IsExistingMacroNote(note) ? NoteAlignment.Target(note) : null;
            try{item.Value.Position.Verify(note,alignment);}
            catch(InvalidOperationException error){throw new Exception("Placement check failed for "+item.Key+": "+error.Message,error);}
            string[] names={"BalloonText","BalloonTextLower","BalloonTextPrefix","BalloonTextSuffix"};
            for(int i=0;i<4;i++)if(unchanged||i>0||!String.IsNullOrWhiteSpace(item.Value.Text[i]))
            {
                string expected=(!unchanged&&item.Value.ExpectedText!=null?item.Value.ExpectedText[i]:item.Value.Text[i])??"";
                if(Convert.ToString(Property.Get(note,names[i]))!=expected)throw new Exception("Preserved text changed: "+item.Key+"/"+names[i]);
            }
            if(unchanged||!String.IsNullOrWhiteSpace(item.Value.Text[0]))
            {object style=Keep(((dynamic)note).Style);var expected=!unchanged&&item.Value.ExpectedFormat!=null?item.Value.ExpectedFormat:item.Value.Format;
                if(!expected.Matches(new StyleValues(style)))throw new Exception("Existing formatting changed outside the requested precision: "+item.Key);}
            if(!unchanged&&String.IsNullOrWhiteSpace(item.Value.Text[0]))Console.WriteLine(item.Key+": "+((dynamic)note).BalloonDisplayedText);
        }
    }
    static void PrepareExpected(object doc,Dictionary<string,Snapshot> before,RuleBook rules,bool circles)
    {
        object sheets=Keep(((dynamic)doc).Sheets);
        using(var reader=new NativeHole(doc))foreach(var item in before)
        {
            if(!item.Value.Macro||!item.Value.Attached)continue;
            string[] key=item.Key.Split('/');object sheet=Keep(((dynamic)sheets).Item(Int32.Parse(key[0])));
            object notes=Keep(((dynamic)sheet).Balloons),note=Keep(((dynamic)notes).Item(Int32.Parse(key[1]))),style=Keep(((dynamic)note).Style);
            bool dual=(bool)Property.Get(style,"DualDisplay");int? quantity;HoleFacts hole=reader.Read(note,out quantity);Decision choice=Notes.Recognize(rules,hole);
            item.Value.ExpectedText=Notes.ApplyThreadRule(item.Value.Text,choice).Select(t=>Notes.ApplyPrecision(t,choice,hole,dual)).ToArray();
            item.Value.ExpectedText=Notes.ApplyQuantity(item.Value.ExpectedText,circles,quantity).Select(Notes.ApplyWordSpacing).ToArray();
            item.Value.ExpectedText=Notes.ApplyOppositeSide(item.Value.ExpectedText,LeaderSide.IsHidden(note));
            item.Value.ExpectedFormat=new StyleValues(style);
            new UnitStyle(style,new NotePlan{Dual=dual,InchPlaces=2,FullSizeStacks=item.Value.ExpectedText.Any(Notes.HasFixedStacks)}).ApplyToSnapshot(item.Value.ExpectedFormat);
        }
    }
    static void VerifyQuantities(object doc,RuleBook rules,bool circles)
    {
        var snapshots=Capture(doc);object sheets=Keep(((dynamic)doc).Sheets);
        using(var reader=new NativeHole(doc))foreach(var item in snapshots)
        {
            if(!item.Value.Macro||!item.Value.Attached)continue;
            string[] key=item.Key.Split('/');object sheet=Keep(((dynamic)sheets).Item(Int32.Parse(key[0])));
            object notes=Keep(((dynamic)sheet).Balloons),note=Keep(((dynamic)notes).Item(Int32.Parse(key[1])));
            int? quantity;Decision choice=Notes.Recognize(rules,reader.Read(note,out quantity));
            bool hidden=LeaderSide.IsHidden(note);
            int headerCount=item.Value.Text.Sum(t=>(t??"").Split('\r').Count(line=>line==Notes.OppositeSideLine));
            if(headerCount!=(hidden?1:0)||(hidden&&!item.Value.Text[0].StartsWith(Notes.OppositeSideLine+"\r",StringComparison.Ordinal)))
                throw new Exception("Hidden-edge header is missing, duplicated or on the wrong side: "+item.Key);
            if(item.Value.Text.Any(Notes.HasFixedStacks))
            {
                object style=Keep(((dynamic)note).Style);
                if(!Property.Equal(Property.Get(style,"ToleranceHeightRatio"),1.0))throw new Exception("Typed stack is smaller than the surrounding text: "+item.Key);
                if(!Property.Equal(Property.Get(style,"Height"),.125*.0254))throw new Exception("Sample note height is not .125 inch: "+item.Key);
            }
            string[] text=item.Value.Text;
            string desired=circles?"(%QC)PLC'S EQ. SP. AS SHOWN":"TYP(%QC)";
            int count=text.Sum(t=>(t??"").Split('\r').Count(line=>line==desired));
            if(count!=(quantity==1?0:1))throw new Exception("Quantity wording does not follow the selected mode: "+item.Key);
            if(circles?text.Any(Notes.HasTypical):text.Any(t=>(t??"").Contains("PLC'S")))
                throw new Exception("Quantity wording from the other mode remains: "+item.Key);
            if(choice.Category=="Threaded hole"&&text.Any(t=>(t??"").Contains("%HS")))throw new Exception("Thread still includes its tap-drill size: "+item.Key);
            if(choice.Family=="SHCS counterbore"&&!text.Any(t=>(t??"").Contains("(FOR X S.H.C.S.)")))throw new Exception("Counterbore lost its purpose line.");
        }
    }
    static void VerifyModeSwitch(object doc,RuleBook rules,bool dual)
    {
        // Start with a completed B.C run. Switching off must change quantities
        // without removing/repositioning circles, marks, dimensions, or notes.
        // Switching back must restore the exact canonical text, even if the
        // user chooses the other unit option (existing note units are retained).
        var canonical=Capture(doc);var circles=CaptureBoltCircles(doc);
        var marks=CaptureProjectionMarks(doc);var dimensions=CaptureDimensions(doc);
        var expected=Capture(doc);PrepareExpected(doc,expected,rules,false);
        RunReport off=TurboCallout.Automation.FillDocument(doc,rules,!dual,null,false);
        if(off.Errors!=0||off.UpdatedExisting==0||off.BoltCirclesEnabled)throw new Exception("B.C to TYP mode switch failed.");
        Verify(doc,expected,false);VerifyQuantities(doc,rules,false);
        VerifyBoltCircles(doc,circles);VerifyProjectionMarks(doc,marks);VerifyDimensions(doc,dimensions);
        RunReport repeat=TurboCallout.Automation.FillDocument(doc,rules,!dual,null,false);
        if(repeat.Errors!=0||repeat.UpdatedExisting!=0)throw new Exception("TYP mode rerun is not stable.");
        RunReport on=TurboCallout.Automation.FillDocument(doc,rules,!dual,null,true);
        if(on.Errors!=0||on.UpdatedExisting==0||on.BoltCirclesCreated!=0||on.BcDimensionsCreated!=0)throw new Exception("TYP to B.C mode switch failed.");
        Verify(doc,canonical,true);VerifyQuantities(doc,rules,true);
        VerifyBoltCircles(doc,circles);VerifyProjectionMarks(doc,marks);VerifyDimensions(doc,dimensions);
        Console.WriteLine("PASS B.C -> TYP -> B.C mode switches preserve note units, machining text, placement and existing B.C annotations.");
    }
    static void BlankMacroNotes(object doc)
    {
        // Exercise initial fills on a disposable copy of the same real-model
        // leaders. Never erase text in the source sample or in custom notes.
        object sheets=Keep(((dynamic)doc).Sheets);
        for(int s=1;s<=(int)((dynamic)sheets).Count;s++)
        {
            object sheet=Keep(((dynamic)sheets).Item(s));if((int)((dynamic)sheet).SectionType!=0)continue;
            object notes=Keep(((dynamic)sheet).Balloons);
            for(int n=1;n<=(int)((dynamic)notes).Count;n++)
            {
                object note=Keep(((dynamic)notes).Item(n));if(!NoteAlignment.IsExistingMacroNote(note))continue;
                foreach(string field in new[]{"BalloonText","BalloonTextLower","BalloonTextPrefix","BalloonTextSuffix"})Property.Set(note,field,"");
            }
        }
    }
    static void SeedMixedRules(object doc,RuleBook rules,bool dual)
    {
        // Exercise both old-note migration and new fills on real linked holes.
        // Only the disposable copy is edited; saved engineering purpose lines
        // stay in the migration case so the spacing rewrite must preserve them.
        object sheets=Keep(((dynamic)doc).Sheets);int threads=0;
        using(var reader=new NativeHole(doc))
        for(int s=1;s<=(int)((dynamic)sheets).Count;s++)
        {
            object sheet=Keep(((dynamic)sheets).Item(s));if((int)((dynamic)sheet).SectionType!=0)continue;
            object notes=Keep(((dynamic)sheet).Balloons);
            for(int i=1;i<=(int)((dynamic)notes).Count;i++)
            {
                object note=Keep(((dynamic)notes).Item(i));
                if(!NoteAlignment.IsExistingMacroNote(note)||!(bool)((dynamic)note).IsTerminatorAttachedToEntity)continue;
                int? quantity;Decision choice=Notes.Recognize(rules,reader.Read(note,out quantity));
                if(choice.Category!="Threaded hole"&&choice.Family!="SHCS counterbore")continue;
                bool blank=choice.Category=="Threaded hole"?++threads==1:dual;
                if(blank)
                {foreach(string name in new[]{"BalloonText","BalloonTextLower","BalloonTextPrefix","BalloonTextSuffix"})Property.Set(note,name,"");continue;}
                string text=(string)((dynamic)note).BalloonText;
                text=text.Replace("(%QC)PLC'S EQ. SP. AS SHOWN","TYP(%QC)\r(%QC)PLC'S EQ. SP.");
                if(choice.Category=="Threaded hole"&&!text.Contains("DRILL"))text="%DI%{%HS/@2}  DRILL  %ZH\r"+text;
                ((dynamic)note).BalloonText=text;((dynamic)note).BalloonTextSuffix="TYP(%QC)";
            }
        }
    }
    static void SeedDuplicateQuantities(object doc,RuleBook rules,bool dual)
    {
        // Reproduce the v1.3 plain-hole defect on an isolated sample copy in
        // either unit mode, even when the user's active leaders are blank.
        RunReport initial=TurboCallout.Automation.FillDocument(doc,rules,dual,null,false);
        if(initial.Errors!=0)throw new Exception("Cannot prepare duplicate-quantity fixture.");
        object sheets=Keep(((dynamic)doc).Sheets);int seeded=0;
        using(var reader=new NativeHole(doc))foreach(var item in Capture(doc))
        {
            if(!item.Value.Macro||!item.Value.Attached)continue;
            string[] key=item.Key.Split('/');object sheet=Keep(((dynamic)sheets).Item(Int32.Parse(key[0])));
            object notes=Keep(((dynamic)sheet).Balloons),note=Keep(((dynamic)notes).Item(Int32.Parse(key[1])));
            int? quantity;Decision choice=Notes.Recognize(rules,reader.Read(note,out quantity));
            if(choice.Family!="Simple drill"||!quantity.HasValue||quantity.Value<=1)continue;
            string text=(string)((dynamic)note).BalloonText;
            if(!text.Contains("PLC'S"))text=text.Replace("TYP(%QC)","(%QC)PLC'S EQ. SP.");
            ((dynamic)note).BalloonText=text+"\rTYP(%QC)";
            ((dynamic)note).BalloonTextSuffix="TYP(%QC)";seeded++;
        }
        if(seeded==0)throw new Exception("This fixture requires repeated simple drilled holes.");
        Console.WriteLine("Seeded duplicate PLC'S/TYP quantities on "+seeded+" existing plain-hole notes.");
    }
    static Dictionary<string,string> CaptureDimensions(object doc)
    {
        var result=new Dictionary<string,string>();object sheets=Keep(((dynamic)doc).Sheets);
        for(int s=1;s<=(int)((dynamic)sheets).Count;s++)
        {
            object sheet=Keep(((dynamic)sheets).Item(s));if((int)((dynamic)sheet).SectionType!=0)continue;
            object dims=Keep(((dynamic)sheet).Dimensions);
            for(int i=1;i<=(int)((dynamic)dims).Count;i++)
            {
                object d=Keep(((dynamic)dims).Item(i)),style=Keep(((dynamic)d).Style);var values=new List<string>();
                foreach(string p in new[]{"Value","DimensionType","TrackDistance","TrackAngle","BreakDistance","BreakPosition","LeaderDistance","TerminatorPosition","SuffixString","OverrideString"})
                {object v=Property.Get(d,p);values.Add(v is double?((double)v).ToString("F7",System.Globalization.CultureInfo.InvariantCulture):Convert.ToString(v));}
                foreach(string p in new[]{"PrimaryUnits","SecondaryUnits","DualDisplay","PrimaryDecimalRoundOff","SecondaryDecimalRoundOff","Height"})values.Add(Convert.ToString(Property.Get(style,p)));
                result.Add(s+"/"+i,String.Join("|",values));
            }
        }
        return result;
    }
    static void VerifyDimensions(object doc,Dictionary<string,string> expected)
    {var actual=CaptureDimensions(doc);if(actual.Count!=expected.Count||expected.Any(p=>!actual.ContainsKey(p.Key)||actual[p.Key]!=p.Value))throw new Exception("Existing B.C dimensions changed in callouts-only mode.");}
    static Dictionary<string,string> CaptureBoltCircles(object doc)
    {
        var result=new Dictionary<string,string>();object sheets=Keep(((dynamic)doc).Sheets);
        for(int si=1;si<=(int)((dynamic)sheets).Count;si++)
        {
            object sheet=Keep(((dynamic)sheets).Item(si));if((int)((dynamic)sheet).SectionType!=0)continue;
            object circles=Keep(((dynamic)sheet).BoltHoleCircles);
            for(int i=1;i<=(int)((dynamic)circles).Count;i++)
            {
                object circle=Keep(((dynamic)circles).Item(i)),style=Keep(((dynamic)circle).Style);
                double x,y,r;((dynamic)circle).Circle2d(out x,out y,out r);
                // Stable displayed precision is much tighter than drawing scale;
                // actual model-reference persistence is checked by the rerun's
                // IsMember checks, not by trusting this coordinate snapshot.
                result.Add(si+"/"+i,String.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0:F9}|{1:F9}|{2:F9}|{3}|{4}|{5}|{6}",x,y,r,((dynamic)circle).Count,
                    ((dynamic)style).CenterLineType,((dynamic)style).LineWidth,((dynamic)style).DrivenColor));
            }
        }
        return result;
    }
    static void VerifyBoltCircles(object doc,Dictionary<string,string> expected)
    {
        var actual=CaptureBoltCircles(doc);
        if(actual.Count!=expected.Count||expected.Any(p=>!actual.ContainsKey(p.Key)||actual[p.Key]!=p.Value))
            throw new Exception("Bolt-circle geometry, membership count or presentation changed.");
    }
    static Dictionary<string,string> CaptureProjectionMarks(object doc)
    {
        var result=new Dictionary<string,string>();object sheets=Keep(((dynamic)doc).Sheets);
        for(int si=1;si<=(int)((dynamic)sheets).Count;si++)
        {
            object sheet=Keep(((dynamic)sheets).Item(si));if((int)((dynamic)sheet).SectionType!=0)continue;
            object marks=Keep(((dynamic)sheet).CenterMarks);
            for(int i=1;i<=(int)((dynamic)marks).Count;i++)
            {
                object mark=Keep(((dynamic)marks).Item(i)),style=Keep(((dynamic)mark).Style),hole=null,arc=null;
                double x,y,z,ax=0,ay=0,az=0;bool key,ak=false;
                ((IBcCenterMark)mark).GetCenter(out hole,out x,out y,out z,out key);Keep(hole);
                if((bool)((dynamic)mark).TwoPoint){((IBcCenterMark)mark).GetArcCenter(out arc,out ax,out ay,out az,out ak);Keep(arc);}
                result.Add(si+"/"+i,String.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0:F9}|{1:F9}|{2:F9}|{3:F9}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}",x,y,ax,ay,key,ak,
                    ((dynamic)mark).TwoPoint,((dynamic)style).AutoCenterProjectionLine,((dynamic)style).ProjectionLineDisplay,
                    ((dynamic)style).CenterMarkExtension,((dynamic)style).CenterMarkSize,((dynamic)style).DrivenColor,((dynamic)style).LineWidth));
            }
        }
        return result;
    }
    static void VerifyProjectionMarks(object doc,Dictionary<string,string> expected)
    {
        var actual=CaptureProjectionMarks(doc);
        if(actual.Count!=expected.Count||expected.Any(p=>!actual.ContainsKey(p.Key)||actual[p.Key]!=p.Value))
            throw new Exception("Center-mark geometry, radial attachments or presentation changed.");
    }
    static void VerifySideMaintenance(object doc,RuleBook rules,Dictionary<string,Snapshot> canonical)
    {
        // Deliberately misplace/duplicate the managed header in disposable notes
        // to test the existing-note path, independently of initial generation.
        object sheets=Keep(((dynamic)doc).Sheets);int changed=0;
        foreach(var item in canonical)
        {
            if(!item.Value.Macro||!item.Value.Attached)continue;
            string[] key=item.Key.Split('/');object sheet=Keep(((dynamic)sheets).Item(Int32.Parse(key[0])));
            object notes=Keep(((dynamic)sheet).Balloons),note=Keep(((dynamic)notes).Item(Int32.Parse(key[1])));
            string body=Notes.ApplyOppositeSide(new[]{item.Value.Text[0]},false)[0];
            ((dynamic)note).BalloonText=body+"\r"+Notes.OppositeSideLine+"\r"+Notes.OppositeSideLine;
            item.Value.Position.Restore(note);changed++;
        }
        RunReport repair=TurboCallout.Automation.FillDocument(doc,rules,false,null,includeBoltCircles);
        if(repair.UpdatedExisting!=changed||repair.Errors!=0)throw new Exception("Existing-note hidden header repair failed.");
        Verify(doc,canonical,true);VerifyQuantities(doc,rules,includeBoltCircles);
        Console.WriteLine("PASS Existing-note hidden header placement, duplicate removal, and visible-edge omission.");
    }
    static void VerifyMixedVisibility(object doc)
    {
        int hidden=0,visible=0;object sheets=Keep(((dynamic)doc).Sheets);
        foreach(var item in Capture(doc))
        {
            if(!item.Value.Macro||!item.Value.Attached)continue;
            string[] key=item.Key.Split('/');object sheet=Keep(((dynamic)sheets).Item(Int32.Parse(key[0])));
            object notes=Keep(((dynamic)sheet).Balloons),note=Keep(((dynamic)notes).Item(Int32.Parse(key[1])));
            if(LeaderSide.IsHidden(note))hidden++;else visible++;
        }
        if(hidden==0||visible==0)throw new Exception("This regression sample must exercise both hidden and visible attachments.");
        Console.WriteLine("PASS Native visibility: "+hidden+" hidden-edge notes; "+visible+" visible-edge notes.");
    }
    [STAThread] static int Main(string[] args)
    {
        object source=null,copy=null,previous=null;bool openedSource=false;
        using(new BusyFilter())try
        {
            object app=Keep(Marshal.GetActiveObject("SolidEdge.Application")),docs=Keep(((dynamic)app).Documents);
            try{previous=Keep(((dynamic)app).ActiveDocument);}catch(COMException){}
            string sourcePath=args.Skip(1).FirstOrDefault(a=>!a.StartsWith("--",StringComparison.Ordinal));
            source=sourcePath!=null?Keep(((dynamic)docs).Open(Path.GetFullPath(sourcePath))):previous;openedSource=sourcePath!=null;
            if((int)((dynamic)source).Type!=2)throw new Exception("Activate a sample draft or supply its path.");
            includeBoltCircles=!args.Contains("--without-bc");
            var original=Capture(source);var originalCircles=CaptureBoltCircles(source);var originalMarks=CaptureProjectionMarks(source);var originalDimensions=CaptureDimensions(source);
            var rules=RuleBook.Load(Path.GetFullPath("..\\Turbo Hole Table Callouts\\Custom.xml"));
            string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
            foreach(bool dual in new[]{false,true})
            {
                string path=Path.Combine(root,dual?"Sample-dual.dft":"Sample-inch.dft");
                ((dynamic)source).SaveCopyAs(path);copy=Keep(((dynamic)docs).Open(path));
                if(args.Contains("--blank"))BlankMacroNotes(copy);
                if(args.Contains("--mixed-rules"))SeedMixedRules(copy,rules,dual);
                if(args.Contains("--duplicate-quantities"))SeedDuplicateQuantities(copy,rules,dual);
                var before=Capture(copy);var beforeCircles=CaptureBoltCircles(copy);var beforeMarks=CaptureProjectionMarks(copy);
                PrepareExpected(copy,before,rules,includeBoltCircles);
                RunReport report=TurboCallout.Automation.FillDocument(copy,rules,dual,Console.WriteLine,includeBoltCircles);Console.WriteLine(report);
                if(report.Filled+report.AlignedExisting+report.UpdatedExisting+report.StylesRemoved==0||report.Errors!=0)throw new Exception("No successful changes or apply errors in sample copy.");
                if(args.Contains("--require-bc")&&report.BoltCirclesCreated+report.BoltCirclesExisting==0)
                    throw new Exception("The sample's expected bolt circle was not recognized.");
                if(!includeBoltCircles)
                {
                    if(report.BoltCirclesEnabled||report.BoltCirclesCreated+report.BoltCirclesExisting+report.ProjectionMarksCreated+report.ProjectionMarksUpdated+report.BcDimensionsCreated+report.BcDimensionsUpdated!=0)
                        throw new Exception("Callouts-only mode performed bolt-circle work.");
                    VerifyBoltCircles(copy,beforeCircles);VerifyProjectionMarks(copy,beforeMarks);VerifyDimensions(copy,originalDimensions);
                }
                object styles=Keep(((dynamic)copy).DimensionStyles);Console.WriteLine("Remaining macro styles:");
                for(int i=1;i<=(int)((dynamic)styles).Count;i++){object style=Keep(((dynamic)styles).Item(i));string name=(string)((dynamic)style).Name;if(NoteStyles.IsManaged(name))Console.WriteLine(name);}
                Verify(copy,before,false);VerifyQuantities(copy,rules,includeBoltCircles);var filled=Capture(copy);
                if(args.Contains("--require-hidden"))VerifyMixedVisibility(copy);
                var filledCircles=CaptureBoltCircles(copy);
                var filledMarks=CaptureProjectionMarks(copy);
                if(filledMarks.Count!=beforeMarks.Count+report.ProjectionMarksCreated||beforeMarks.Any(p=>filledMarks[p.Key]!=p.Value))
                    throw new Exception("Projection creation changed the manually corrected marks.");
                if(filledCircles.Count!=beforeCircles.Count+report.BoltCirclesCreated||beforeCircles.Any(p=>filledCircles[p.Key]!=p.Value))
                    throw new Exception("Bolt-circle creation changed pre-existing annotations.");
                if(filled.Count!=before.Count)throw new Exception("Macro changed the number of callouts.");
                VerifySideMaintenance(copy,rules,filled);
                VerifyBoltCircles(copy,filledCircles);VerifyProjectionMarks(copy,filledMarks);
                ((dynamic)copy).Save();((dynamic)copy).Close(false);copy=null;
                copy=Keep(((dynamic)docs).Open(path));Verify(copy,filled,true);VerifyQuantities(copy,rules,includeBoltCircles);VerifyBoltCircles(copy,filledCircles);VerifyProjectionMarks(copy,filledMarks);
                RunReport repeat=TurboCallout.Automation.FillDocument(copy,rules,dual,null,includeBoltCircles);
                if(repeat.Filled!=0||repeat.AlignedExisting!=0||repeat.UpdatedExisting!=0||repeat.StylesRemoved!=0||repeat.Errors!=0)throw new Exception("Repeat run changed filled callouts or failed.");
                if(repeat.BoltCirclesCreated!=0||repeat.ProjectionMarksCreated!=0||repeat.ProjectionMarksUpdated!=0||repeat.BoltCirclesReview!=report.BoltCirclesReview||
                    repeat.BoltCirclesExisting<report.BoltCirclesCreated+report.BoltCirclesExisting)
                    throw new Exception("Bolt circles duplicated or lost their model-linked members after reopening.");
                VerifyBoltCircles(copy,filledCircles);
                VerifyProjectionMarks(copy,filledMarks);
                if(!includeBoltCircles)VerifyDimensions(copy,originalDimensions);
                if(includeBoltCircles)VerifyModeSwitch(copy,rules,dual);
                object window=Keep(((dynamic)app).ActiveWindow);((dynamic)window).SaveAsImage(Path.ChangeExtension(path,".jpg"),1800,1400,1.0,24,1,false);
                ((dynamic)copy).Close(false);copy=null;Console.WriteLine("PASS "+path);
            }
            Verify(source,original,true);VerifyBoltCircles(source,originalCircles);VerifyProjectionMarks(source,originalMarks);VerifyDimensions(source,originalDimensions);Console.WriteLine("PASS Original sample text, callout placements, bolt circles, dimensions and projection marks unchanged.");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
        finally
        {
            if(copy!=null)try{((dynamic)copy).Close(false);}catch{}
            if(openedSource&&source!=null)try{((dynamic)source).Close(false);}catch{}
            if(previous!=null)try{((dynamic)previous).Activate();}catch{}
            for(int i=held.Count-1;i>=0;i--)Com.Release(held[i]);
        }
    }
}
