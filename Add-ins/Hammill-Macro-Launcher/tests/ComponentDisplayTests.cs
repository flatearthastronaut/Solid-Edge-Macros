using System;
using System.Collections.Generic;
using Hammill.MacroLauncher;
public sealed class DisplayItems{public object[] Values;public DisplayItems(params object[] values){Values=values;}public int Count{get{return Values.Length;}}public object Item(int i){return Values[i-1];}}
public sealed class DisplayMember
{
    public string ComponentName,VisibleEdgeStyleName="Visible",HiddenEdgeStyleName="Hidden";
    public int ComponentType,DisplayType=1;public bool DisplayAsReference,ShowHiddenEdges;
    public DisplayItems ModelMembers=new DisplayItems();
}
public sealed class DisplayView{public DisplayItems ModelMembers;}
public sealed class DisplaySheet{public DisplayItems DrawingViews;}
public sealed class DisplayDocument{public int Type=2;public DisplayItems Sheets;}
public sealed class DisplayApplication{public object ActiveDocument;}
internal static class ComponentDisplayTests
{
    public static void Run(Action<bool,string> check)
    {
        check(ComponentDisplay.Prefix("uc_Test")==1 && ComponentDisplay.Prefix("REF_Test")==1 && ComponentDisplay.Prefix("NORM_Test")==2 && ComponentDisplay.Prefix("undo_Test")==2 && ComponentDisplay.Prefix("U")==0,"component display recognizes all existing prefixes safely");
        check(ComponentDisplay.BodyTag("Body.THRD_1")=="THRD" && ComponentDisplay.BodyTag("Body.tb_2")=="TB" && ComponentDisplay.BodyTag("Design Body")=="","component body markers preserve original naming convention");
        var thread=new DisplayMember{ComponentName="Body.THRD_1",ComponentType=7};var tube=new DisplayMember{ComponentName="Body.TB_1",ComponentType=7};
        var part=new DisplayMember{ComponentName="UC_Part",ComponentType=1,ModelMembers=new DisplayItems(new DisplayMember{ComponentName="Design Body"},thread,tube)};
        var normal=new DisplayMember{ComponentName="NORM_Part",ComponentType=6,DisplayAsReference=true,VisibleEdgeStyleName="Phantom",HiddenEdgeStyleName="Phantom",ModelMembers=new DisplayItems(new DisplayMember{ComponentName="Design Body"})};
        var untouched=new DisplayMember{ComponentName="Normal",ComponentType=1,ModelMembers=new DisplayItems(new DisplayMember{ComponentName="Design Body"})};
        var sub=new DisplayMember{ComponentName="Sub",ComponentType=0,ModelMembers=new DisplayItems(part,normal,untouched)};
        var doc=new DisplayDocument{Sheets=new DisplayItems(new DisplaySheet{DrawingViews=new DisplayItems(new DisplayView{ModelMembers=new DisplayItems(sub)})})};var app=new DisplayApplication{ActiveDocument=doc};var errors=new List<string>();
        check(ComponentDisplay.BeforeCommand(app,123,errors.Add)==0 && !part.DisplayAsReference,"unrelated commands never change component display");
        int changed=ComponentDisplay.BeforeCommand(app,57083,errors.Add);
        check(changed==4 && part.DisplayAsReference && part.VisibleEdgeStyleName=="Phantom" && part.HiddenEdgeStyleName=="Phantom","Update Views applies reference styling inside nested assemblies");
        check(!normal.DisplayAsReference && normal.VisibleEdgeStyleName=="Visible" && normal.HiddenEdgeStyleName=="Hidden","normal prefixes reset part and sheet-metal reference styling");
        check(thread.DisplayType==0 && thread.VisibleEdgeStyleName=="Hidden" && !thread.ShowHiddenEdges,"thread bodies show with Hidden edge styles");
        check(tube.DisplayType==0 && tube.VisibleEdgeStyleName=="Phantom" && tube.ShowHiddenEdges,"tube bodies show hidden edges in Phantom style");
        check(!untouched.DisplayAsReference && errors.Count==0 && ComponentDisplay.BeforeCommand(app,57083,errors.Add)==0,"unmarked components unchanged and repeated update is idempotent");
        sub.ComponentName="REF_Sub";part.VisibleEdgeStyleName="Custom";ComponentDisplay.BeforeCommand(app,57083,errors.Add);
        check(sub.DisplayAsReference && part.VisibleEdgeStyleName=="Custom","assembly prefix applies to branch without overwriting child styling");
        doc.Type=3;sub.DisplayAsReference=false;check(ComponentDisplay.BeforeCommand(app,57083,errors.Add)==0 && !sub.DisplayAsReference,"component display only operates on draft documents");
    }
}
