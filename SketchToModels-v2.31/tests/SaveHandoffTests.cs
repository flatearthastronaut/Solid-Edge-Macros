using System;using System.Linq;using System.Reflection;using System.Threading;using System.Windows.Forms;using SketchToModels;
public class C {public int Count {get;set;}}
public class M {public bool Read;public int Count {get{Read=true;return 0;}}}
public class A {public D ActiveDocument;}
public class D {public string FullName="test.par";public C ProfileSets=new C();public C Sketches=new C{Count=1};public M Models=new M();public A Application;public D(){Application=new A{ActiveDocument=this};}}
class Test {[STAThread]static void Main(){using(var gate=new ManualResetEvent(false)){var job=new ChainSelectionJob(cancel=>{gate.WaitOne();return !cancel();});var doc=new D();using(var f=new ProjectionPrompt(doc,new object(),1)){
typeof(ProjectionPrompt).GetField("selectionJob",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(f,job);
var b=f.Controls.OfType<Button>().Single(x=>x.Text=="Continue");typeof(Button).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,new object[]{EventArgs.Empty});
if(!doc.Models.Read)throw new Exception("Validation was blocked by the search");
if(!f.Controls.OfType<Label>().Any(x=>x.Text.Contains("Finish the revolve")))throw new Exception("Validation result missing");
if(job.IsCompleted)throw new Exception("Search should still be blocked");gate.Set();SpinWait.SpinUntil(()=>job.IsCompleted,2000);if(job.Result)throw new Exception("Search was not canceled");
Console.WriteLine("PASS: actual Check & save handler cancels a blocked search, immediately validates the document, and displays the validation result.");}}}}

