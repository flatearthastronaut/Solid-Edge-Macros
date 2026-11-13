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
    static void Typical(NotePlan note){Check(note.Text.Split('\r').Count(line=>line=="TYP(%QC)")==1,"Generated note has exactly one native TYP quantity line");}
    static HoleFacts Facts(Preset p)
    {
        int type=p.Kind=="Simple"?1:p.Kind=="CounterBore"?2:p.Kind=="CounterDrill"?7:0;
        bool thread=p.Port!=0&&p.Kind=="CounterDrill";
        return new HoleFacts {Type=thread?8:type,Diameter=p.Diameter,Depth=p.Depth>0?(double?)p.Depth:null,
            Bore=type==1?null:(double?)p.Bore,BoreDepth=type==1?null:(double?)p.BoreDepth,
            Thread=thread?p.Thread:"",ThreadDepth=thread?.5:(double?)null};
    }
    static void Limits(string text,string field,double nominal,double minimum,double maximum,bool dual,int places=4)
    {
        if(dual)
        {
            Match fixedPair=Regex.Match(text,@"%\{/ST(?<mh>[\d.]+)\^(?<ml>[\d.]+)\}\[%\{/ST(?<ih>[\d.]+)\^(?<il>[\d.]+)\}\]");
            Check(fixedPair.Success,"Dual higher-precision limits are fixed stacked text in one callout");
            foreach(var item in new[]{new{Key="mh",Value=maximum*25.4,Digits=places-1},new{Key="ml",Value=minimum*25.4,Digits=places-1},
                new{Key="ih",Value=maximum,Digits=places},new{Key="il",Value=minimum,Digits=places}})
            {
                string value=fixedPair.Groups[item.Key].Value;
                Check(value.Split('.')[1].Length==item.Digits,"Fixed limits have the requested decimal places");
                Check(Math.Abs(Double.Parse(value,CultureInfo.InvariantCulture)-Math.Round(item.Value,item.Digits,MidpointRounding.AwayFromZero))<1e-8,"Fixed limit preserves the approved manufacturing value");
            }
            Check(!Regex.IsMatch(text,@"%\{%"+field+@"(?:/|\})"),"High-precision dual value does not use the shared two-place secondary formatter");
            return;
        }
        Match m=Regex.Match(text,@"%\{%"+field+@"/LT(?<hi>[+\-]?[\d.]+)\^(?<lo>[+\-]?[\d.]+)/@"+places+@"\}");
        Check(m.Success,"Native tolerance fields retained");double scale=dual?25.4:1;
        Check(Math.Abs(nominal+Double.Parse(m.Groups["hi"].Value,CultureInfo.InvariantCulture)/scale-maximum)<1e-7,"Upper manufacturing limit retained");
        Check(Math.Abs(nominal+Double.Parse(m.Groups["lo"].Value,CultureInfo.InvariantCulture)/scale-minimum)<1e-7,"Lower manufacturing limit retained");
    }
    static int Main()
    {
        try
        {
            foreach(string text in new[]{"TYP(%QC)","SIZE\rTYP(2)","SIZE\r typ. (3) ","SIZE\nTYP (%QC)","SIZE%RTTYP(%QC)"})
                Check(Notes.EnsureTypical(text)==text,"Preserve existing TYP line verbatim");
            foreach(string text in new[]{"SIZE","SIZE\r(%QC)PLC'S EQ. SP. AS SHOWN","SIZE\r(FOR X S.H.C.S.)"})
            {
                string result=Notes.EnsureTypical(text);Check(result==text+"\rTYP(%QC)","Append one native TYP line without changing saved text");
                Check(Notes.EnsureTypical(result)==result,"TYP append is idempotent");
            }
            Check(Notes.EnsureTypical("")=="","Do not turn empty text into a quantity-only note");
            foreach(string sep in new[]{"\r","\n","\r\n","%RT"})
            foreach(var pair in new[]{
                new{Before="%DI%{%HS/DU/@1} DRILL %ZH",After="%DI%{%HS/DU/@1}  DRILL  %ZH"},
                new{Before="%DI%{/ST8.10^8.00}[%{/ST.319^.315}] DIA %{%BD/DU/@1} DP.",After="%DI%{/ST8.10^8.00}[%{/ST.319^.315}] DIA   %{%BD/DU/@1} DP."},
                new{Before=".25  DRILL  1.50 DP.",After=".25  DRILL  1.50 DP."},
                new{Before=".25    DRILL     1.50 DP. DIA    THRU",After=".25    DRILL     1.50 DP. DIA    THRU"},
                new{Before=".25 drill THRU DIA %ZH",After=".25  drill  THRU DIA   %ZH"},
                new{Before="DIA DRILL DRILL DIA",After="DIA   DRILL  DRILL  DIA   "},
                new{Before="%TS THR'D %ZT",After="%TS THR'D  %ZT"},
                new{Before="%DI%{%BS/DU/@1} SPOTFACE %{%BD/DU/@1} DP.",After="%DI%{%BS/DU/@1} SPOTFACE  %{%BD/DU/@1} DP."},
                new{Before="#4 STRAIGHT THR'D O-RING PORT",After="#4 STRAIGHT THR'D  O-RING PORT"},
                new{Before="THR'D SPOTFACE DRILL DIA THR'D",After="THR'D  SPOTFACE  DRILL  DIA   THR'D  "},
                new{Before="M4 thr'd %ZT SPOTFACE    .13 DP.",After="M4 thr'd  %ZT SPOTFACE    .13 DP."},
                new{Before="SPOTFACES %{THR'D|SPOTFACE} %SPOTFACE",After="SPOTFACES %{THR'D|SPOTFACE} %SPOTFACE"},
                new{Before="%DI%{%BS/DU/@1} C'BORE %{%BD/DU/@1} DP.",After="%DI%{%BS/DU/@1} C'BORE  %{%BD/DU/@1} DP."},
                new{Before=".60 CBORE .41 DP.",After=".60 CBORE  .41 DP."},
                new{Before=".60 c’bore .41 DP.",After=".60 c’bore  .41 DP."},
                new{Before="C'BORE    %BD DP.",After="C'BORE    %BD DP."},
                new{Before="DIA CBORE SPOTFACE THR'D C'BORE",After="DIA   CBORE  SPOTFACE  THR'D  C'BORE  "},
                new{Before="CBORED C'BORES %{C'BORE|CBORE} %CBORE",After="CBORED C'BORES %{C'BORE|CBORE} %CBORE"},
                new{Before="DRILLING DIAMETER RADIAL %DIA %DRILL %{DRILL|DIA} %{%HS/LT+.001^-.001/@4}",After="DRILLING DIAMETER RADIAL %DIA %DRILL %{DRILL|DIA} %{%HS/LT+.001^-.001/@4}"}})
            {
                string input=pair.Before+sep+"(DO NOT CHAMFER HOLE)"+sep+"TYP(%QC)",expected=pair.After+sep+"(DO NOT CHAMFER HOLE)"+sep+"TYP(%QC)";
                string spaced=Notes.ApplyWordSpacing(input);
                Check(spaced==expected,"Machining words get the requested spacing while other text/fields are preserved");
                Check(Notes.ApplyWordSpacing(spaced)==spaced,"Rerunning does not accumulate word spacing");
                Check(spaced.Replace(" ","")==input.Replace(" ",""),"Spacing changes only ordinary space characters");
            }
            Check(Notes.ApplyWordSpacing(null)==null&&Notes.ApplyWordSpacing("")=="","Empty native text fields remain empty");
            foreach(string sep in new[]{"\r","\n","\r\n","%RT"})
            foreach(string typ in new[]{"TYP(1)","TYP(%QC)"," typ. ( 1 ) ","TYP(%{%QC})"})
            {
                foreach(var pair in new[]{new{Text="SIZE"+sep+typ,Expected="SIZE"},new{Text=typ+sep+"SIZE",Expected="SIZE"},
                    new{Text="SIZE"+sep+typ+sep+"FIT",Expected="SIZE"+sep+"FIT"}})
                {
                    string result=Notes.OmitSingleTypical(pair.Text,1);
                    Check(result==pair.Expected,"Omit exactly the single-quantity line without a blank line");
                    Check(Notes.OmitSingleTypical(result,1)==result,"Single-quantity omission is idempotent");
                }
            }
            Check(Notes.OmitSingleTypical("TYP(1)",null)=="","Explicit TYP(1) needs no model-quantity guess");
            foreach(int? quantity in new int?[]{null,0,2,10})
                Check(Notes.OmitSingleTypical("SIZE\rTYP(%QC)",quantity)=="SIZE\rTYP(%QC)","Keep linked quantity unless it resolves to one");
            foreach(string text in new[]{"SIZE\rTYP(2)","TYP(10)","TYP(11)","SIZE TYP(1)","(2)PLC'S EQ. SP. AS SHOWN","(FOR X S.H.C.S.)"})
                Check(Notes.OmitSingleTypical(text,1)==text,"Preserve other counts, inline text and manufacturing lines");
            foreach(string line in new[]{"TYP(1) EACH SIDE","TYP(%QC) PLC'S EQ. SP. AS SHOWN","(%QC)PLC'S EQ. SP. AS SHOWN",
                "(%QC)PLC'S EQ. SP.","(1)PLC'S EQ. SP. AS SHOWN","PLC'S EQ. SP. AS SHOWN"})
                Check(Notes.OmitSingleTypical("SIZE\r"+line+"\r(FOR X S.H.C.S.)",1)=="SIZE\r(FOR X S.H.C.S.)","Omit the entire single-occurrence quantity/spacing line");
            foreach(int? quantity in new int?[]{null,2,3})
                Check(Notes.OmitSingleTypical("SIZE\r(%QC)PLC'S EQ. SP. AS SHOWN\rTYP(%QC)",quantity)=="SIZE\r(%QC)PLC'S EQ. SP. AS SHOWN\rTYP(%QC)","Retain linked spacing and TYP lines when count exceeds one or is unresolved");
            const string equalSpacing="(%QC)PLC'S EQ. SP. AS SHOWN";
            foreach(var choice in new[]{new Decision{Category="Threaded hole"},new Decision{Family="SHCS counterbore"}})
            {
                foreach(string sep in new[]{"\r","\n","\r\n","%RT"})
                foreach(string quantityText in new[]{"TYP(%QC)","typ. (2)","TYP(%{%QC}) PLC'S EQ. SP. AS SHOWN",equalSpacing,"(2)PLC'S EQ. SP."})
                foreach(int quantity in new[]{1,2,10})
                {
                    var input=new[]{"SIZE"+sep+quantityText+sep+"(FOR X S.H.C.S.)","","",""};
                    string[] result=Notes.ApplyQuantity(input,choice,quantity);
                    Check(result[0]=="SIZE"+sep+(quantity==1?"":equalSpacing+sep)+"(FOR X S.H.C.S.)","Thread/counterbore quantity uses spacing only, omitting one, and preserves manufacturing text/endings");
                    Check(result.SequenceEqual(Notes.ApplyQuantity(result,choice,quantity)),"Family quantity update is idempotent");
                    Check(input[0].Contains(quantityText),"Quantity formatting does not mutate its input");
                }
                for(int field=0;field<4;field++)
                {
                    var input=new[]{"SIZE","LOWER","PREFIX","SUFFIX"};
                    input[field]+="\r"+equalSpacing;input[(field+1)%4]+="\rTYP(%QC)";
                    var result=Notes.ApplyQuantity(input,choice,2);
                    Check(result[field]==input[field],"Existing spacing line stays in its original native text field");
                    Check(!result.Any(Notes.HasTypical)&&result.Sum(t=>t.Split('\r').Count(line=>line==equalSpacing))==1,"All four fields share exactly one spacing line and no TYP");
                    for(int i=0;i<4;i++)Check(result[i].StartsWith(new[]{"SIZE","LOWER","PREFIX","SUFFIX"}[i]),"Other text survives field migration");
                }
                var duplicate=Notes.ApplyQuantity(new[]{"SIZE\r"+equalSpacing,"TYP(%QC)",equalSpacing,""},choice,2);
                Check(duplicate.SequenceEqual(new[]{"SIZE\r"+equalSpacing,"","",""}),"Duplicate spacing/TYP lines consolidate across fields");
                Check(Notes.ApplyQuantity(new[]{"SIZE","","",""},choice,1)[0]=="SIZE","Single hole does not acquire a spacing line");
                Check(Notes.ApplyQuantity(new[]{"SIZE","","",""},choice,2)[0]=="SIZE\r"+equalSpacing,"Spacing returns when native quantity increases");
                Check(Notes.ApplyQuantity(new[]{"SIZE\rTYP(%QC)"},choice,null)[0]=="SIZE\r"+equalSpacing,"Unknown native count retains a linked quantity");
                Check(Notes.ApplyQuantity(new[]{"SIZE\rTYP(1)"},choice,null)[0]=="SIZE","Literal one still omits the whole quantity line");
                bool customRejected=false;
                try{Notes.ApplyQuantity(new[]{"SIZE\rTYP(2) EACH SIDE"},choice,2);}catch(InvalidOperationException){customRejected=true;}
                Check(customRejected,"Custom TYP manufacturing wording is left for review rather than discarded");
            }
            foreach(var choice in new[]{new Decision{Family="SAE port",Port=4},new Decision{Family="O-ring face seal"},new Decision{Family="Dowel"},new Decision{Family="Simple drill"}})
            {
                var input=new[]{"SIZE\r"+equalSpacing,"TYP(2)","",""};
                Check(Notes.ApplyQuantity(input,choice,2).SequenceEqual(input),"Special families and simple drills keep their existing quantity policy");
            }
            foreach(string text in new[]{"%{/ST6.350^6.337}","%{/ST.2500^.2495}","%{/ST+.054^+.050}","SIZE\r%{/ST-.001^-.003}"})
                Check(Notes.HasFixedStacks(text),"Recognize typed numeric stacks for full text height without changing values");
            foreach(string text in new[]{null,"","%{%HS/LT+.0005^-.0005/@4}","%{/ST%HS^%BS}","6.350/6.337"})
                Check(!Notes.HasFixedStacks(text),"Native limits and ordinary text keep their existing tolerance-height setting");
            Check(TurboCallout.Automation.IsEvaluatedText("%DI%{/ST6.350^6.337}[%{/ST.2500^.2495}] DIA 9.7 DP."),"Accept valid fixed numeric stacks in displayed text");
            foreach(string unresolved in new[]{"", "%{%HS/DU}", "%{/ST%HS^%BS}", "%{/ST6.35^?}", "?"})
                Check(!TurboCallout.Automation.IsEvaluatedText(unresolved),"Unresolved native fields still fail verification");
            Check(NoteStyles.IsManaged("Turbo Callout - Dual 4dp")&&NoteStyles.IsManaged("Turbo Callout - Inch 3dp (2)"),"Recognize shared macro style names");
            Check(NoteStyles.IsLegacy("Turbo Callout "+Guid.NewGuid().ToString("N")),"Recognize legacy GUID styles");
            Check(!NoteStyles.IsManaged("Turbo Callout custom")&&!NoteStyles.IsManaged("Turbo Callout Temporary "+Guid.NewGuid().ToString("N")),"Protect unrelated styles");
            Check(NoteAlignment.ForBreakline(true,true,0)==0,"West breakline left-aligns the note");
            Check(NoteAlignment.ForBreakline(true,true,2)==2,"East breakline right-aligns the note");
            Check(NoteAlignment.ForBreakline(true,true,1)==null,"North breakline keeps alignment");
            Check(NoteAlignment.ForBreakline(true,true,3)==null,"South breakline keeps alignment");
            Check(NoteAlignment.ForBreakline(true,true,99)==null,"Unknown direction keeps alignment");
            Check(NoteAlignment.ForBreakline(false,true,0)==null,"Leaderless note keeps alignment");
            Check(NoteAlignment.ForBreakline(true,false,2)==null,"No-breakline note keeps alignment");
            RuleBook book=RuleBook.Load("..\\Turbo Hole Table Callouts\\Custom.xml");
            foreach(Preset preset in book.Presets)
            {
                HoleFacts facts=Facts(preset);Decision decision=book.Match(facts);if(decision.Slot==0)continue;
                Check(Notes.InchPlaces(decision,facts,"HD")==2&&Notes.InchPlaces(decision,facts,"TD")==2,"Drilling and thread depths use two inch places");
                Check(Notes.InchPlaces(decision,facts,"BD")== (decision.Family=="O-ring face seal"?3:2),"Only face-seal recess depth uses three inch places");
                if(decision.IsDowel)Check(Notes.InchPlaces(decision,facts,facts.Type==1?"HS":"BS")==4,"Dowel diameter uses four inch places");
                else if(decision.Family=="Spring pin")Check(Notes.InchPlaces(decision,facts,"HS")==3,"Spring-pin diameter uses three inch places");
                else Check(Notes.InchPlaces(decision,facts,"HS")==2,"Pilot drill diameter uses two inch places");
                foreach(bool dual in new[]{false,true})
                {
                    NotePlan note=Notes.Build(book,facts,dual);
                    Check(note.FullSizeStacks==(dual&&(decision.IsDowel||decision.Family=="Spring pin"||decision.Family=="Koenig plug"||decision.Family=="O-ring face seal")),"Only generated typed stacks request full note text height");
                    Check(note.InchPlaces==2,"All notes share the two-place inch unit style");
                    Check(Notes.OmitSingleTypical(note.Text,1)==note.Text.Replace("\rTYP(%QC)","").Replace("\r(%QC)PLC'S EQ. SP. AS SHOWN","").Replace("\r(%QC)PLC'S EQ. SP.",""),"Every hole family omits its quantity/spacing lines at quantity one");
                    Check(Notes.OmitSingleTypical(note.Text,2)==note.Text,"Every hole family retains its linked quantity line above one");
                    Check(Notes.ApplyQuantity(new[]{note.Text},decision,2)[0]==note.Text,"New template already satisfies its family quantity policy");
                    Check(Notes.ApplyPrecision(note.Text,decision,facts,dual)==note.Text,"Precision formatting is idempotent without rescaling tolerances");
                    Check(Notes.ApplyWordSpacing(note.Text)==note.Text,"Every new hole family already has stable word spacing");
                    foreach(Match word in Regex.Matches(note.Text,@"(?<before> *)\bDRILL\b(?<after> *)"))
                        Check(word.Groups["before"].Length==2&&word.Groups["after"].Length==2,"Every template has exactly two spaces around DRILL");
                    foreach(Match word in Regex.Matches(note.Text,@"\bDIA\b(?<after> *)"))
                        Check(word.Groups["after"].Length==3,"Every template has exactly three spaces after DIA");
                    foreach(Match word in Regex.Matches(note.Text,@"\b(?:THR'D|SPOTFACE|C['’]?BORE)\b(?<after> *)"))
                        Check(word.Groups["after"].Length>=2,"Thread, spotface and counterbore templates have two following spaces");
                    foreach(Match m in Regex.Matches(note.Text,@"%\{%(?<field>HS|HD|BS|BD|TD)(?<options>[^{}]*)\}"))
                    {
                        int digits=Notes.InchPlaces(decision,facts,m.Groups["field"].Value);
                        Check(!dual||digits==2,"Only two-place native numeric fields remain in dual notes");
                        Check(m.Groups["options"].Value.EndsWith("/@"+(dual?1:digits)),"Each native machining field has its required precision");
                    }
                }
            }
            foreach(Preset p in book.Presets.Where(p=>p.Family=="Dowel"))
            {
                HoleFacts hole=Facts(p);Decision choice=book.Match(hole);Check(choice.IsDowel,"Recognize "+p.Name);
                foreach(bool dual in new[]{false,true})
                {
                    NotePlan n=Notes.Build(book,hole,dual);Typical(n);Check(n.InchPlaces==2,"Dowel depth style precision");
                    Check(n.Text.Contains(choice.Fit=="PF"?"P.F.":"S.F."),"Fit note");
                    Check(!n.Text.Contains("TYP(2)")&&!n.Text.Contains("FROM OPP."),"No guessed quantity or direction");
                    Check(n.Text.Contains(dual?"%{/ST":"/@4"),"Dowel diameter has fixed dual limits or native four-place inch limits");
                    string field=hole.Type==1?"HS":"BS";Limits(n.Text,field,(hole.Type==1?hole.Diameter:hole.Bore).Value,choice.Minimum,choice.Maximum,dual);
                    if(!dual)Check(!n.Text.Contains("[")&&!n.Text.Contains("/DU"),"Inch-only pin fit and fields");
                }
            }
            foreach(Preset p in book.Presets.Where(p=>p.Family=="SAE port"))
            {
                var hole=Facts(p);
                foreach(bool dual in new[]{false,true})
                {
                    var note=Notes.Build(book,hole,dual);Typical(note);Check(note.Text.Contains("#"+p.Port),"Port size");
                    Check(note.Text.Contains(hole.Type==8?"%TS":RuleBook.PortDesignation(p.Port)),"Correct thread source");
                    Check(note.InchPlaces==2,"SAE inch precision");
                    Check(note.Text.Contains("%BS")&&note.Text.Contains("%BD"),"Port dimensions stay native");
                }
            }
            foreach(Preset p in book.Presets.Where(p=>p.Family=="O-ring face seal"))
            foreach(bool dual in new[]{false,true})
            {var h=Facts(p);var n=Notes.Build(book,h,dual);Typical(n);Limits(n.Text,"BD",h.BoreDepth.Value,.050,.054,dual,3);}
            foreach(Preset p in book.Presets.Where(p=>p.Family=="Spring pin"))
            foreach(bool dual in new[]{false,true})
            {var n=Notes.Build(book,Facts(p),dual);Typical(n);Check(n.Text.Contains("(FOR 3/8 SPRING PIN)"),"Spring-pin purpose line retained with TYP");}
            foreach(Preset p in book.Presets.Where(p=>p.Family=="Koenig plug"))
            {
                var h=Facts(p);h.BoreDepth=.27;
                foreach(bool dual in new[]{false,true})
                {
                    var n=Notes.Build(book,h,dual);Typical(n);Check(n.Text.Split('\r').Count(s=>s==LinkedCallouts.KoenigNote)==1,"Exactly one Koenig caution");
                    Check((dual?n.Text.Contains("%{/ST"):n.Text.Contains("%BS"))&&n.Text.Contains("%BD"),"Koenig seat limits retained and depth linked");
                }
            }
            foreach(Preset p in book.Presets.Where(p=>p.Family=="SHCS counterbore"))
            foreach(bool dual in new[]{false,true})
            {
                var note=Notes.Build(book,Facts(p),dual);Check(!Notes.HasTypical(note.Text),"Counterbore has no TYP line in either mode");
                Check(note.Text.Split('\r').Count(line=>line==equalSpacing)==1,"Counterbore retains exactly one saved equal-spacing quantity line");
                Check(note.Text.Contains("%HS")&&note.Text.Contains("%BS")&&note.Text.Contains("%BD")&&note.Text.Contains("%ZH"),"Counterbore size and depths remain linked");
                Check(note.Text.Split('\r').Count(line=>line=="(FOR X S.H.C.S.)")==1,"Counterbore retains exactly one literal saved screw-size line");
                Check(note.Text.EndsWith(equalSpacing+"\r(FOR X S.H.C.S.)",StringComparison.Ordinal),"Counterbore keeps screw-size line after its only quantity line");
                Check(note.InchPlaces==2&&note.Dual==dual,"Counterbore precision and unit mode retained");
            }
            // The new sample uses the saved M8 .34/.53 diameter pair with a
            // .35 recess, rather than the preset's .34. Depth is part-specific;
            // the exact diameter pair and native unthreaded type identify it.
            var changedCounterbore=new HoleFacts{Type=2,Diameter=.34000001,Depth=2.00000001,Bore=.53000001,BoreDepth=.35000001};
            Check(book.Match(changedCounterbore).Slot==0,"Hole-table default still requires its original depth match");
            foreach(bool dual in new[]{false,true})
            {
                var note=Notes.Build(book,changedCounterbore,dual);
                Check(note.Category=="SHCS counterbore","Sample M8 counterbore recognized at .35 depth");
                Check(note.Text.Contains(dual?"%{%BD/DU/@1}":"%{%BD/@2}"),"Changed counterbore depth remains parametric in both unit modes");
                Check(note.Text.Contains("%HS")&&note.Text.Contains("%BS")&&note.Text.Contains("%ZH"),"All other counterbore machining values remain linked");
                Check(note.Text.Contains(equalSpacing)&&!Notes.HasTypical(note.Text)&&note.Text.Contains("(FOR X S.H.C.S.)"),"Variable-depth note keeps the approved quantity and purpose lines");
            }
            foreach(Preset p in book.Presets.Where(p=>p.Family=="SHCS counterbore"))
            foreach(double depthFactor in new[]{.75,1.5})
            {
                var hole=Facts(p);hole.BoreDepth=p.BoreDepth*depthFactor;
                Check(book.Match(hole).Slot==0,"Changed SHCS depths do not alter table behavior");
                Check(Notes.Recognize(book,hole).Family=="SHCS counterbore","Saved SHCS diameter pairs allow shallower and deeper recesses");
            }
            foreach(double? depth in new double?[]{null,0,-.35,Double.NaN,Double.PositiveInfinity,Double.NegativeInfinity,.052})
            {
                var hole=new HoleFacts{Type=2,Diameter=.34,Bore=.53,BoreDepth=depth};
                bool blocked=false;try{Notes.Build(book,hole,false);}catch(InvalidOperationException){blocked=true;}
                Check(blocked,"Missing/invalid depths and unidentified face-seal recesses remain for review");
            }
            foreach(var hole in new[]{new HoleFacts{Type=2,Diameter=.3401,Bore=.53,BoreDepth=.35},
                new HoleFacts{Type=2,Diameter=.34,Bore=.5301,BoreDepth=.35},
                new HoleFacts{Type=5,Diameter=.34,Bore=.53,BoreDepth=.35,Thread="M8"},
                new HoleFacts{Type=2,Diameter=.34,Bore=.53,BoreDepth=.35,Thread="M8"},
                new HoleFacts{Type=7,Diameter=.34,Bore=.53,BoreDepth=.35}})
            {
                Check(book.Match(hole,true).Family!="SHCS counterbore","Variable depth never relaxes diameter, thread or native-type evidence");
            }
            var counterboreCollision=new Preset{Family="SAE port",Kind="CounterBore",Bore=.53,Port=2,Slot=2};
            book.Presets.Add(counterboreCollision);
            try
            {
                Check(book.Match(changedCounterbore,true).Reason=="Conflicting saved preset matches.","Variable-depth SHCS candidates participate in family conflict checks");
                bool blocked=false;try{Notes.Build(book,changedCounterbore,false);}catch(InvalidOperationException){blocked=true;}
                Check(blocked,"A conflicting counterbore is never assigned a generic saved note");
            }
            finally{book.Presets.Remove(counterboreCollision);}
            foreach(double? threadDepth in new double?[]{.88,null})
            foreach(bool dual in new[]{false,true})
            {
                var hole=new HoleFacts{Type=4,Diameter=.4219,Depth=threadDepth.HasValue?(double?)1.08:null,Thread="1/2-13 UNC",ThreadDepth=threadDepth};
                var note=Notes.Build(book,hole,dual);Check(!Notes.HasTypical(note.Text),"Thread has no TYP line in either mode");
                string saved=LinkedCallouts.ReadSaved(dual?"HC -THR'D M[I]":"HC -THR'D");
                Check(note.Text==Notes.ApplyWordSpacing(Notes.ApplyPrecision(Notes.FormatFields("%DI%HS DRILL %ZH%RT"+saved.Replace("TYP(%QC)",equalSpacing),dual),Notes.Recognize(book,hole),hole,dual).Replace("%RT","\r")),"Thread retains saved machining fields with the requested spacing wording");
                Check(note.Text.Split('\r').Count(line=>line==equalSpacing)==1,"Thread uses exactly one linked spacing line in either mode");
                Check(note.Text.Contains("%HS")&&note.Text.Contains("%TS")&&note.Text.Contains("%ZT")&&note.Text.Contains("%ZH"),"Blind and through thread fields stay native");
                Check(note.InchPlaces==2&&note.Dual==dual,"Thread depth uses two-place inch precision");
            }
            foreach(var simple in new[]{new HoleFacts{Type=1,Diameter=.3125,Depth=1},new HoleFacts{Type=1,Diameter=.4375,Depth=.75},
                new HoleFacts{Type=1,Diameter=.2,Depth=null}})
            foreach(bool dual in new[]{false,true})
            {
                var note=Notes.Build(book,simple,dual);Typical(note);
                Check(note.Category=="Simple drilled hole","Ordinary drilled-hole fallback");
                Check(note.Text==Notes.ApplyWordSpacing(Notes.EnsureTypical(Notes.ApplyPrecision(Notes.FormatFields(LinkedCallouts.ReadSaved(dual?"HC -DRILL M[I]":"HC -DRILL"),dual),Notes.Recognize(book,simple),simple,dual).Replace("%RT","\r"))),"Complete requested saved drill note plus TYP");
                Check(note.Text.Contains("%HS")&&note.Text.Contains("%ZH")&&note.Text.Contains("%QC"),"Drill size, smart depth and quantity remain linked");
                Check(note.Text.Contains(dual?"TYP(%QC)":"(%QC)PLC'S EQ. SP."),"Mode-specific drill quantity wording");
                Check(note.InchPlaces==2&&note.Dual==dual,"Drill precision and unit mode");
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
