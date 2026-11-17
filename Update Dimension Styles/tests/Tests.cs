using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Hammill.StyleRepair;
public sealed class FakeStyle {public string Name;public int Format;}
public sealed class FakeStyles
{
    public List<FakeStyle> Items=new List<FakeStyle>();public string Active="Custom";public string Block;
    public int Count{get{return Items.Count;}}
    public FakeStyle Item(int i){return Items[i-1];}
    public void Remove(string name){if(name==Block)throw new COMException("In use");Items.RemoveAll(x=>x.Name==name);}
}
public sealed class FakeDoc
{
    public int Type=2,Imports;public FakeStyles DimensionStyles=new FakeStyles();public bool Ignore;public string Name="Test.dft";
    public void ImportStyles2(int kind,bool replace,object source)
    {
        if(kind!=0 || replace)throw new Exception("Unsafe import options");Imports++;
        if(Ignore)return;
        foreach(var style in ((FakeDoc)source).DimensionStyles.Items)
            if(!DimensionStyles.Items.Any(s=>s.Name==style.Name))DimensionStyles.Items.Add(new FakeStyle{Name=style.Name,Format=style.Format});
    }
}
internal static class Tests
{
    static int count;static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
    static void Reject(Action action,string name){try{action();}catch{Check(true,name);return;}throw new Exception(name);}
    static FakeDoc Doc(params string[] names){var d=new FakeDoc();d.DimensionStyles.Items.AddRange(names.Select(n=>new FakeStyle{Name=n,Format=42}));return d;}
    [STAThread]static int Main(){try{
        var required=new[]{"1 2 place","3 2 place (vert)"};var target=Doc("1 2 place","Custom");var master=Doc("1 2 place","3 2 place (vert)","Extra");
        target.DimensionStyles.Items[0].Format=99;
        Check(Repair.Missing(target,required).SequenceEqual(new[]{required[1]}),"detects missing vertical style");
        Check(Repair.AddMissing(target,master,required).Length==1,"adds missing cycle style");
        Check(target.DimensionStyles.Items[0].Format==99 && target.DimensionStyles.Active=="Custom","existing formatting and active style preserved");
        Check(Repair.Names(target).ContainsKey("Custom") && !Repair.Names(target).ContainsKey("Extra"),"keeps old custom styles and removes only newly imported extras");
        Check(Repair.AddMissing(target,master,required).Length==0 && target.Imports==1,"rerun is a no-op");
        target=Doc();Reject(()=>Repair.AddMissing(target,Doc("Other"),required),"incomplete master blocks import");Check(target.Imports==0,"master checked before mutations");
        target=Doc();target.Ignore=true;Reject(()=>Repair.AddMissing(target,master,required),"silent import failure detected");
        target=Doc();target.DimensionStyles.Block="Extra";Repair.AddMissing(target,master,required);Check(Repair.Retained.SequenceEqual(new[]{"Extra"}) && Repair.Missing(target,required).Length==0,"protected imported extras reported without losing cycle styles");
        Check(Repair.Missing(Doc("1 2 PLACE ","3 2 place (vert)"),required).Length==0,"matches cycle case and trailing-space rules");
        target=Doc("1 2 PLACE ");target.DimensionStyles.Items[0].Format=99;Repair.AddMissing(target,master,required);
        Check(Repair.Names(target).Count==2 && target.DimensionStyles.Items[0].Format==99,"new aliases with different case or spaces do not duplicate existing styles");
        Reject(()=>Repair.Names(Doc("Duplicate","duplicate ")),"ambiguous normalized styles rejected");
        target=Doc();target.Type=3;Reject(()=>Repair.AddMissing(target,master,required),"non-draft target rejected");
        string work=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,".work");Directory.CreateDirectory(work);string list=Path.Combine(work,"styles.txt");
        File.WriteAllText(list,"[Vertical Inch]\r\n3 2 place (vert)\r\n3 3 place (vert)\r\n");Check(Repair.Required(list).Length==2,"reads exact configured cycle names");
        File.WriteAllText(list,"[Vertical Inch]\r\nSame\r\nsame\r\n");Reject(()=>Repair.Required(list),"uses cycle parser to reject duplicates");
        File.WriteAllText(list,"[Vertical Inch]\r\n");Reject(()=>Repair.Required(list),"empty cycle list cannot report a successful check");
        Console.WriteLine(count+" checks passed.");return 0;
    }catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
