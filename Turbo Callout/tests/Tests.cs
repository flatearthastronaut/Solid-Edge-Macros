using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using HoleTableFormatter;
using TurboCallout;

public sealed class FakeNote
{
    public int Callout { get; set; }
    public bool DisplayByItemNumber { get; set; }
    public bool LinkToPartsList { get; set; }
    public string BalloonText { get; set; }
    public string BalloonTextLower { get; set; }
    public string BalloonTextPrefix { get; set; }
    public string BalloonTextSuffix { get; set; }
}
internal static class Tests
{
    static int checks;
    static void Check(bool value,string text){checks++;if(!value)throw new Exception(text);}
    static HoleFacts Facts(Preset p)
    {
        int type=p.Kind=="Simple"?1:p.Kind=="CounterBore"?2:p.Kind=="CounterDrill"?7:0;
        bool thread=p.Port!=0&&p.Kind=="CounterDrill";
        return new HoleFacts {Type=thread?8:type,Diameter=p.Diameter,Depth=p.Depth>0?(double?)p.Depth:null,
            Bore=type==1?null:(double?)p.Bore,BoreDepth=type==1?null:(double?)p.BoreDepth,
            Thread=thread?p.Thread:"",ThreadDepth=thread?.5:(double?)null};
    }
    static void Limits(string text,string field,double nominal,double minimum,double maximum,bool dual)
    {
        Match m=Regex.Match(text,@"%\{%"+field+@"(?:/DU)?/LT(?<hi>[+\-]?[\d.]+)\^(?<lo>[+\-]?[\d.]+)\}");
        Check(m.Success,"Native tolerance fields retained");double scale=dual?25.4:1;
        Check(Math.Abs(nominal+Double.Parse(m.Groups["hi"].Value,CultureInfo.InvariantCulture)/scale-maximum)<1e-7,"Upper manufacturing limit retained");
        Check(Math.Abs(nominal+Double.Parse(m.Groups["lo"].Value,CultureInfo.InvariantCulture)/scale-minimum)<1e-7,"Lower manufacturing limit retained");
    }
    static int Main()
    {
        try
        {
            Check(NoteAlignment.ForBreakline(true,true,0)==0,"West breakline left-aligns the note");
            Check(NoteAlignment.ForBreakline(true,true,2)==2,"East breakline right-aligns the note");
            Check(NoteAlignment.ForBreakline(true,true,1)==null,"North breakline keeps alignment");
            Check(NoteAlignment.ForBreakline(true,true,3)==null,"South breakline keeps alignment");
            Check(NoteAlignment.ForBreakline(true,true,99)==null,"Unknown direction keeps alignment");
            Check(NoteAlignment.ForBreakline(false,true,0)==null,"Leaderless note keeps alignment");
            Check(NoteAlignment.ForBreakline(true,false,2)==null,"No-breakline note keeps alignment");
            RuleBook book=RuleBook.Load("..\\Turbo Hole Table Callouts\\Custom.xml");
            foreach(Preset p in book.Presets.Where(p=>p.Family=="Dowel"))
            {
                HoleFacts hole=Facts(p);Decision choice=book.Match(hole);Check(choice.IsDowel,"Recognize "+p.Name);
                foreach(bool dual in new[]{false,true})
                {
                    NotePlan n=Notes.Build(book,hole,dual);Check(n.InchPlaces==4,"Dowel precision");
                    Check(n.Text.Contains(choice.Fit=="PF"?"P.F.":"S.F."),"Fit note");
                    Check(!n.Text.Contains("TYP(2)")&&!n.Text.Contains("FROM OPP."),"No guessed quantity or direction");
                    Check(!n.Text.Contains("/@"),"Metric precision independently controlled");
                    string field=hole.Type==1?"HS":"BS";Limits(n.Text,field,(hole.Type==1?hole.Diameter:hole.Bore).Value,choice.Minimum,choice.Maximum,dual);
                    if(!dual)Check(!n.Text.Contains("[")&&!n.Text.Contains("/DU"),"Inch-only pin fit and fields");
                }
            }
            foreach(Preset p in book.Presets.Where(p=>p.Family=="SAE port"))
            {
                var hole=Facts(p);
                foreach(bool dual in new[]{false,true})
                {
                    var note=Notes.Build(book,hole,dual);Check(note.Text.Contains("#"+p.Port),"Port size");
                    Check(note.Text.Contains(hole.Type==8?"%TS":RuleBook.PortDesignation(p.Port)),"Correct thread source");
                    Check(note.InchPlaces==((p.Port==6||p.Port==8)?2:3),"SAE inch precision");
                    Check(note.Text.Contains("%BS")&&note.Text.Contains("%BD"),"Port dimensions stay native");
                }
            }
            foreach(Preset p in book.Presets.Where(p=>p.Family=="O-ring face seal"))
            foreach(bool dual in new[]{false,true})
            {var h=Facts(p);var n=Notes.Build(book,h,dual);Limits(n.Text,"BD",h.BoreDepth.Value,.050,.054,dual);}
            foreach(Preset p in book.Presets.Where(p=>p.Family=="Koenig plug"))
            {
                var h=Facts(p);h.BoreDepth=.27;
                foreach(bool dual in new[]{false,true})
                {
                    var n=Notes.Build(book,h,dual);Check(n.Text.Split('\r').Count(s=>s==LinkedCallouts.KoenigNote)==1,"Exactly one Koenig caution");
                    Check(n.Text.Contains("%BS")&&n.Text.Contains("%BD"),"Koenig size/depth linked");
                }
            }
            const string equalSpacing="(%QC)PLC'S EQ. SP. AS SHOWN";
            foreach(Preset p in book.Presets.Where(p=>p.Family=="SHCS counterbore"))
            foreach(bool dual in new[]{false,true})
            {
                var note=Notes.Build(book,Facts(p),dual);
                Check(note.Text.Split('\r').Count(line=>line==equalSpacing)==1,"Counterbore retains exactly one saved equal-spacing quantity line");
                Check(note.Text.Contains("%HS")&&note.Text.Contains("%BS")&&note.Text.Contains("%BD")&&note.Text.Contains("%ZH"),"Counterbore size and depths remain linked");
                Check(note.Text.Split('\r').Count(line=>line=="(FOR X S.H.C.S.)")==1,"Counterbore retains exactly one literal saved screw-size line");
                Check(note.Text.EndsWith(equalSpacing+"\r(FOR X S.H.C.S.)",StringComparison.Ordinal),"Counterbore keeps screw-size line after the quantity line");
                Check(note.InchPlaces==2&&note.Dual==dual,"Counterbore precision and unit mode retained");
            }
            foreach(double? threadDepth in new double?[]{.88,null})
            foreach(bool dual in new[]{false,true})
            {
                var hole=new HoleFacts{Type=4,Diameter=.4219,Depth=threadDepth.HasValue?(double?)1.08:null,Thread="1/2-13 UNC",ThreadDepth=threadDepth};
                var note=Notes.Build(book,hole,dual);
                string saved=LinkedCallouts.ReadSaved(dual?"HC -THR'D M[I]":"HC -THR'D");
                Check(note.Text==Notes.FormatFields("%DI%HS DRILL %ZH%RT"+saved,dual).Replace("%RT","\r"),"Thread retains complete mode-specific saved note plus drilling diameter");
                Check(note.Text.Split('\r').Count(line=>line==(dual?equalSpacing:"TYP(%QC)"))==1,"Thread retains exactly one saved quantity line");
                Check(note.Text.Contains("%HS")&&note.Text.Contains("%TS")&&note.Text.Contains("%ZT")&&note.Text.Contains("%ZH"),"Blind and through thread fields stay native");
                Check(note.InchPlaces==3&&note.Dual==dual,"Thread precision and unit mode retained");
            }
            foreach(var simple in new[]{new HoleFacts{Type=1,Diameter=.3125,Depth=1},new HoleFacts{Type=1,Diameter=.4375,Depth=.75},
                new HoleFacts{Type=1,Diameter=.2,Depth=null}})
            foreach(bool dual in new[]{false,true})
            {
                var note=Notes.Build(book,simple,dual);
                Check(note.Category=="Simple drilled hole","Ordinary drilled-hole fallback");
                Check(note.Text==Notes.FormatFields(LinkedCallouts.ReadSaved(dual?"HC -DRILL M[I]":"HC -DRILL"),dual).Replace("%RT","\r"),"Complete requested saved drill note");
                Check(note.Text.Contains("%HS")&&note.Text.Contains("%ZH")&&note.Text.Contains("%QC"),"Drill size, smart depth and quantity remain linked");
                Check(note.Text.Contains(dual?"TYP(%QC)":"(%QC)PLC'S EQ. SP."),"Mode-specific drill quantity wording");
                Check(note.InchPlaces==3&&note.Dual==dual,"Drill precision and unit mode");
            }
            // A special-family conflict must never degrade to a generic drill.
            Preset existing=book.Presets.First(p=>p.Family=="Dowel"&&p.Kind=="Simple");
            var ambiguous=Facts(existing);var collision=new Preset{Family="Spring pin",Kind="Simple",Diameter=existing.Diameter,Depth=existing.Depth};
            book.Presets.Add(collision);bool rejected=false;
            try{Notes.Build(book,ambiguous,false);}catch(InvalidOperationException){rejected=true;}finally{book.Presets.Remove(collision);}
            Check(rejected,"Conflicting simple-hole families stay for review");
            foreach(var unsupported in new[]{new HoleFacts{Type=2,Diameter=.3,Bore=.7,BoreDepth=.1},new HoleFacts{Type=3,Diameter=.3,Sink=.5},
                new HoleFacts{Type=1,Diameter=.2,Thread="1/4-20 UNC"},new HoleFacts{Type=1,Diameter=.2,Bore=.5},
                new HoleFacts{Type=1,Diameter=null},new HoleFacts{Type=1,Diameter=Double.NaN},new HoleFacts{Type=1,Diameter=Double.PositiveInfinity},
                new HoleFacts{Type=1,Diameter=.2,Depth=-1}})
            {
                rejected=false;try{Notes.Build(book,unsupported,false);}catch(InvalidOperationException){rejected=true;}
                Check(rejected,"Malformed or non-simple evidence cannot use drill fallback");
            }
            Check(book.Match(new HoleFacts{Type=1,Diameter=.3125,Depth=1}).Slot==0,"Shared hole-table behavior unchanged");
            var conflict=new HoleFacts {Type=5,Diameter=.25,Bore=.979,BoreDepth=.13,Thread="7/16-20 UNF"};
            rejected=false;try{Notes.Build(book,conflict,false);}catch(InvalidOperationException){rejected=true;}Check(rejected,"Conflicting port evidence rejected");
            var threadNote=Notes.Build(book,new HoleFacts{Type=4,Diameter=.4219,Depth=1.08,Thread="1/2-13 UNC",ThreadDepth=.88},true);
            Check(threadNote.Text.Contains("%HS")&&threadNote.Text.Contains("%TS")&&threadNote.Text.Contains("%ZT"),"Thread drill size, designation and smart depth retained");
            var fake=new FakeNote{Callout=1};Check(TurboCallout.Automation.IsBlank(fake),"All-empty note eligible");
            foreach(string name in new[]{"BalloonText","BalloonTextLower","BalloonTextPrefix","BalloonTextSuffix"})
            {Property.Set(fake,name,"existing");Check(!TurboCallout.Automation.IsBlank(fake),"Protect existing "+name);Property.Set(fake,name,"");}
            fake.Callout=0;Check(!TurboCallout.Automation.IsBlank(fake),"Ignore item balloons");fake.Callout=1;
            fake.LinkToPartsList=true;Check(!TurboCallout.Automation.IsBlank(fake),"Ignore parts-list links");fake.LinkToPartsList=false;
            fake.DisplayByItemNumber=true;Check(!TurboCallout.Automation.IsBlank(fake),"Ignore item-number balloons");
            Console.WriteLine(checks+" checks passed.");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
    }
}
