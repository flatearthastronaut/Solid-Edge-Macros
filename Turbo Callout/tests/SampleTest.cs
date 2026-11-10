using System;
using System.Collections.Generic;
using System.IO;
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
                result.Add(s+"/"+n,new Snapshot{Text=new[]{(string)((dynamic)note).BalloonText,(string)((dynamic)note).BalloonTextLower,
                    (string)((dynamic)note).BalloonTextPrefix,(string)((dynamic)note).BalloonTextSuffix},Position=new Placement(note)});
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
            item.Value.Position.Verify(note);
            string[] names={"BalloonText","BalloonTextLower","BalloonTextPrefix","BalloonTextSuffix"};
            for(int i=0;i<4;i++)if(unchanged||i>0||!String.IsNullOrWhiteSpace(item.Value.Text[i]))
                if(Convert.ToString(Property.Get(note,names[i]))!=(item.Value.Text[i]??""))throw new Exception("Preserved text changed: "+item.Key+"/"+names[i]+" before=["+item.Value.Text[i]+"] after=["+Property.Get(note,names[i])+"]");
            if(!unchanged&&String.IsNullOrWhiteSpace(item.Value.Text[0]))Console.WriteLine(item.Key+": "+((dynamic)note).BalloonDisplayedText);
        }
    }
    [STAThread] static int Main(string[] args)
    {
        object source=null,copy=null;
        using(new BusyFilter())try
        {
            object app=Keep(Marshal.GetActiveObject("SolidEdge.Application")),docs=Keep(((dynamic)app).Documents);
            source=Keep(((dynamic)app).ActiveDocument);var original=Capture(source);
            var rules=RuleBook.Load(Path.GetFullPath("..\\Turbo Hole Table Callouts\\Custom.xml"));
            string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
            foreach(bool dual in new[]{false,true})
            {
                string path=Path.Combine(root,dual?"Sample-dual.dft":"Sample-inch.dft");
                ((dynamic)source).SaveCopyAs(path);copy=Keep(((dynamic)docs).Open(path));var before=Capture(copy);
                RunReport report=TurboCallout.Automation.FillDocument(copy,rules,dual,Console.WriteLine);Console.WriteLine(report);
                if(report.Filled==0||report.Errors!=0)throw new Exception("No successful callouts or apply errors in sample copy.");
                Verify(copy,before,false);((dynamic)copy).Save();((dynamic)copy).Close(false);copy=null;
                copy=Keep(((dynamic)docs).Open(path));Verify(copy,before,false);
                if(TurboCallout.Automation.FillDocument(copy,rules,dual,null).Filled!=0)throw new Exception("Repeat run changed filled callouts.");
                ((dynamic)copy).Close(false);copy=null;Console.WriteLine("PASS "+path);
            }
            Verify(source,original,true);Console.WriteLine("PASS Original sample text and all callout placements unchanged.");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
        finally
        {
            if(copy!=null)try{((dynamic)copy).Close(false);}catch{}
            if(source!=null)try{((dynamic)source).Activate();}catch{}
            for(int i=held.Count-1;i>=0;i--)Com.Release(held[i]);
        }
    }
}
