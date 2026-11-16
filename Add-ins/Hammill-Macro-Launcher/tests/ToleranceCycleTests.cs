using System;
using System.Reflection;
using Hammill.MacroLauncher;

internal static class ToleranceCycleTests
{
    private static CycleTestDocument Document(bool metricPrimary, bool dual, int orientation)
    {
        var style = new CycleTestStyle("Original") { PrimaryUnits = metricPrimary ? 3 : 5,
            SecondaryUnits = metricPrimary ? 5 : 3, DualDisplay = dual, TextOrientation = orientation };
        return new CycleTestDocument { SelectSet = new CycleTestSelection { Values = new object[] { new CycleTestDimension { Style = style } } } };
    }
    private static void Fails(Action action, string message, Action<bool,string> check)
    {
        try { action(); } catch (InvalidOperationException) { check(true, message); return; }
        check(false, message);
    }
    internal static void Run(Action<bool,string> check)
    {
        check(LimitToleranceCycle.Next(4,".0005","-.0005",5)==.001m && LimitToleranceCycle.Next(4,".001","-.001",5)==.002m && LimitToleranceCycle.Next(4,".002","-.002",5)==0m && LimitToleranceCycle.Next(1,"0","0",5)==.0005m, "tolerances cycle through Nominal and wrap to half-thousandths");
        check(LimitToleranceCycle.Next(4,".0127","-.0127",3)==.001m && LimitToleranceCycle.Next(4,"0.0005 in","-0.0005 in",3)==.001m, "existing equivalent metric and explicit inch tolerance entries are recognized");
        check(LimitToleranceCycle.Next(2,".001","-.001",5)==.0005m && LimitToleranceCycle.Next(4,".001","+.001",5)==.0005m && LimitToleranceCycle.Next(4,"custom","note",5)==.0005m, "non-limit and unmatched values start with the first preset");
        foreach (bool metric in new[] { false, true })
        foreach (int orientation in new[] { 0, 2 })
        {
            var document = Document(metric, metric, orientation);
            var dimension = (CycleTestDimension)document.SelectSet.Item(1);
            foreach (decimal expected in new[] { .0005m,.001m,.002m,0m,.0005m })
            {
                check(LimitToleranceCycle.Run(document)==expected && dimension.NumericUpper==expected && dimension.NumericLower==-expected && dimension.DisplayType==(expected==0m?1:4), "numeric limits advance through real Nominal display and cleared offsets");
                check((metric?dimension.Style.SecondaryDecimalRoundOff:dimension.Style.PrimaryDecimalRoundOff)==(expected==.0005m?6:5), "each tolerance selects exactly four or three inch decimal places, including wraparound");
                if(metric) check(dimension.Style.PrimaryDecimalRoundOff==(expected==.0005m?5:4), "dual metric precision is always one place less than inch");
                check(dimension.Value==.0127 && dimension.Style.Name=="Original" && dimension.Style.TextOrientation==orientation && dimension.Style.PrimaryUnits==(metric?3:5) && dimension.Style.DualDisplay==metric && document.SelectSet.Item(1)==dimension, "tolerance cycling retains nominal, style family, orientation and selection");
            }
            check((metric?dimension.Style.SecondaryDecimalRoundOff:dimension.Style.PrimaryDecimalRoundOff)==6 && (metric?dimension.Style.PrimaryDecimalRoundOff==5:dimension.Style.SecondaryDecimalRoundOff==4), "half-thousandths use four inch and three metric places; hidden secondary settings are unchanged for inch-only dimensions");
            check(dimension.SuffixString==" B.C" && dimension.SubfixString=="TYP(4)" && dimension.TrackDistance==.023 && dimension.BreakDistance==.007 && dimension.Style.Writes==0, "tolerance cycle retains engineering text and placement without assigning a named style");
        }
        var doc = Document(false,true,0); var dim = (CycleTestDimension)doc.SelectSet.Item(1);
        dim.Style.PrimaryDecimalRoundOff=7;
        LimitToleranceCycle.Run(doc);
        check(dim.Style.PrimaryDecimalRoundOff==6 && dim.Style.SecondaryDecimalRoundOff==5 && dim.Style.SecondaryToleranceEquivalent, "preset replaces higher existing inch precision and keeps coherent inch-first dual units");
        dim.ToleranceChanging=()=>{ dim.ToleranceChanging=null; throw new InvalidOperationException("Partial mutation"); };
        Fails(()=>LimitToleranceCycle.Run(doc),"partial native failure is reported",check);
        check(dim.NumericUpper==.0005m && dim.NumericLower==-.0005m && dim.DisplayType==4 && dim.PrimaryUpperTolerance=="0.0005" && dim.Style.PrimaryDecimalRoundOff==6 && dim.Style.SecondaryDecimalRoundOff==5, "rollback restores calculated native limits and both precisions after partial mutation");
        dim.IgnoreUpper=true;
        Fails(()=>LimitToleranceCycle.Run(doc),"silently ignored native tolerance writes are detected",check);
        dim.IgnoreUpper=false;
        LimitToleranceCycle.Run(doc); LimitToleranceCycle.Run(doc); // +/- .002
        dim.Style.PrimaryDecimalRoundOff=7;dim.Style.SecondaryDecimalRoundOff=6;
        dim.IgnoredDisplay=1;
        Fails(()=>LimitToleranceCycle.Run(doc),"ignored Nominal display assignment is detected",check);
        check(dim.DisplayType==4 && dim.NumericUpper==.002m && dim.NumericLower==-.002m && dim.Style.PrimaryDecimalRoundOff==7 && dim.Style.SecondaryDecimalRoundOff==6,"failed Nominal step restores both numeric limits and current places");
        dim.IgnoredDisplay=-1;
        check(LimitToleranceCycle.Run(doc)==0m && dim.DisplayType==1 && dim.NumericUpper==0 && dim.NumericLower==0 && dim.Style.PrimaryDecimalRoundOff==7 && dim.Style.SecondaryDecimalRoundOff==6,"Nominal clears active offsets and preserves custom places");
        dim.ToleranceChanging=()=>{dim.ToleranceChanging=null;throw new InvalidOperationException("Nominal exit failure");};
        Fails(()=>LimitToleranceCycle.Run(doc),"failed exit from Nominal is reported",check);
        check(dim.DisplayType==1 && dim.PrimaryUpperTolerance=="0" && dim.PrimaryLowerTolerance=="0" && dim.NumericUpper==0 && dim.NumericLower==0 && dim.Style.PrimaryDecimalRoundOff==7 && dim.Style.SecondaryDecimalRoundOff==6,"failed Nominal exit restores its display, zero numeric offsets and places");
        check(LimitToleranceCycle.Run(doc)==.0005m && dim.Style.PrimaryDecimalRoundOff==6 && dim.Style.SecondaryDecimalRoundOff==5,"Nominal restarts the precise four/three-place limit preset");
        foreach (int kind in new[] {3,7,11}) { dim.DimensionType=kind; Fails(()=>LimitToleranceCycle.Run(doc),"angular tolerances are not changed",check); }
        dim.DimensionType=1; dim.OverrideString="CUSTOM"; Fails(()=>LimitToleranceCycle.Run(doc),"overridden measurements are not given calculated limits",check); dim.OverrideString="";
        dim.Style.RoundOffType=2; Fails(()=>LimitToleranceCycle.Run(doc),"fractional inch styles are not changed",check); dim.Style.RoundOffType=1;
        dim.Style.PrimaryUnits=3; dim.Style.SecondaryUnits=3; Fails(()=>LimitToleranceCycle.Run(doc),"metric-only dimensions are not changed",check); dim.Style.PrimaryUnits=5;
        dim.PrimaryUpperTolerance="CUSTOM"; Fails(()=>LimitToleranceCycle.Run(doc),"unsupported existing numeric limit text stops before mutation",check);
        doc.SelectSet.Values=new object[0]; Fails(()=>LimitToleranceCycle.Run(doc),"empty tolerance selection is explained",check);
        doc.SelectSet.Values=new object[]{dim,dim}; Fails(()=>LimitToleranceCycle.Run(doc),"multiple tolerance selections are not changed",check);
        doc.Type=3; Fails(()=>LimitToleranceCycle.Run(doc),"limit command requires a draft",check);

        doc=Document(false,false,0); dim=(CycleTestDimension)doc.SelectSet.Item(1);
        var addin=new Addin();
        typeof(Addin).GetField("hostApplication",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(addin,new CycleTestApplication{ActiveDocument=doc});
        var command=typeof(Addin).GetMethod("OnCommand",BindingFlags.Instance|BindingFlags.NonPublic);
        dim.ToleranceChanging=()=>{dim.ToleranceChanging=null;command.Invoke(addin,new object[]{Addin.LimitToleranceCommand});};
        command.Invoke(addin,new object[]{Addin.LimitToleranceCommand});
        check(dim.NumericUpper==.001m && dim.NumericLower==-.001m,"actual add-in dispatch queues a repeated click without losing it or launching a macro");
        var ribbon=new RibbonTarget();
        addin.RegisterButton(ribbon,"{08244193-B78D-11D2-9216-00C04F79BE98}",Addin.LimitToleranceCommand,"limit-tolerance-cycle",Addin.ToleranceCaption,"Drafting","Cycle",IconResources.LimitToleranceImage);
        check(ribbon.CommandId==3 && ribbon.SmallImage==12 && ribbon.LargeImage==13 && ribbon.Style==(int)SolidEdgeFramework.SeButtonStyle.seButtonIconAndCaptionBelow && ribbon.Names.Contains("\nCycle Tolerance\n"),"Cycle Tolerance caption retains its stable ID and large icon pair");
    }
}
