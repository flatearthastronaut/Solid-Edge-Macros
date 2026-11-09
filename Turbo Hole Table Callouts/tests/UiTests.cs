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
    {return new RowPlan{TableIndex=table,Label=label,Status=status,Eligible=status=="Ready",Selected=status=="Ready",Before="%DI.25 DRILL",After="LINKED FORMAT",Reason="Test",Category="Test",Decision=new Decision{Slot=4}};}
    [STAThread] static int Main()
    {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        Check(Automation.ReadTitle(new FakeTable())=="HOLE TABLE - TOP"+Environment.NewLine+"SECOND LINE","Read actual title values, not the internal name.");
        var scan=new ScanResult{Document="sample.dft",TableCount=2};
        var top=new TableInfo{Index=1,TitleText="HOLE TABLE - TOP"};var side=new TableInfo{Index=2,TitleText="HOLE TABLE - SIDE"};
        scan.Tables.Add(top);scan.Tables.Add(side);
        scan.Rows.AddRange(new[]{Row(1,"D.10","Ready"),Row(1,"D.2","Already set"),Row(1,"A.1","Review"),Row(1,"D.3","Custom row preserved"),Row(2,"B.1","Ready")});
        Check(String.Join(",",Presentation.OrderRows(scan.Rows).Select(r=>r.Label))=="A.1,B.1,D.2,D.3,D.10","Natural ordering must cross status groups.");
        Check(HoleLabelComparer.Instance.Compare("A.99999999999999999999","A.100000000000000000000")<0,"Long numeric labels must not overflow.");
        Check(Presentation.Caption(top).Contains("HOLE TABLE - TOP"),"Caption must contain title text.");
        Check(Presentation.Caption(new TableInfo{Index=3,TitleText=" "})=="Untitled hole table (Table 3)","Blank titles need a distinct fallback.");
        Check(Presentation.Caption(new TableInfo{Index=2,TitleText=top.TitleText})!=Presentation.Caption(top),"Duplicate titles need distinct table numbers.");
        Check(Presentation.Display("%DI.25%RT%PM.001")=="\u00D8.25"+Environment.NewLine+"\u00B1.001","Native display symbols are preserved for preview.");
        using(var first=new TableForm(scan,top,index=>0))using(var second=new TableForm(scan,side,index=>0))
        {
            Check(first.Text.Contains(top.TitleText)&&second.Text.Contains(side.TitleText),"Each window must carry its own title.");
            Check(first.Grid.Rows.Count==4&&second.Grid.Rows.Count==1,"Windows must isolate their own table rows.");
            Check((string)first.Grid.Rows[1].Cells["Hole"].Value=="D.2"&&(string)first.Grid.Rows[3].Cells["Hole"].Value=="D.10","Displayed rows use natural order.");
            var set=first.Grid.Rows[1];var ready=first.Grid.Rows[3];
            Check(set.DefaultCellStyle.BackColor==Presentation.SetColor,"Already Set row must be green.");
            Check(set.Cells[0].ReadOnly&&set.Cells[0].InheritedStyle.BackColor==Presentation.SetColor,"Read-only checkbox must remain green.");
            Check(ready.DefaultCellStyle.BackColor==Presentation.ReadyColor,"Ready row must be yellow.");
            Check(ready.DefaultCellStyle.SelectionBackColor==Presentation.ReadyColor,"Selection must retain yellow status.");
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
        }
        using(var main=new MainForm())
        {
            main.LoadScan(scan,false);Check(main.Text=="Turbo Hole Table Callouts v0.3","Renamed macro title must include current version.");
            Check(main.Controls.OfType<ListBox>().Single().Items.Count==2,"Controller must list each native table.");
        }
        Console.WriteLine("PASS: "+checks+" table-window, title, sorting, selection and color checks.");return 0;
    }
}
