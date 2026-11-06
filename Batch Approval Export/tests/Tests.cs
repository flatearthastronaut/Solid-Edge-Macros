using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Hammill.BatchApprovalExport;
public sealed class Items{public object[] Values;public Items(params object[] values){Values=values;}public int Count{get{return Values.Length;}}public object Item(int i){return Values[i-1];}}
public sealed class Link{public object ModelDocument;}
public sealed class Doc{public int Type;public string FullName;public Items ModelLinks=new Items();public void Activate(){}}
internal sealed class Exporter:IExporter
{
    public int Copies,Opens,Exports,Closes;public bool Empty,FailExport,FailClose;public string RaceDestination;public readonly List<bool> PdfFlags=new List<bool>();
    public void SaveCopy(object source,string path){Copies++;File.WriteAllText(path,"native snapshot");}
    public object Open(string path){Opens++;return path;}
    public void Export(object doc,string path,bool pdf){Exports++;PdfFlags.Add(pdf);if(FailExport)throw new IOException("Translator failure");File.WriteAllText(path,Empty?"":"translated output");if(RaceDestination!=null)File.WriteAllText(RaceDestination,"other process");}
    public void Close(object doc){Closes++;if(FailClose)throw new IOException("Close failed");}
}
internal static class Tests
{
    static int n;static void Check(bool ok,string name){if(!ok)throw new Exception(name);n++;Console.WriteLine("PASS "+name);}
    static void Reject(Action a,string name){try{a();}catch{Check(true,name);return;}throw new Exception(name);}
    [STAThread]static int Main(){try{
        Application.EnableVisualStyles();string root=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..",".work"));Directory.CreateDirectory(root);string work=Path.Combine(root,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);
        var draft=new Doc{Type=2,FullName=Path.Combine(work,"SameName.dft")};var assembly=new Doc{Type=3,FullName=Path.Combine(work,"SameName.asm")};draft.ModelLinks=new Items(new Link{ModelDocument=assembly});
        var date=new DateTime(2026,11,6);var jobs=ExportEngine.Plan(draft,draft.FullName,assembly,assembly.FullName,date,null,true,true,true,true);
        Check(jobs.Count==4 && jobs.Select(x=>x.Destination).Distinct(StringComparer.OrdinalIgnoreCase).Count()==4,"four exports never collide for matching draft and assembly names");
        Check(Path.GetFileName(jobs[3].Destination)=="SameName 20261106 3D.pdf" && jobs[3].ThreeDimensionalPdf && !jobs[1].ThreeDimensionalPdf,"date suffix and 3D PDF flag preserved");
        Check(ExportEngine.Plan(draft,draft.FullName,null,null,date,null,true,true,false,false).Count==2,"2D exports work without linked assembly");
        Reject(()=>ExportEngine.Plan(draft,draft.FullName,null,null,date,null,false,false,true,false),"3D export requires saved assembly");
        Reject(()=>ExportEngine.Plan(draft,"Untitled",null,null,date,null,true,false,false,false),"unsaved draft rejected");
        Reject(()=>ExportEngine.Plan(draft,draft.FullName,null,null,date,null,false,false,false,false),"empty selection rejected");
        var exporter=new Exporter();var result=ExportEngine.Run(jobs,exporter,s=>{});
        Check(result.All(s=>s.StartsWith("Created")) && jobs.All(j=>File.Exists(j.Destination)),"all staged outputs installed after validation");
        Check(exporter.Copies==4 && exporter.Opens==4 && exporter.Closes==4 && exporter.PdfFlags.SequenceEqual(new[]{false,false,false,true}),"each translator uses and closes a separate copy with correct PDF mode");
        Check(Directory.GetDirectories(work,".hammill-export-*").Length==0,"successful snapshots cleaned up");
        result=ExportEngine.Run(jobs,exporter,s=>{});Check(result.All(s=>s.StartsWith("Skipped")) && exporter.Copies==4,"existing files skipped before opening CAD snapshots");
        var failure=ExportEngine.Plan(draft,draft.FullName,null,null,date.AddDays(1),null,true,false,false,false);
        exporter=new Exporter{FailExport=true};result=ExportEngine.Run(failure,exporter,s=>{});Check(result.Any(s=>s.Contains("Translator failure")) && exporter.Closes==1 && !File.Exists(failure[0].Destination),"failed export closes snapshot and leaves no output");
        Check(Directory.GetDirectories(work,".hammill-export-*").Length==0,"failure workspace cleaned after copy closes");
        exporter=new Exporter{Empty=true};result=ExportEngine.Run(failure,exporter,s=>{});Check(result.Any(s=>s.Contains("nonempty")) && !File.Exists(failure[0].Destination),"empty translator output never published");
        exporter=new Exporter{RaceDestination=failure[0].Destination};result=ExportEngine.Run(failure,exporter,s=>{});Check(File.ReadAllText(failure[0].Destination)=="other process" && result.Any(s=>s.StartsWith("Failed")),"destination race never overwrites another file");
        var closeFailure=ExportEngine.Plan(draft,draft.FullName,null,null,date.AddDays(2),null,true,false,false,false);exporter=new Exporter{FailClose=true};result=ExportEngine.Run(closeFailure,exporter,s=>{});
        Check(result.Any(s=>s.Contains("retained")) && Directory.GetDirectories(work,".hammill-export-*").Length==1 && !File.Exists(closeFailure[0].Destination),"unclosed CAD copy retained and reported without publishing output");
        using(var form=new ExportForm(new object(),draft)){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);form.Show();Application.DoEvents();Check(form.Controls[0].Controls.OfType<ComboBox>().Single().Items.Count==1 && form.Controls[0].Controls.OfType<TextBox>().Any(x=>x.Text.Contains(".dwg")),"shown window resolves assembly links and previews output paths");using(var bmp=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bmp,new Rectangle(Point.Empty,form.Size));bmp.Save(Path.Combine(root,"preview.png"));}form.Hide();}
        Check(true,"export window renders planned paths");Console.WriteLine(n+" checks passed.");return 0;
    }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
