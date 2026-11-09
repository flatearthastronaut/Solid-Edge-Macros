using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HoleTableFormatter;

// Public fake COM shape exercises ReadTitle without requiring a live SE session.
public sealed class FakeTitle { public string Value {get;set;} }
public sealed class FakeTitles
{
    public int Count {get{return 3;}}
    public FakeTitle Item(int index){return new FakeTitle{Value=index==1?"HOLE TABLE - TOP":index==2?"":"SECOND LINE"};}
}
public sealed class FakeTable {public FakeTitles Titles {get{return new FakeTitles();}}}
internal static class UiTests
{
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static RowPlan Row(int table,string label,string status)
    {return new RowPlan{TableIndex=table,Label=label,Status=status,Eligible=status=="Hole type found",Selected=status=="Hole type found",Before="%DI.25 DRILL",After="LINKED FORMAT",Reason="Test",Category="Test",Decision=new Decision{Slot=4}};}
    static void ShowOffscreen(Form form,Form owner=null)
    {
        form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);
        if(owner==null)form.Show();else form.Show(owner);
    }
    static ScanResult Regrouped(string secondLabel)
    {
        var scan=new ScanResult{Document="refresh.dft",TableCount=2};
        scan.Tables.Add(new TableInfo{Index=1,TitleText="HOLE TABLE - TOP"});
        scan.Tables.Add(new TableInfo{Index=2,TitleText="HOLE TABLE - FRONT"});
        var pin=Row(1,"A.1","Hole type found");pin.Decision.Family="Dowel";pin.Decision.Fit="PF";pin.Facts=new HoleFacts{Type=1};
        var port=Row(1,secondLabel,"Hole type found");port.Decision.Family="SAE port";port.Decision.Port=8;port.Facts=new HoleFacts{Type=2};
        scan.Rows.AddRange(new[]{pin,port,Row(2,"C.1","Already set")});return scan;
    }
    static void RefreshChecks()
    {
        int scans=0,errors=0;bool fail=false;
        ScanResult next=Regrouped("B.1");MainForm controller=null;
        using(var main=new MainForm((path,scratch)=>
        {
            scans++;Check(path=="refresh.dft","Refresh must target the original draft, not the active document.");
            Check(!controller.RefreshButton.Enabled&&controller.OwnedForms.All(f=>!f.Enabled),"All preview actions must be disabled during the scan.");
            if(fail)throw new InvalidOperationException("Simulated scan failure.");return next;
        },error=>errors++))
        {
            controller=main;ShowOffscreen(main);
            Check(!main.RefreshButton.Enabled,"Main refresh is disabled before a draft has been scanned.");
            var original=Regrouped("A.2");main.LoadScan(original,false);
            var first=main.GetTableWindow(original.Tables[0]);var second=main.GetTableWindow(original.Tables[1]);
            ShowOffscreen(first,main);ShowOffscreen(second,main);
            Check(first.MixedGroupCount==1&&first.MixedWarning.Visible,"Initial mixed group is visible before splitting.");
            Check(first.RefreshButton.Text=="Refresh"&&second.RefreshButton.Enabled,"Every table has Refresh, even with no eligible rows.");
            var bounds=first.Bounds;
            first.Grid.Rows[0].Cells[0].Value=false;
            next.Tables[0].TitleText="HOLE TABLE - TOP REGROUPED";
            first.RefreshButton.PerformClick();
            Check(scans==1&&errors==0,"A table refresh invokes exactly one new scan.");
            Check(ReferenceEquals(first,main.GetTableWindow(next.Tables[0]))&&first.Bounds==bounds,"Refresh retains the existing window and its size/location.");
            Check(first.Text.Contains("TOP REGROUPED"),"Refresh reads the updated native table title.");
            Check(first.MixedGroupCount==0&&!first.MixedWarning.Visible,"Splitting the mixed group clears its outline and warning.");
            Check(String.Join(",",first.Grid.Rows.Cast<DataGridViewRow>().Select(r=>(string)r.Cells["Hole"].Value))=="A.1,B.1","Refresh shows current hole letters in natural order.");
            Check(ReferenceEquals(first.Grid.Rows[0].Tag,next.Rows[0])&&Convert.ToBoolean(first.Grid.Rows[0].Cells[0].Value),"New plans replace old row IDs and checkbox selections.");
            Check((string)first.Grid.Rows[0].Cells["Status"].Value=="Hole type found"&&first.Grid.Rows[0].DefaultCellStyle.BackColor==Presentation.FoundColor,"Renamed status remains yellow after refreshing.");
            Check(ReferenceEquals(second.Grid.Rows[0].Tag,next.Rows[2]),"Refreshing one window updates the other open tables from the same snapshot.");
            next=Regrouped("A.2");second.RefreshButton.PerformClick();
            Check(scans==2&&first.MixedGroupCount==1&&first.MixedWarning.Visible,"Refreshing a different table also restores newly mixed warnings.");
            next=Regrouped("B.1");main.RefreshButton.PerformClick();
            Check(scans==3&&first.MixedGroupCount==0,"Main window refresh uses the same rescan behavior.");
            var preserved=first.Grid.Rows[0].Tag;fail=true;first.RefreshButton.PerformClick();
            Check(errors==1&&ReferenceEquals(first.Grid.Rows[0].Tag,preserved),"A failed refresh preserves the previous preview and reports the error.");
            Check(main.RefreshButton.Enabled&&first.Enabled&&second.Enabled,"A failed refresh restores usable window controls.");
            fail=false;next=Regrouped("B.1");next.Tables.RemoveAt(1);next.Rows.RemoveAll(r=>r.TableIndex==2);next.TableCount=1;
            first.RefreshButton.PerformClick();
            Check(second.IsDisposed&&main.Controls.OfType<ListBox>().Single().Items.Count==1,"A removed native table closes its old preview on refresh.");
            next=Regrouped("B.1");first.RefreshButton.PerformClick();
            Check(main.Controls.OfType<ListBox>().Single().Items.Count==2&&main.OwnedForms.Length==1,"New tables enter the title list without reopening closed windows.");
            main.Close();
        }
    }
    [STAThread] static int Main()
    {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        Check(Automation.ReadTitle(new FakeTable())=="HOLE TABLE - TOP"+Environment.NewLine+"SECOND LINE","Read actual title values, not the internal name.");
        var scan=new ScanResult{Document="sample.dft",TableCount=2};
        var top=new TableInfo{Index=1,TitleText="HOLE TABLE - TOP"};var side=new TableInfo{Index=2,TitleText="HOLE TABLE - SIDE"};
        scan.Tables.Add(top);scan.Tables.Add(side);
        scan.Rows.AddRange(new[]{Row(1,"D.10","Hole type found"),Row(1,"D.2","Already set"),Row(1,"A.1","Review"),Row(1,"D.3","Custom row preserved"),Row(2,"B.1","Hole type found")});
        Check(String.Join(",",Presentation.OrderRows(scan.Rows).Select(r=>r.Label))=="A.1,B.1,D.2,D.3,D.10","Natural ordering must cross status groups.");
        Check(HoleLabelComparer.Instance.Compare("A.99999999999999999999","A.100000000000000000000")<0,"Long numeric labels must not overflow.");
        Check(Presentation.Caption(top).Contains("HOLE TABLE - TOP"),"Caption must contain title text.");
        Check(Presentation.Caption(new TableInfo{Index=3,TitleText=" "})=="Untitled hole table (Table 3)","Blank titles need a distinct fallback.");
        Check(Presentation.Caption(new TableInfo{Index=2,TitleText=top.TitleText})!=Presentation.Caption(top),"Duplicate titles need distinct table numbers.");
        Check(Presentation.Display("%DI.25%RT%PM.001")=="\u00D8.25"+Environment.NewLine+"\u00B1.001","Native display symbols are preserved for preview.");
        Check(Presentation.FormatLabel(new RowPlan{SeparateLinked=true,Decision=new Decision{Family="Dowel",Category="Dowel pin (slip fit)",Slot=2}})=="Linked F2 fit","Explicit slip-fit label must retain F2 fit routing.");
        Check(Presentation.FormatLabel(new RowPlan{Decision=new Decision{Family="O-ring face seal",Slot=3}})=="%F3","Face seal must display its native format selection.");
        Check(Presentation.FormatLabel(new RowPlan{SeparateLinked=true,Decision=new Decision{Port=8}})=="Linked #8","#8 must have the correct linked-format label.");
        Check(Presentation.FormatLabel(new RowPlan{SeparateLinked=true,Decision=new Decision{Family="Spring pin"}})=="Linked spring pin","Spring-pin format must not be labeled #6.");
        Check(Presentation.HoleLetter(" AA.12 ")=="AA"&&Presentation.HoleLetter("b.3")=="B"&&Presentation.HoleLetter("12")=="","Extract letter groups without conflating numeric labels.");
        using(var first=new TableForm(scan,top,index=>0,()=>{}))using(var second=new TableForm(scan,side,index=>0,()=>{}))
        {
            Check(first.Text.Contains(top.TitleText)&&second.Text.Contains(side.TitleText),"Each window must carry its own title.");
            Check(first.Grid.Rows.Count==4&&second.Grid.Rows.Count==1,"Windows must isolate their own table rows.");
            Check((string)first.Grid.Rows[1].Cells["Hole"].Value=="D.2"&&(string)first.Grid.Rows[3].Cells["Hole"].Value=="D.10","Displayed rows use natural order.");
            var set=first.Grid.Rows[1];var ready=first.Grid.Rows[3];
            Check(set.DefaultCellStyle.BackColor==Presentation.SetColor,"Already Set row must be green.");
            Check(set.Cells[0].ReadOnly&&set.Cells[0].InheritedStyle.BackColor==Presentation.SetColor,"Read-only checkbox must remain green.");
            Check(ready.DefaultCellStyle.BackColor==Presentation.FoundColor,"Hole type found row must be yellow.");
            Check(ready.DefaultCellStyle.SelectionBackColor==Presentation.FoundColor,"Selection must retain yellow status.");
            Check((string)ready.Cells["Status"].Value=="Hole type found","Eligible rows use the requested status text.");
            Check(first.Grid.Rows[0].DefaultCellStyle.BackColor==Presentation.ReviewColor,"Unmatched/review row must be red.");
            Check(first.Grid.Rows[0].DefaultCellStyle.SelectionBackColor==Presentation.ReviewColor,"Selected review row must stay red.");
            Check(Presentation.RowColor(new RowPlan{Status="Custom row preserved",Decision=new Decision{Slot=0}})==Presentation.ReviewColor,"Unmatched custom row must be red while remaining preserved.");
            Check(Presentation.RowColor("Review format")==Presentation.ReviewColor,"Format mismatch must be visibly flagged red.");
            Check(Presentation.Display("%{.3937^.3932/LT0.0005^0/NU/@4}")==".3937/.3932","Evaluated fit limits must be legible in the preview.");
            Check(first.Grid.Rows[2].Cells[0].ReadOnly,"Custom override cannot be selected for replacement.");
            ready.Cells[0].Value=false;first.CaptureSelections();
            Check(!scan.Rows[0].Selected&&scan.Rows[4].Selected,"Changing one window must retain another window's selection.");
            ready.Cells[0].Value=true;first.CaptureSelections();
            Check(Automation.SelectedRows(scan,1).Count==1&&Automation.SelectedRows(scan,1)[0].TableIndex==1,"Apply must be restricted to its window's table.");
            Check(Automation.SelectedRows(scan,2).Count==1&&Automation.SelectedRows(scan,2)[0].TableIndex==2,"Second-table apply must not include first-table rows.");
            scan.Rows[0].Status="Applied";scan.Rows[0].Eligible=false;scan.Rows[0].Selected=false;first.Fill();
            Check(first.Grid.Rows[3].DefaultCellStyle.BackColor==Presentation.SetColor,"Applied row becomes green.");
            Check((string)first.Grid.Rows[3].Cells["Proposed callout"].Value=="LINKED FORMAT","Applied preview remains available.");
            Check(first.MixedGroupCount==0,"Status changes alone must not produce a multiple-type warning.");
        }
        var mixedScan=new ScanResult{Document="mixed.dft",TableCount=2};mixedScan.Tables.Add(top);mixedScan.Tables.Add(side);
        var pin=Row(1,"B.1","Hole type found");pin.Decision.Family="Dowel";pin.Decision.Fit="PF";pin.Facts=new HoleFacts{Type=7};
        var port=Row(1,"B.2","Hole type found");port.Decision.Family="SAE port";port.Decision.Port=4;port.Facts=new HoleFacts{Type=2};
        var plain=Row(1,"C.1","Review");plain.Decision.Slot=0;plain.Facts=new HoleFacts{Type=1};
        var bored=Row(1,"C.2","Review");bored.Decision.Slot=0;bored.Facts=new HoleFacts{Type=2};
        var otherTable=Row(2,"B.1","Hole type found");otherTable.Decision.Family="Dowel";otherTable.Decision.Fit="SF";otherTable.Facts=new HoleFacts{Type=1};
        mixedScan.Rows.AddRange(new[]{pin,port,plain,bored,otherTable});
        Check(Presentation.MixedHoleGroups(mixedScan.Rows.Where(r=>r.TableIndex==1)).SetEquals(new[]{"B","C"}),"Detect semantic types and differing native types inside one letter group.");
        using(var first=new TableForm(mixedScan,top,index=>0,()=>{}))using(var second=new TableForm(mixedScan,side,index=>0,()=>{}))
        {
            Check(first.MixedGroupCount==2&&second.MixedGroupCount==0,"Group outlines must be isolated to their own table window.");
            Check(first.MixedWarning.Text=="Multiple hole types found with the same selected hole size","Use Greg's exact warning text.");
            pin.Status="Applied";pin.Eligible=false;pin.Selected=false;first.Fill();
            Check(first.MixedGroupCount==2,"Applying a row must not remove a genuine type warning.");
        }
        var press=Row(1,"A.1","Hole type found");press.Decision.Family="Dowel";press.Decision.Fit="PF";press.Facts=new HoleFacts{Type=1};
        var slip=Row(1,"A.2","Hole type found");slip.Decision.Family="Dowel";slip.Decision.Fit="SF";slip.Facts=new HoleFacts{Type=1};
        Check(Presentation.MixedHoleGroups(new[]{press,slip}).Contains("A"),"Different fits count as different hole types even with the same native shape.");
        using(var main=new MainForm())
        {
            main.LoadScan(scan,false);Check(main.Text=="Turbo Hole Table Callouts v0.8","Renamed macro title must include current version.");
            Check(main.Controls.OfType<ListBox>().Single().Items.Count==2,"Controller must list each native table.");
        }
        RefreshChecks();
        Console.WriteLine("PASS: "+checks+" table-window, title, sorting, selection and color checks.");return 0;
    }
}
