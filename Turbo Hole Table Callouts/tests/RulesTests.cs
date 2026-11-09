using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using HoleTableFormatter;

internal static class RulesTests
{
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static XElement Preset(string name,string type,double dia,double depth,double bore,double boreDepth,string thread)
    {
        return new XElement("SavedHole",new XAttribute("name",name),new XElement("Type",type),
            new XElement("Unit","StandardMeasurementUnit"),new XElement("Diameter",dia),new XElement("Depth",depth),
            new XElement("Counterbore_Diameter",bore),new XElement("Counterbore_Depth",boreDepth),
            new XElement("Counterbore_Bottom_Angle",type=="CounterDrill"?90:0),new XElement("Thread_Designation",thread));
    }
    static XDocument Data()
    {
        return new XDocument(new XElement("Settings",
            Preset("Pin, Dowel- 8mm PF","CounterDrill",.25,5,.3145,.47,""),
            Preset("Pin, Dowel- 5/16 SF","Simple",.31325,.31,0,0,""),
            Preset("Pin, Dowel- 10mm PF","CounterDrill",.3125,2,.3932,.6,""),
            Preset("Pin, Dowel- .250 PF","CounterDrill",.188,.38,.249,.38,""),
            Preset("Pin, Dowel- .250 SF","Simple",.251,.31,.25,.38,""),
            Preset("Pin, Dowel- 08mm SF","Simple",.316,.44,.25,.38,""),
            Preset("CV Hole- 05mm Plug","CounterDrill",.125,2,.199,.22,""),
            Preset("CV Hole- 08mm Plug","CounterDrill",.25,3,.317,.33,""),
            Preset("SAE Port- #2 (spotface)","CounterBore",.25,4,.682,.13,""),
            Preset("SAE Port- #4 (spotface)","CounterBore",.3125,6,.838,.13,""),
            Preset("SAE Port- #6 (spotface)","CounterBore",.375,12,.979,.13,""),
            Preset("SAE Port- #4 (thread only)","CounterDrill",.4375,.55,.4475,.0932,"7/16-20 UNF"),
            Preset("C' Bore- 1/4","CounterBore",.28,0,.41,.28,"")));
    }
    static void Main()
    {
        var rules=RuleBook.Read(Data(),"test data");
        Check(rules.Match(new HoleFacts{Type=1,Diameter=.31475,Depth=.47}).Slot==2,"Old simple dowel seat must match newer relieved preset.");
        Check(rules.Match(new HoleFacts{Type=7,Diameter=.19,Depth=9,Bore=.31475,BoreDepth=.47}).Slot==2,"Dowel relief drilling length/diameter must not change its seat identity.");
        Check(rules.Match(new HoleFacts{Type=1,Diameter=.31325,Depth=.5}).Slot==0,"A spring-pin combination must not become a dowel just because its diameter coincides.");
        Check(rules.Match(new HoleFacts{Type=7,Bore=.199,BoreDepth=.22,Depth=7}).Slot==3,"Recognize plug independently of passage length.");
        Check(rules.Match(new HoleFacts{Type=7,Bore=.199,BoreDepth=.23}).Slot==3,"Changed CV/Koenig seat depth must retain its family.");
        Check(rules.Match(new HoleFacts{Type=7,Bore=.199,BoreDepth=.052}).Slot==3,"The face-seal ambiguity applies to counterbores, not an identified CV counterdrill.");
        Check(rules.Match(new HoleFacts{Type=7,Bore=.199,BoreDepth=.6,Thread="M5"}).Slot==0,"A threaded recess must not be classified as a plug seat.");
        Check(rules.Match(new HoleFacts{Type=7,Bore=.199,BoreDepth=0}).Slot==0,"A missing recess is not a plug seat.");
        Check(rules.Match(new HoleFacts{Type=7,Bore=.197,BoreDepth=.22}).Slot==0,"Do not use manufacturing limits as nominal matching tolerance.");
        Check(rules.Match(new HoleFacts{Type=7,Bore=.1990000001,BoreDepth=.22}).Slot==3,"Allow floating-point serialization noise.");
        Check(rules.Match(new HoleFacts{Type=2,Bore=.682,BoreDepth=.13}).Slot==2,"#2 port selects F2.");
        Check(rules.Match(new HoleFacts{Type=2,Diameter=.25,Bore=.838,BoreDepth=.13}).Slot==4,"Legacy #4 spotface permits a different passage diameter.");
        Check(rules.Match(new HoleFacts{Type=2,Bore=.979,BoreDepth=.13}).Port==6,"#6 port retains its size identity.");
        Check(rules.Match(new HoleFacts{Type=2,Bore=.979,BoreDepth=.25}).Port==6,"Changed SAE spotface depth must retain port identity.");
        Check(rules.Match(new HoleFacts{Type=2,Bore=.838,BoreDepth=.31}).Port==4,"Changed legacy SAE depth is allowed.");
        Check(rules.Match(new HoleFacts{Type=2,Bore=.838,BoreDepth=.052}).Port==4,"Known SAE spotface diameter remains a port even at .052 depth.");
        Check(rules.Match(new HoleFacts{Type=5,Bore=.838,BoreDepth=.13,Thread="9/16-18 UNF"}).Slot==0,"Conflicting thread and spotface must be reviewed.");
        Check(rules.Match(new HoleFacts{Type=8,Bore=.4475,BoreDepth=.0932,Thread="7/16-20 UNF"}).Slot==4,"Recognize accurately modeled thread entrance preset.");
        Check(rules.Match(new HoleFacts{Type=8,Bore=.4475,BoreDepth=.2,Thread="7/16-20 UNF"}).Slot==4,"Changed SAE threaded entrance depth is allowed.");
        Check(rules.Match(new HoleFacts{Type=5,Bore=.838,BoreDepth=.052,Thread="7/16-20 UNF"}).Slot==4,"A matching modeled thread resolves shallow-port/face-seal ambiguity.");
        Check(rules.Match(new HoleFacts{Type=4,Thread="7/16-20 UNF"}).Slot==0,"Port-size thread alone is insufficient.");
        Check(rules.Match(new HoleFacts{Type=4,Thread="M8 x 1.25"}).Slot==1,"Ordinary native thread uses F1.");
        Check(rules.Match(new HoleFacts{Type=2,Diameter=.28,Bore=.41,BoreDepth=.28}).Slot==1,"Standard SHCS uses F1.");
        Check(rules.Match(new HoleFacts{Type=1,Diameter=.4321,Depth=.9876}).Slot==0,"Oddball hole stays unchanged.");
        Check(RuleBook.PortThread(" 7/16 - 20 UNF-2B ")==4,"Normalize thread spacing and fit suffix.");
        Check(RuleBook.PortThread("5/16-240")==0,"Do not partially match thread pitch.");
        var six=new Decision{Category="SAE port #6",Slot=4,Port=6};
        Check(RuleBook.TemplateProblem(six,new HoleFacts{Type=2},"#4 PORT 7/16-20")!=null,"F4 with #4 text cannot label a #6 port.");
        Check(RuleBook.TemplateProblem(six,new HoleFacts{Type=2},"#6 PORT 9/16-18")==null,"Matching port template is usable.");
        Check(RuleBook.TemplateProblem(six,new HoleFacts{Type=8,Thread="9/16-18 UNF"},"%TS THR'D %ZT MIN")!=null,"Thread-only port rows must explicitly use two-place depth precision too.");
        Check(RuleBook.TemplateProblem(new Decision{Category="Dowel",Slot=2},new HoleFacts{Type=1},"")!=null,"Empty slot cannot be applied.");
        var smallPin=rules.Match(new HoleFacts{Type=1,Diameter=.31475,Depth=.47});
        Check(RuleBook.TemplateProblem(smallPin,new HoleFacts{Type=1,Diameter=.31475},"%DI %{%HS/LT.00025^-.00025/NU/@4} DIA %ZH%RT"+LinkedCallouts.DowelFitNote(smallPin))==null,"Small dowel F2 fits must reproduce the chart and saved note.");
        var largePin=rules.Match(new HoleFacts{Type=1,Diameter=.4995,Depth=.68});
        Check(RuleBook.TemplateProblem(largePin,new HoleFacts{Type=1,Diameter=.4995},"%DI %{%HS/LT.00025^-.00025/NU/@4} DIA %ZH")!=null,"A half-inch pin requires its larger fit interval, not the generic small-pin interval.");
        Check(RuleBook.TemplateProblem(largePin,new HoleFacts{Type=1,Diameter=.4995},"%DI %{%HS/LT.0005^-.0005/NU/@4} DIA %ZH%RT"+LinkedCallouts.DowelFitNote(largePin))==null,"Matching large-pin limits and note are usable.");
        Check(rules.Match(new HoleFacts{Type=1,Diameter=.3145,Depth=.47}).Slot==2,"Recognize exact saved-preset endpoints without treating them as chart midpoints.");
        string counterdrillF2="%DI%{%HS/@2/NU} DRILL %ZH%RT%DI %{%BS/LT.00025^-.00025/@4/NU} DIA %{%BD/@2/NU} DP.";
        var tenFacts=new HoleFacts{Type=7,Diameter=.3125,Bore=.3932,BoreDepth=.6};
        var ten=rules.Match(tenFacts);
        Check(ten.Slot==2&&RuleBook.Near(ten.Minimum,.3932)&&RuleBook.Near(ten.Maximum,.3937),"Sample B.2/B.3/B.4 must match the 10mm preset and retain chart limits.");
        Check(RuleBook.TemplateProblem(ten,tenFacts,counterdrillF2)!=null,"Centered F2 limits on a lower endpoint must be rejected.");
        string linkedTen=LinkedCallouts.For(ten,tenFacts,counterdrillF2);
        Check(linkedTen.Contains("%{%BS/LT0.0005^0")&&linkedTen.Contains("THRU"),"Endpoint callout must retain its native size and correct asymmetric limits.");
        Check(RuleBook.TemplateProblem(ten,tenFacts,linkedTen)==null,"Adapted fit callout must satisfy the same validation as an F2 definition.");
        var quarterFacts=new HoleFacts{Type=7,Diameter=.188,Bore=.249,BoreDepth=.38};
        var quarter=rules.Match(quarterFacts);
        Check(quarter.Slot==2&&RuleBook.Near(quarter.Minimum,.2495)&&RuleBook.Near(quarter.Maximum,.25),"Sample D.1/D.2/D.3 modeled drill stock must retain the finished chart limits.");
        Check(LinkedCallouts.For(quarter,quarterFacts,counterdrillF2).Contains("/LT0.001^0.0005"),"Drill-stock nominal must not shift the finished fit interval.");
        Check(rules.Match(new HoleFacts{Type=1,Diameter=.251,Depth=.31}).Slot==2,"Sample I.1/I.2/I.3 must match the exact quarter-inch SF preset.");
        Check(rules.Match(new HoleFacts{Type=1,Diameter=.316,Depth=.44}).Slot==2,"Sample J.1/J.2/J.3 must match the exact 8mm SF preset including its saved depth.");
        Check(rules.Match(new HoleFacts{Type=1,Diameter=.3161,Depth=.44}).Slot==0,"Nearby oddball diameters must not be rounded into a dowel preset.");
        Check(rules.Match(new HoleFacts{Type=1,Diameter=.316,Depth=.50}).Slot==0,"Unknown dowel diameter/depth combination remains for review.");
        string sixLinked=LinkedCallouts.For(six,new HoleFacts{Type=2,Depth=12},"#4 7/16-20");
        Check(sixLinked.Contains("#6")&&sixLinked.Contains("9/16-18")&&!sixLinked.Contains("7/16-20"),"Separate #6 text must come from the correct saved callout.");
        Check(sixLinked.Contains("%{%HD/NU/@2}")&&sixLinked.Contains("%{%BD/NU/@2}")&&!sixLinked.Contains("%ZH"),"Blind depth and spotface depth must stay linked at two places.");
        Check(!sixLinked.Contains("[00]")&&!sixLinked.Contains("CW]]"),"Saved-callout metadata must not enter manufacturing text.");
        Check(LinkedCallouts.For(six,new HoleFacts{Type=2},"#4 7/16-20").Contains("THRU"),"Through ports must not receive an empty depth expression.");
        Check(LinkedCallouts.For(new Decision{Port=4},new HoleFacts{Type=2},"#4 7/16-20")==null,"Separate #6 fallback must not change #4 definitions.");
        // Exercise every real saved dowel preset, not just hand-picked examples.
        // The local XML is Greg's supplied snapshot; production still reads SE's
        // configured file so subsequent approved preset edits are picked up.
        string fixture=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,@"..\..\Custom.xml"));
        var actual=RuleBook.Load(fixture);
        var allSaved=actual.Presets.Where(p=>p.Family=="Dowel"&&p.Name.StartsWith("Pin, Dowel-",StringComparison.Ordinal)).ToList();
        Check(allSaved.Count==25&&allSaved.Count(p=>p.Fit=="SF")==12,"All 25 saved dowels, including all 12 slip-fit presets, must be loaded.");
        foreach(Preset saved in allSaved)
        {
            var hole=new HoleFacts{Type=saved.Kind=="Simple"?1:7,Diameter=saved.Diameter,Depth=saved.Depth,Bore=saved.Bore,BoreDepth=saved.BoreDepth};
            var match=actual.Match(hole);
            Check(match.IsDowel&&match.Slot==2&&match.Fit==saved.Fit,"Saved dowel was not identified: "+saved.Name);
            Check(match.Category=="Dowel pin ("+(saved.Fit=="SF"?"slip fit":"press fit")+")","Fit must be explicit in the displayed category: "+saved.Name);
            string format=hole.Type==1?"%DI %{%HS/LT.00025^-.00025/NU/@4} DIA %{%HD/NU/@2} DP.":counterdrillF2;
            if(RuleBook.TemplateProblem(match,hole,format)!=null)format=LinkedCallouts.For(match,hole,format);
            Check(format!=null&&RuleBook.TemplateProblem(match,hole,format)==null,"Saved dowel must have a usable linked fit: "+saved.Name);
            string fitAmount=LinkedCallouts.DowelFitNote(match).Split(new[]{" FOR "},StringSplitOptions.None)[0];
            Check(!fitAmount.Contains("[")&&!fitAmount.Contains("]")&&!fitAmount.Contains("mm"),"Every saved dowel fit amount must be inch-only: "+saved.Name);
        }
        var sealPresets=actual.Presets.Where(p=>p.Family=="O-ring face seal").ToList();
        Check(sealPresets.Count==3,"All three Face Seal presets must be loaded.");
        string sealF3="%DI%{%HS/NU/@2} DRILL %ZH%RT%DI%{%BS/NU/@2} DIA %{%BD/@3/LT.002^-.002/NU} DP.";
        foreach(Preset saved in sealPresets)
        {
            var hole=new HoleFacts{Type=2,Diameter=saved.Diameter,Depth=8,Bore=saved.Bore,BoreDepth=saved.BoreDepth};
            var match=actual.Match(hole);
            Check(match.Family=="O-ring face seal"&&match.Slot==3,"Saved face seal must select F3: "+saved.Name);
            Check(RuleBook.TemplateProblem(match,hole,sealF3)==null,"Existing F3 must preserve saved .054/.050 face-seal limits.");
            Check(RuleBook.TemplateProblem(match,hole,sealF3.Replace("/@3/","/@2/"))!=null,"Two-place rounding cannot conceal face-seal depth limits.");
            Check(RuleBook.TemplateProblem(match,hole,sealF3.Replace(".002^-.002",".003^-.003"))!=null,"Do not accept wrong face-seal depth tolerances.");
        }
        Check(actual.Match(new HoleFacts{Type=2,Diameter=.188,Bore=.375,BoreDepth=.052}).Category=="O-ring face seal","Sample D.4-D.6 must be identified with through inlet drilling.");
        Check(actual.Match(new HoleFacts{Type=2,Bore=.837,BoreDepth=.052}).Slot==0,"Unknown .052 recess diameter must not become an SAE port.");
        Check(actual.Match(new HoleFacts{Type=2,Bore=.4375,BoreDepth=.052}).Slot==0,"Do not round the saved .440 face-seal bore to a nominal fraction.");
        Check(actual.Match(new HoleFacts{Type=2,Bore=.375,BoreDepth=.06}).Slot==0,"A nonstandard face-seal depth remains unmatched.");
        Check(actual.Match(new HoleFacts{Type=5,Bore=.375,BoreDepth=.052,Thread="M8"}).Slot==0,"Threaded counterbore must not be mistaken for a face seal.");
        var springFacts=new HoleFacts{Type=1,Diameter=.378,Depth=.62};
        var spring=actual.Match(springFacts);
        Check(spring.Family=="Spring pin"&&spring.Category=="Spring pin (3/8 inch)","Top A.1/A.2 must match the saved 3/8 spring-pin preset.");
        Check(RuleBook.Near(spring.Minimum,.375)&&RuleBook.Near(spring.Maximum,.382),"Spring-pin limits must come from SP 375 PF.");
        Check(RuleBook.TemplateProblem(spring,springFacts,"%DI%{%HS/NU/@2} DRILL %ZH")!=null,"Rounded plain drill callout is insufficient for a spring pin.");
        string springText=LinkedCallouts.For(spring,springFacts,"");
        Check(springText.Contains("%{%HS/LT0.004^-0.003/NU/@3}")&&springText.Contains("3/8 SPRING PIN"),"Spring-pin expression must retain native size, saved fit limits and pin note.");
        Check(RuleBook.TemplateProblem(spring,springFacts,springText)==null&&springText.Contains("%{%HD/NU/@2}"),"Linked spring-pin preview must validate and keep its depth field.");
        Check(actual.Match(new HoleFacts{Type=1,Diameter=.3781,Depth=.62}).Slot==0,"Nearby oddball diameter must not be rounded into a spring-pin fit.");
        Check(actual.Match(new HoleFacts{Type=4,Diameter=.378,Depth=.62,Thread="M10"}).Family!="Spring pin","Threaded hole cannot be classified as the spring-pin preset.");
        var eightFacts=new HoleFacts{Type=2,Diameter=.5,Depth=12,Bore=1.181,BoreDepth=.03};
        var eight=actual.Match(eightFacts);
        Check(eight.Port==8&&eight.Category=="SAE port #8","Front B.1/B.2 must be #8 despite changed spotface depth.");
        Check(RuleBook.Near(eight.ThreadDepth,.563),"Use #8 thread extent .563, never drilling depth .689 or placeholder 2.0.");
        Check(actual.Match(new HoleFacts{Type=2,Bore=1.181,BoreDepth=.25}).Port==8,"Other positive spotface depths must keep the #8 identity.");
        Check(actual.Match(new HoleFacts{Type=2,Bore=1.181,BoreDepth=.052}).Port==8,"Do not reserve .052 depth exclusively for face seals when the diameter identifies #8.");
        Check(actual.Match(new HoleFacts{Type=2,Bore=.979,BoreDepth=.052}).Port==6,"The same varying-depth rule applies to #6 ports.");
        Check(actual.Match(new HoleFacts{Type=5,Bore=1.181,BoreDepth=.03,Thread="9/16-18 UNF"}).Slot==0,"Conflicting #6 thread and #8 spotface remain a review case.");
        Check(RuleBook.PortThread("3/4-16 UNF-2B")==8&&RuleBook.PortThread(".750-16 UNF")==8,"Recognize fractional and decimal #8 thread designations.");
        Check(RuleBook.PortThread("3/4-160")==0,"Do not partially match #8 pitch.");
        Check(actual.Match(new HoleFacts{Type=8,Bore=.7616,BoreDepth=.16,Thread="3/4-16 UNF"}).Port==8,"Accurate #8 threaded entrance supports varied depth.");
        Check(actual.Match(new HoleFacts{Type=4,Thread="3/4-16 UNF"}).Slot==0,"#8 thread alone does not prove a port.");
        string eightText=LinkedCallouts.For(eight,eightFacts,"");
        Check(eightText.Contains("#8")&&eightText.Contains("3/4-16 THR'D 0.56 DP.")&&!eightText.Contains("#4"),"Separate #8 callout must use its own standard thread and two-place depth.");
        Check(eightText.Contains("%{%BS/NU/@2}")&&eightText.Contains("%{%BD/NU/@2}")&&eightText.Contains("%{%HD/NU/@2}"),"#8 diameters and actual spotface/passage depths must stay linked at two places.");
        Check(LinkedCallouts.For(new Decision{Port=8},eightFacts,"")==null,"Missing #8 thread depth cannot be guessed.");
        Check(RuleBook.TemplateProblem(eight,eightFacts,"#6 9/16-18")!=null&&RuleBook.TemplateProblem(eight,eightFacts,eightText)==null,"Do not apply #6 shared text to #8.");
        Check(LinkedCallouts.PortHasTwoPlaces(sixLinked)&&LinkedCallouts.PortHasTwoPlaces(eightText),"Every #6/#8 linked dimensional field must use two places.");
        Check(!LinkedCallouts.PortHasTwoPlaces("#6 9/16-18 %{%BD/NU/@3}"),"An old matching #6 template must still be revised for precision.");
        Check(LinkedCallouts.Previous(eight,eightFacts,"").Contains("0.563 DP."),"Retain exact prior #8 expression for verified upgrades and rollback.");
        var threaded=new HoleFacts{Type=8,Thread="9/16-18 UNF",ThreadDepth=.53,Depth=.7};
        string threadText=LinkedCallouts.For(six,threaded,"%TS THR'D %ZT%RT%DI%{%BS/NU/@3} C'DRILL %BD DP.");
        Check(threadText.Contains("%TS")&&threadText.Contains("%{%TD/NU/@2}")&&LinkedCallouts.PortHasTwoPlaces(threadText),"Threaded-entrance rows retain native threads and two-place model depths.");
        Check(LinkedCallouts.For(six,threaded,"#4 PORT 7/16-20 %{%BD/NU/@3}")==null,"Changing precision must never approve another port's thread text.");
        Check(LinkedCallouts.DowelFitNote(ten)=="(.0005 MAX P.F. FOR 10mm DOWEL)","Use the exact saved inch press-fit amount, retaining the pin designation.");
        Check(LinkedCallouts.DowelFitNote(rules.Match(new HoleFacts{Type=1,Diameter=.251,Depth=.31}))=="(.001 MAX S.F. FOR 1/4 DOWEL)","Use exact saved inch slip-fit note.");
        Check(LinkedCallouts.HasLine(linkedTen,LinkedCallouts.DowelFitNote(ten)),"Dowel fit note must be on its own line.");
        Check(LinkedCallouts.For(ten,tenFacts,linkedTen)==linkedTen,"Rescanning a linked dowel must not duplicate its note or tolerance options.");
        string previousNote=LinkedCallouts.PreviousDowelNote(ten,tenFacts,counterdrillF2);
        Check(previousNote.Contains("(.013[.0005] MAX P.F. FOR 10mm DOWEL)"),"Retain v0.7 note text for verified updates and rollback.");
        Check(RuleBook.TemplateProblem(ten,tenFacts,previousNote)!=null,"An otherwise correct prior dual-unit note needs an inch-only update.");
        Check(LinkedCallouts.For(ten,tenFacts,previousNote)==linkedTen,"Updating a dual-unit note must preserve linked fit limits and not duplicate notes.");
        Check(LinkedCallouts.DowelFitNote(new Decision{Pin="8 mm",Fit="SF"})=="(.001 MAX S.F. FOR 8mm DOWEL)","Remove brackets from an inch-only saved amount.");
        Check(LinkedCallouts.DowelFitNote(new Decision{Pin="12 mm",Fit="SF"})=="(.0015 MAX S.F. FOR 12mm DOWEL)","Use saved .0015, not a rounded conversion of .038mm.");
        Check(LinkedCallouts.DowelFitNote(new Decision{Pin="3/8 inch",Fit="SF"})=="(.001 MAX S.F. FOR 3/8 DOWEL)","Bracketed inch-pin notes must also become plain inch amounts.");
        Check(LinkedCallouts.DowelFitNote(new Decision{Pin="1/8 inch",Fit="PF"})=="(P.F. FOR 1/8 DOWEL)","Missing saved sizes get a fit/size label without invented numeric requirements.");
        var plugFacts=new HoleFacts{Type=7,Bore=.199,BoreDepth=.22};var plug=rules.Match(plugFacts);
        string plugBase="%DI%{%HS/NU/@3} DRILL %ZH%RT%DI%{%BS/NU/@3} DIA %{%BD/NU/@3} DP.";
        Check(RuleBook.TemplateProblem(plug,plugFacts,plugBase)!=null,"Every Koenig plug must require the no-chamfer instruction.");
        string plugText=LinkedCallouts.For(plug,plugFacts,plugBase);
        Check(LinkedCallouts.HasLine(plugText,"(DO NOT CHAMFER HOLE)")&&RuleBook.TemplateProblem(plug,plugFacts,plugText)==null,"Append the exact Koenig instruction while retaining native dimensions.");
        Check(LinkedCallouts.For(plug,plugFacts,plugText)==plugText,"Do not duplicate an existing Koenig note.");
        foreach(Preset savedPlug in actual.Presets.Where(p=>p.Family=="Koenig plug"))
        {
            var hole=new HoleFacts{Type=7,Bore=savedPlug.Bore,BoreDepth=.5,Diameter=.125,Depth=4};
            string callout=LinkedCallouts.For(actual.Match(hole),hole,plugBase);
            Check(LinkedCallouts.HasLine(callout,LinkedCallouts.KoenigNote),"All saved Koenig sizes need the full instruction: "+savedPlug.Name);
        }
        var facts=HoleFacts.Parse("Counterbore",".25000000||.83800000|.13000000|||");
        Check(facts.Type==2&&facts.Depth==null&&facts.Bore==.838,"Parse exact values and preserve inapplicable fields.");
        bool bad=false;try{HoleFacts.Parse("Counterbore",".25in||||||");}catch(InvalidDataException){bad=true;}Check(bad,"Reject ambiguous numeric fields.");
        var duplicate=Data();duplicate.Root.Add(Preset("SAE Port- #6 duplicate","CounterBore",.25,3,.838,.13,""));
        Check(RuleBook.Read(duplicate,"conflict").Match(new HoleFacts{Type=2,Bore=.838,BoreDepth=.13}).Slot==0,"Conflicts must not be resolved by first match.");
        string temp=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"external-entity.xml");
        File.WriteAllText(temp,"<!DOCTYPE Settings [<!ENTITY x SYSTEM 'file:///unrelated'>]><Settings>&x;</Settings>");
        bad=false;try{RuleBook.Load(temp);}catch(System.Xml.XmlException){bad=true;}finally{File.Delete(temp);}Check(bad,"External entity expansion must be prohibited.");
        Console.WriteLine("PASS: "+checks+" recognition and template checks.");
    }
}
