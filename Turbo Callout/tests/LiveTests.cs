using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HoleTableFormatter;
using TurboCallout;

[ComImport, Guid("23C52E80-4698-11CE-B307-0800363A1E02"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface IPartKeyBinder {
 [DispId(24685)] void BindKeyToObject([In, Out, MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UI1)] ref Array key, [MarshalAs(UnmanagedType.IDispatch)] out object result);
}
internal static class LiveTests
{
    static readonly List<object> held = new List<object>();
    static object Keep(object value) { held.Add(value); return value; }
    static int checks;
    static string Rendered(object note)
    {
        object display=Keep(((dynamic)note).GetDisplayData());var text=new List<string>();
        for(int i=0;i<(int)((dynamic)display).GetTextCount();i++)
        {
            string s="";double a=0,b=0,c=0,d=0,e=0,f=0,g=0,h=0,j=0;
            ((dynamic)display).GetTextAtIndex(i,ref s,ref a,ref b,ref c,ref d,ref e,ref f,ref g,ref h,ref j);
            // Some versions include the raw source as a metadata entry. It is
            // not rendered text and must not make a precision check pass.
            if(!String.IsNullOrEmpty(s)&&!s.Contains("%"))text.Add(s);
        }
        return String.Join(" | ",text);
    }
    static void Check(bool yes, string text) { if (!yes) throw new Exception(text); checks++; Console.WriteLine("PASS " + text); }
    static double StackWidth(object note)
    {
        // The metric upper limit and its following bracket are separate native
        // text runs. Their origins measure the rendered stack width, avoiding
        // an assertion based only on the style property we just assigned.
        object display=Keep(((dynamic)note).GetDisplayData());double? start=null,end=null;
        for(int i=0;i<(int)((dynamic)display).GetTextCount();i++)
        {
            string s="";double x=0,y=0,z=0,a=0,b=0,c=0,d=0,e=0,f=0;
            ((dynamic)display).GetTextAtIndex(i,ref s,ref x,ref y,ref z,ref a,ref b,ref c,ref d,ref e,ref f);
            if(s=="6.350")start=x;if(s=="[")end=x;
        }
        if(!start.HasValue||!end.HasValue)throw new Exception("Cannot measure the rendered metric tolerance stack.");
        return Math.Abs(end.Value-start.Value);
    }
    static object Profile(object part, object plane)
    {
        object sets = Keep(((dynamic)part).ProfileSets), set = Keep(((dynamic)sets).Add()), profiles = Keep(((dynamic)set).Profiles);
        return Keep(((dynamic)profiles).Add(plane));
    }
    [STAThread] static int Main(string[] args)
    {
        string root = Path.GetFullPath(args[0]); Directory.CreateDirectory(root);
        object app=null,docs=null,previous=null,part=null,draft=null;
        using(new BusyFilter()) try
        {
            app=Keep(Marshal.GetActiveObject("SolidEdge.Application")); docs=Keep(((dynamic)app).Documents);
            try{previous=Keep(((dynamic)app).ActiveDocument);}catch(COMException){}
            part=Keep(((dynamic)docs).Add("SolidEdge.PartDocument"));
            object planes=Keep(((dynamic)part).RefPlanes), plane=Keep(((dynamic)planes).Item(3));
            object profile=Profile(part,plane), circles=Keep(((dynamic)profile).Circles2d);
            Keep(((dynamic)circles).AddByCenterRadius(0.0,0.0,.05)); ((dynamic)profile).End(1); ((dynamic)profile).Visible=false;
            Array profiles=new DispatchWrapper[]{new DispatchWrapper(profile)};object models=Keep(((dynamic)part).Models);
            object model=Keep(((dynamic)models).AddFiniteExtrudedProtrusion(1,ref profiles,2,.04));
            object hp=Profile(part,plane), pts=Keep(((dynamic)hp).Holes2d);Keep(((dynamic)pts).Add(.01,.01));((dynamic)hp).End(1);((dynamic)hp).Visible=false;
            object datas=Keep(((dynamic)part).HoleDataCollection);
            object data=Keep(((dynamic)datas).Add(33,.24975*.0254));
            ((dynamic)data).TreatmentType=44;
            object holes=Keep(((dynamic)model).Holes),hole=Keep(((dynamic)holes).AddFinite(hp,2,.38*.0254,data));
            string partPath=Path.Combine(root,"Callout-fixture.par");((dynamic)part).SaveAs(partPath);
            draft=Keep(((dynamic)docs).Add("SolidEdge.DraftDocument"));
            object links=Keep(((dynamic)draft).ModelLinks),link=Keep(((dynamic)links).Add(partPath));
            object sheet=Keep(((dynamic)draft).ActiveSheet),views=Keep(((dynamic)sheet).DrawingViews);
            object view=Keep(((dynamic)views).AddPartView(link,4,1.0,.15,.15,0));((dynamic)view).Update();
            object edges=Keep(((dynamic)view).DVCircles2d), target=null;
            for(int i=1;i<=(int)((dynamic)edges).Count;i++)
            {
                object projected=Keep(((dynamic)edges).Item(i));string file="";Array key=new byte[0];int size=0;
                ((dynamic)projected).GetReferenceKey(ref file,ref key,ref size); object actual;((IPartKeyBinder)part).BindKeyToObject(ref key,out actual);Keep(actual);
                object owner=Keep(((dynamic)actual).Parent);
                Console.WriteLine("Projected "+i+" parent="+((dynamic)owner).Type);
                if((int)((dynamic)owner).Type==462094722){target=projected;break;}
            }
            Check(target!=null,"Fixture has a native projected hole edge");
            object targetRef=Keep(((dynamic)view).GetReferenceToGraphicMember2[target]);
            double cx,cy;((dynamic)target).GetCenterPoint(out cx,out cy);
            object notes=Keep(((dynamic)sheet).Balloons);
            object first=Keep(((dynamic)notes).Add(.10,.24,0.0)),second=Keep(((dynamic)notes).Add(.24,.24,0.0));
            foreach(object note in new[]{first,second})
            {
                ((dynamic)note).Callout=1;((dynamic)note).Leader=true;((dynamic)note).BreakLine=true;
                ((dynamic)note).PromoteToCurrentVersion();
                // A zero-length newly created API callout ignores direction
                // setters. A whitespace placeholder is still blank to the
                // macro and lets the fixture retain deliberate left/right legs.
                ((dynamic)note).SetTerminator(targetRef,cx,cy,0.0,false);((dynamic)note).BalloonText=" ";
            }
            Property.Set(first,"CalloutBalloonBreaklineDirection",0); // West
            Property.Set(second,"CalloutBalloonBreaklineDirection",2); // East
            Property.Set(first,"CalloutHorizontalAlignment",2); // intentionally wrong
            Property.Set(second,"CalloutHorizontalAlignment",0);
            Property.Set(second,"BreakLineDirection",true); // initialize the legacy Add() leader too
            Console.WriteLine("Before fill: first direction="+((dynamic)first).CalloutBalloonBreaklineDirection+" align="+((dynamic)first).CalloutHorizontalAlignment+
                "; second direction="+((dynamic)second).CalloutBalloonBreaklineDirection+" align="+((dynamic)second).CalloutHorizontalAlignment);
            object otherStyle=Keep(((dynamic)second).Style);int untouchedUnits=(int)((dynamic)otherStyle).PrimaryUnits;
            RuleBook rules=RuleBook.Load(Path.GetFullPath("..\\Turbo Hole Table Callouts\\Custom.xml"));
            var firstPlace=new Placement(first); var secondPlace=new Placement(second);
            string originalStyle=(string)((dynamic)otherStyle).Name;
            bool rolledBack=false;
            try {TurboCallout.Automation.Fill(second,new NotePlan {Text="",InchPlaces=4,Dual=true});}
            catch(InvalidOperationException){rolledBack=true;}
            secondPlace.Verify(second);
            Check(rolledBack&&TurboCallout.Automation.IsBlank(second)&&(string)((dynamic)otherStyle).Name==originalStyle,"Failed evaluation restores blank text, style and placement");
            using(var reader=new NativeHole(draft))
            {
                int? quantity;HoleFacts facts=reader.Read(first,out quantity); Console.WriteLine("facts="+facts.Type+" diameter="+facts.Diameter+" depth="+facts.Depth);
                Check(quantity==1,"Native hole probe reads quantity one");
                Check(rules.Match(facts).IsDowel,"Native attachment resolves to the saved dowel preset");
                TurboCallout.Automation.Fill(first,Notes.Build(rules,facts,false));
                firstPlace.Verify(first,0);
                Check((int)((dynamic)first).CalloutHorizontalAlignment==0,"Left breakline receives left-aligned text");
                Console.WriteLine("Inch: "+((dynamic)first).BalloonDisplayedText);
                Console.WriteLine("Raw: "+((dynamic)first).BalloonText);
                Console.WriteLine("Rendered inch: "+Rendered(first));
                Check(Rendered(first).Contains(".2500"),"Inch note evaluates finished upper limit");
                Check(Rendered(first).Contains(".38")&&!Rendered(first).Contains(".380"),"Inch dowel depth renders two places alongside four-place limits");
                Check((int)((dynamic)otherStyle).PrimaryUnits==untouchedUnits,"Per-callout unit change leaves other annotation style unchanged");
                TurboCallout.Automation.Fill(second,Notes.Build(rules,reader.Read(second),true));
                Console.WriteLine("After fill second direction="+((dynamic)second).CalloutBalloonBreaklineDirection+" align="+((dynamic)second).CalloutHorizontalAlignment);
                secondPlace.Verify(second,2);
                Check((int)((dynamic)second).CalloutHorizontalAlignment==2,"Right breakline receives right-aligned text");
                Console.WriteLine("Dual: "+((dynamic)second).BalloonDisplayedText);
                Console.WriteLine("Rendered dual: "+Rendered(second));
                Check(Rendered(second).Contains(".2500"),"Dual note retains four-place inch limit");
                Check(Rendered(second).Contains("6.35"),"Dual note displays metric equivalent");
                Check(Rendered(second).Contains("6.350")&&Rendered(second).Contains("6.337")&&Rendered(second).Contains("9.7")&&
                    Rendered(second).Contains(".38")&&!Rendered(second).Contains(".380"),"Single dual callout mixes fixed four/three-place limits with linked two/one-place depth");
                Check(Property.Equal(Property.Get(otherStyle,"Height"),.125*.0254)&&Property.Equal(Property.Get(otherStyle,"ToleranceHeightRatio"),1.0),"Typed stacks use the full .125-inch note height");
                double fullWidth=StackWidth(second),smallWidth;string unchanged=(string)((dynamic)second).BalloonText;
                try{Property.Set(otherStyle,"ToleranceHeightRatio",.8);smallWidth=StackWidth(second);}
                finally{Property.Set(otherStyle,"ToleranceHeightRatio",1.0);}
                Check(Math.Abs(fullWidth/smallWidth-1.25)<1e-6&&(string)((dynamic)second).BalloonText==unchanged,"Native rendered stack grows from 80 to 100 percent while all typed values remain unchanged");
            }
            Check(!Notes.HasTypical((string)((dynamic)first).BalloonText)&&!Notes.HasTypical((string)((dynamic)second).BalloonText),"New inch and dual callouts omit TYP when quantity is one");
            Check((int)((dynamic)notes).Count==2,"Each filled leader remains exactly one native callout");
            object styleCollection=Keep(((dynamic)draft).DimensionStyles),firstStyle=Keep(((dynamic)first).Style);
            string sharedName=(string)((dynamic)firstStyle).Name;
            Check(sharedName=="Turbo Callout - Inch 2dp"&&(string)((dynamic)otherStyle).Name=="Turbo Callout - Dual 2dp","Shared styles have readable two-place unit names");
            int styleCount=(int)((dynamic)styleCollection).Count;
            object duplicate=Keep(((dynamic)first).Duplicate(.01,0.0)),duplicateStyle=Keep(((dynamic)duplicate).Style);
            ((dynamic)duplicate).SetTerminator(targetRef,cx,cy,0.0,false);((dynamic)duplicate).BalloonText=" ";
            using(var reader=new NativeHole(draft))TurboCallout.Automation.Fill(duplicate,Notes.Build(rules,reader.Read(duplicate),false));
            Check((string)((dynamic)duplicateStyle).Name==sharedName&&(int)((dynamic)styleCollection).Count==styleCount,"Identical callouts reuse a style without growing the style list");
            ((dynamic)duplicate).BalloonText=" ";bool reuseFailed=false;
            try{TurboCallout.Automation.Fill(duplicate,new NotePlan{Text="",InchPlaces=4,Dual=false});}catch(InvalidOperationException){reuseFailed=true;}
            Check(reuseFailed&&(int)((dynamic)styleCollection).Count==styleCount&&(string)((dynamic)firstStyle).Name==sharedName,"Failed fill never removes a shared style used by another note");
            ((dynamic)duplicate).BalloonText=(string)((dynamic)first).BalloonText+"\rTYP(2)\r(%QC)PLC'S EQ. SP. AS SHOWN\rTYP(%QC)\r(FOR X S.H.C.S.)";
            ((dynamic)duplicate).BalloonTextLower="TYP(1)";
            string legacyName="Turbo Callout "+Guid.NewGuid().ToString("N");Keep(((dynamic)styleCollection).Add(legacyName,sharedName));
            ((dynamic)duplicateStyle).Name=legacyName;Property.Set(duplicateStyle,"Height",(double)Property.Get(duplicateStyle,"Height")*1.1);
            var legacyFormat=new StyleValues(duplicateStyle);var legacyPosition=new Placement(duplicate);string legacyText=(string)((dynamic)duplicate).BalloonText;
            string unusedName="Turbo Callout "+Guid.NewGuid().ToString("N");Keep(((dynamic)styleCollection).Add(unusedName,sharedName));
            // An item balloon is outside Turbo Callout's scope. Native removal
            // must protect its legacy style even during requested cleanup.
            string protectedName="Turbo Callout "+Guid.NewGuid().ToString("N");Keep(((dynamic)styleCollection).Add(protectedName,sharedName));
            object protectedBalloon=Keep(((dynamic)notes).Add(.04,.04,0.0));((dynamic)protectedBalloon).Callout=0;((dynamic)protectedBalloon).BalloonText="42";
            object protectedStyle=Keep(((dynamic)protectedBalloon).Style);((dynamic)protectedStyle).Name=protectedName;
            object allSheets=Keep(((dynamic)draft).Sheets), extraSheet=Keep(((dynamic)allSheets).Add("Other working sheet"));
            object extraNotes=Keep(((dynamic)extraSheet).Balloons),unattached=Keep(((dynamic)extraNotes).Add(.03,.03,0.0));
            ((dynamic)unattached).Callout=1;((dynamic)unattached).BalloonText="";
            object custom=Keep(((dynamic)notes).Add(.07,.07,0.0));
            ((dynamic)custom).Callout=1;((dynamic)custom).BalloonText="EXISTING CUSTOM NOTE";
            object customStyle=Keep(((dynamic)custom).Style);((dynamic)customStyle).Name=originalStyle;
            ((dynamic)custom).Leader=true;((dynamic)custom).BreakLine=true;Property.Set(custom,"CalloutBalloonBreaklineDirection",0);
            ((dynamic)custom).SetTerminator(targetRef,cx,cy,0.0,false);
            Property.Set(custom,"CalloutHorizontalAlignment",2);var customPlace=new Placement(custom);
            // Simulate notes already filled with the v0.3 alignment rule. A rerun must correct
            // only justification, retaining their raw text and selected units.
            string firstText=(string)((dynamic)first).BalloonText,secondText=(string)((dynamic)second).BalloonText;
            string secondStyle=(string)((dynamic)otherStyle).Name;
            Property.Set(first,"CalloutHorizontalAlignment",2);Property.Set(second,"CalloutHorizontalAlignment",0);
            RunReport repaired=TurboCallout.Automation.FillDocument(draft,rules,false,null);
            Check(repaired.Filled==0&&repaired.AlignedExisting==2&&repaired.Errors==0,"Rerun repairs existing macro notes in both directions");
            Check(repaired.UpdatedExisting==1&&repaired.StylesRemoved==2&&repaired.StylesKept==1,"Rerun consolidates legacy styles and removes only unused styles");
            Check((string)((dynamic)duplicateStyle).Name==sharedName&&legacyFormat.Matches(new StyleValues(duplicateStyle)),"Legacy migration preserves all readable local formatting overrides");
            legacyPosition.Verify(duplicate);Check((string)((dynamic)duplicate).BalloonText==Notes.OmitSingleTypical(legacyText,1),"Existing TYP(1) and linked spacing lines are removed with placement preserved");
            Check(String.IsNullOrEmpty((string)((dynamic)duplicate).BalloonTextLower)&&((string)((dynamic)duplicate).BalloonText).Contains("TYP(2)"),"Other text fields omit literal TYP(1) while other fixed quantities remain");
            Check((string)((dynamic)protectedStyle).Name==protectedName&&(string)((dynamic)protectedBalloon).BalloonText=="42","Style used by an unrelated item balloon is protected");
            Check((string)((dynamic)first).BalloonText==firstText&&(string)((dynamic)second).BalloonText==secondText&&
                (string)((dynamic)otherStyle).Name==secondStyle,"Alignment-only repair preserves note text and unit style");
            customPlace.Verify(custom);Check(true,"Unrelated existing custom note remains unchanged");
            RunReport repeated=TurboCallout.Automation.FillDocument(draft,rules,false,null);
            Check(repeated.Filled==0&&repeated.AlignedExisting==0&&repeated.UpdatedExisting==0&&repeated.StylesRemoved==0,"Repeated run preserves filled callouts and style count");
            Check(repeated.Blank==1&&repeated.Review==1,"All working sheets scanned; unattached blank remains for review");
            ((dynamic)sheet).Activate();
            string draftPath=Path.Combine(root,"Callout-fixture.dft");((dynamic)draft).SaveAs(draftPath);((dynamic)draft).Close(false);draft=null;
            draft=Keep(((dynamic)docs).Open(draftPath));sheet=Keep(((dynamic)draft).ActiveSheet);notes=Keep(((dynamic)sheet).Balloons);
            first=Keep(((dynamic)notes).Item(1));second=Keep(((dynamic)notes).Item(2));
            firstPlace.Verify(first,0);secondPlace.Verify(second,2);Check(true,"Both callout alignments and leader positions survive save/reopen");
            using(var reader=new NativeHole(draft))Check(rules.Match(reader.Read(first)).IsDowel,"Model association survives save/reopen");
            Check(((string)((dynamic)first).BalloonText).Contains("%HS"),"Saved callout retains native linked fields");
            object savedStyle=Keep(((dynamic)first).Style);Check((string)((dynamic)savedStyle).Name==sharedName&&!Notes.HasTypical((string)((dynamic)first).BalloonText),"Shared style and omitted TYP line survive save/reopen");
            // Change only this disposable fixture model. The dual limit text
            // must stay fixed as requested, while both notes' depths and the
            // inch-only native diameter continue to follow their model fields.
            ((dynamic)data).HoleDiameter=.25975*.0254;((dynamic)hole).Depth=.52*.0254;((dynamic)part).Save();
            object reopenedLinks=Keep(((dynamic)draft).ModelLinks),reopenedLink=Keep(((dynamic)reopenedLinks).Item(1));
            ((dynamic)reopenedLink).ForceUpdateViews();
            Check(Rendered(first).Contains(".2600")&&Rendered(first).Contains(".52"),"Inch diameter and two-place depth update from the model");
            Check(Rendered(second).Contains(".2500")&&Rendered(second).Contains("6.350")&&!Rendered(second).Contains(".2600")&&
                Rendered(second).Contains("13.2")&&Rendered(second).Contains(".52"),"Dual typed limits remain fixed while its depth updates in both units");
            ((dynamic)data).HoleDiameter=.24975*.0254;((dynamic)hole).Depth=.38*.0254;((dynamic)part).Save();
            ((dynamic)reopenedLink).ForceUpdateViews();((dynamic)draft).Save();
            Console.WriteLine(checks+" live checks passed.");return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
        finally
        {
            if(draft!=null)try{((dynamic)draft).Close(false);}catch{}
            if(part!=null)try{((dynamic)part).Close(false);}catch{}
            if(previous!=null)try{((dynamic)previous).Activate();}catch{}
            for(int i=held.Count-1;i>=0;i--)Com.Release(held[i]);
        }
    }
}
