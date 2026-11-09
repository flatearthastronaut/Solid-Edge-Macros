using System;
using System.IO;
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
        Check(rules.Match(new HoleFacts{Type=2,Bore=.838,BoreDepth=.052}).Slot==0,"Face seal is not an SAE port.");
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
        Check(RuleBook.TemplateProblem(six,new HoleFacts{Type=8,Thread="9/16-18 UNF"},"%TS THR'D %ZT MIN")==null,"Separate thread row may use native smart thread text.");
        Check(RuleBook.TemplateProblem(new Decision{Category="Dowel",Slot=2},new HoleFacts{Type=1},"")!=null,"Empty slot cannot be applied.");
        var smallPin=rules.Match(new HoleFacts{Type=1,Diameter=.31475,Depth=.47});
        Check(RuleBook.TemplateProblem(smallPin,new HoleFacts{Type=1,Diameter=.31475},"%DI %{%HS/LT.00025^-.00025/NU/@4} DIA %ZH")==null,"Small dowel F2 fits must reproduce the chart.");
        var largePin=rules.Match(new HoleFacts{Type=1,Diameter=.4995,Depth=.68});
        Check(RuleBook.TemplateProblem(largePin,new HoleFacts{Type=1,Diameter=.4995},"%DI %{%HS/LT.00025^-.00025/NU/@4} DIA %ZH")!=null,"A half-inch pin requires its larger fit interval, not the generic small-pin interval.");
        Check(RuleBook.TemplateProblem(largePin,new HoleFacts{Type=1,Diameter=.4995},"%DI %{%HS/LT.0005^-.0005/NU/@4} DIA %ZH")==null,"Matching large-pin limits are usable.");
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
        Check(sixLinked.Contains("%{%HD/NU/@3}")&&sixLinked.Contains("%{%BD/NU/@3}")&&!sixLinked.Contains("%ZH"),"Blind depth and spotface depth must stay linked and evaluable in row overrides.");
        Check(!sixLinked.Contains("[00]")&&!sixLinked.Contains("CW]]"),"Saved-callout metadata must not enter manufacturing text.");
        Check(LinkedCallouts.For(six,new HoleFacts{Type=2},"#4 7/16-20").Contains("THRU"),"Through ports must not receive an empty depth expression.");
        Check(LinkedCallouts.For(new Decision{Port=4},new HoleFacts{Type=2},"#4 7/16-20")==null,"Separate #6 fallback must not change #4 definitions.");
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
