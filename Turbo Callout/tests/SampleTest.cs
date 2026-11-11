using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HoleTableFormatter;
using TurboCallout;

internal static class SampleTest
{
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
            item.Value.Position.Verify(note,alignment);
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
    static void PrepareExpected(object doc,Dictionary<string,Snapshot> before,RuleBook rules)
    {
        object sheets=Keep(((dynamic)doc).Sheets);
        using(var reader=new NativeHole(doc))foreach(var item in before)
        {
            if(!item.Value.Macro||!item.Value.Attached)continue;
            string[] key=item.Key.Split('/');object sheet=Keep(((dynamic)sheets).Item(Int32.Parse(key[0])));
            object notes=Keep(((dynamic)sheet).Balloons),note=Keep(((dynamic)notes).Item(Int32.Parse(key[1]))),style=Keep(((dynamic)note).Style);
            bool dual=(bool)Property.Get(style,"DualDisplay");HoleFacts hole=reader.Read(note);Decision choice=Notes.Recognize(rules,hole);
            item.Value.ExpectedText=item.Value.Text.Select(t=>Notes.ApplyPrecision(t,choice,hole,dual)).ToArray();
            if(!item.Value.Text.Any(Notes.HasTypical))item.Value.ExpectedText[0]=Notes.EnsureTypical(item.Value.ExpectedText[0]);
            item.Value.ExpectedFormat=new StyleValues(style);
            new UnitStyle(style,new NotePlan{Dual=dual,InchPlaces=2}).ApplyToSnapshot(item.Value.ExpectedFormat);
        }
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
    [STAThread] static int Main(string[] args)
    {
        object source=null,copy=null,previous=null;bool openedSource=false;
        using(new BusyFilter())try
        {
            object app=Keep(Marshal.GetActiveObject("SolidEdge.Application")),docs=Keep(((dynamic)app).Documents);
            try{previous=Keep(((dynamic)app).ActiveDocument);}catch(COMException){}
            source=args.Length>1?Keep(((dynamic)docs).Open(Path.GetFullPath(args[1]))):previous;openedSource=args.Length>1;
            if((int)((dynamic)source).Type!=2)throw new Exception("Activate a sample draft or supply its path.");
            var original=Capture(source);
            var rules=RuleBook.Load(Path.GetFullPath("..\\Turbo Hole Table Callouts\\Custom.xml"));
            string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
            foreach(bool dual in new[]{false,true})
            {
                string path=Path.Combine(root,dual?"Sample-dual.dft":"Sample-inch.dft");
                ((dynamic)source).SaveCopyAs(path);copy=Keep(((dynamic)docs).Open(path));
                if(args.Length>2&&args[2]=="--blank")BlankMacroNotes(copy);
                var before=Capture(copy);
                PrepareExpected(copy,before,rules);
                RunReport report=TurboCallout.Automation.FillDocument(copy,rules,dual,Console.WriteLine);Console.WriteLine(report);
                if(report.Filled+report.AlignedExisting+report.UpdatedExisting+report.StylesRemoved==0||report.Errors!=0)throw new Exception("No successful changes or apply errors in sample copy.");
                object styles=Keep(((dynamic)copy).DimensionStyles);Console.WriteLine("Remaining macro styles:");
                for(int i=1;i<=(int)((dynamic)styles).Count;i++){object style=Keep(((dynamic)styles).Item(i));string name=(string)((dynamic)style).Name;if(NoteStyles.IsManaged(name))Console.WriteLine(name);}
                Verify(copy,before,false);var filled=Capture(copy);
                if(filled.Count!=before.Count)throw new Exception("Macro changed the number of callouts.");
                ((dynamic)copy).Save();((dynamic)copy).Close(false);copy=null;
                copy=Keep(((dynamic)docs).Open(path));Verify(copy,filled,true);
                RunReport repeat=TurboCallout.Automation.FillDocument(copy,rules,dual,null);
                if(repeat.Filled!=0||repeat.AlignedExisting!=0||repeat.UpdatedExisting!=0||repeat.StylesRemoved!=0||repeat.Errors!=0)throw new Exception("Repeat run changed filled callouts or failed.");
                ((dynamic)copy).Close(false);copy=null;Console.WriteLine("PASS "+path);
            }
            Verify(source,original,true);Console.WriteLine("PASS Original sample text and all callout placements unchanged.");return 0;
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
