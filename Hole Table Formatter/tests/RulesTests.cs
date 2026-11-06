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
            Preset("Pin, Dowel- 8mm PF","CounterDrill",.25,5,.31475,.47,""),
            Preset("Pin, Dowel- 5/16 SF","Simple",.31325,.31,0,0,""),
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
        Check(rules.Match(new HoleFacts{Type=7,Bore=.199,BoreDepth=.23}).Slot==0,"Wrong plug seat depth must be reviewed.");
        Check(rules.Match(new HoleFacts{Type=7,Bore=.197,BoreDepth=.22}).Slot==0,"Do not use manufacturing limits as nominal matching tolerance.");
        Check(rules.Match(new HoleFacts{Type=7,Bore=.1990000001,BoreDepth=.22}).Slot==3,"Allow floating-point serialization noise.");
        Check(rules.Match(new HoleFacts{Type=2,Bore=.682,BoreDepth=.13}).Slot==2,"#2 port selects F2.");
        Check(rules.Match(new HoleFacts{Type=2,Diameter=.25,Bore=.838,BoreDepth=.13}).Slot==4,"Legacy #4 spotface permits a different passage diameter.");
        Check(rules.Match(new HoleFacts{Type=2,Bore=.979,BoreDepth=.13}).Port==6,"#6 port retains its size identity.");
        Check(rules.Match(new HoleFacts{Type=2,Bore=.838,BoreDepth=.052}).Slot==0,"Face seal is not an SAE port.");
        Check(rules.Match(new HoleFacts{Type=5,Bore=.838,BoreDepth=.13,Thread="9/16-18 UNF"}).Slot==0,"Conflicting thread and spotface must be reviewed.");
        Check(rules.Match(new HoleFacts{Type=8,Bore=.4475,BoreDepth=.0932,Thread="7/16-20 UNF"}).Slot==4,"Recognize accurately modeled thread entrance preset.");
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
        Check(RuleBook.TemplateProblem(smallPin,new HoleFacts{Type=1},"%DI %{%HS/LT.00025^-.00025/NU/@4} DIA %ZH")==null,"Small dowel F2 fits must reproduce the chart.");
        var largePin=rules.Match(new HoleFacts{Type=1,Diameter=.4995,Depth=.68});
        Check(RuleBook.TemplateProblem(largePin,new HoleFacts{Type=1},"%DI %{%HS/LT.00025^-.00025/NU/@4} DIA %ZH")!=null,"A half-inch pin requires its larger fit interval, not the generic small-pin interval.");
        Check(RuleBook.TemplateProblem(largePin,new HoleFacts{Type=1},"%DI %{%HS/LT.0005^-.0005/NU/@4} DIA %ZH")==null,"Matching large-pin limits are usable.");
        Check(rules.Match(new HoleFacts{Type=1,Diameter=.3145,Depth=.47}).Slot==0,"Do not silently treat a fit endpoint as the nominal midpoint.");
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
