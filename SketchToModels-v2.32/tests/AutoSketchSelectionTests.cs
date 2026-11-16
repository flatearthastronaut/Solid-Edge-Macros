using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using SketchToModels;

// Simulate command preselection, not CAD projection. These tests deliberately
// make no claim that Solid Edge consumes SelectSet in Project to Sketch; that
// is the live acceptance test for this opt-in trial.
public class Sketch { public int Type=1689979564; }
public class Sketches
{
    public List<Sketch> Values=new List<Sketch>{new Sketch(),new Sketch()};
    public int Count {get{return Values.Count;}}
    public Sketch Item(int i){return Values[i-1];}
}
public class Selection
{
    public object Value;
    public int Adds,Clears;
    public bool Fail,Ignore;
    public int Count {get{return Value==null?0:1;}}
    public void RemoveAll(){Clears++;Value=null;}
    public void Add(object value){Adds++;if(Fail)throw new InvalidOperationException("Selection rejected");if(!Ignore)Value=value;}
}
public class CountCollection { public int Count; }
public class Constructions { public CountCollection RevolvedSurfaces=new CountCollection(); }
public class Parent { public int Type=-1521484909; }
public class Lines
{
    public int Count;
    public object AddBy2Points(double a,double b,double c,double d){Count++;return new object();}
}
public class Relations { public void AddFix(object line){} }
public class Axis
{
    public void StartPoint(ref Array a){a=new double[]{0,0,0};}
    public void Direction(ref Array a){a=new double[]{0,0,1};}
}
public class Profile
{
    public int Type=1584866912;
    public Parent Parent=new Parent();
    public Lines Lines2d=new Lines();
    public Relations Relations2d=new Relations();
    public object SetAxisOfRevolution(object line){return new Axis();}
    public void Convert3DCoordinate(double x,double y,double z,ref double u,ref double v){u=y;v=z;}
    public void Convert2DCoordinate(double u,double v,ref double x,ref double y,ref double z){x=0;y=u;z=v;}
}
public class App
{
    public Doc ActiveDocument;
    public object CommandSource;
    public int LastCommand;
    public void StartCommand(int command){LastCommand=command;CommandSource=ActiveDocument.SelectSet.Value;}
}
public class Doc
{
    public string FullName="preselection-test.par";
    public Sketches Sketches=new Sketches();
    public Selection SelectSet=new Selection();
    public CountCollection ProfileSets=new CountCollection{Count=1};
    public Constructions Constructions=new Constructions();
    public object ActiveSketch;
    public App Application;
    public Doc(){Application=new App{ActiveDocument=this};}
}
class AutoSketchSelectionTests
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static object Call(object form,string method)
    {return form.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(form,null);}
    [STAThread] static void Main()
    {
        var doc=new Doc();var selected=new object();doc.SelectSet.Value=selected;
        var manual=new AutoSketchSelection(2,false);manual.Prepare(doc,2);
        Check(doc.SelectSet.Value==selected && doc.SelectSet.Adds==0,"Manual mode touched selection.");
        var auto=new AutoSketchSelection(2,true);auto.Prepare(doc,2);auto.Prepare(doc,2);
        Check(doc.SelectSet.Value==doc.Sketches.Item(2) && doc.SelectSet.Adds==1,"Wrong source or repeated selection.");
        Check(auto.Message.Contains("requested"),"Selection must not be called a completed projection.");
        auto.UseManual(doc);auto.Prepare(doc,2);
        Check(doc.SelectSet.Count==0 && doc.SelectSet.Adds==1 && !auto.Enabled,"Manual handoff reselected source.");

        foreach(int badIndex in new[]{0,3}) {
            doc=new Doc();doc.SelectSet.Value=selected;
            auto=new AutoSketchSelection(badIndex,true);auto.Prepare(doc,2);
            Check(!auto.Enabled && doc.SelectSet.Value==selected,"Invalid index altered existing selection.");
        }
        doc=new Doc();auto=new AutoSketchSelection(2,true);auto.Prepare(doc,3);
        Check(!auto.Enabled && doc.SelectSet.Adds==0,"Changed source collection accepted.");
        doc=new Doc();doc.Sketches.Item(2).Type=1584866912;
        auto=new AutoSketchSelection(2,true);auto.Prepare(doc,2);
        Check(!auto.Enabled && doc.SelectSet.Adds==0,"Destination profile accepted as source.");
        foreach(bool throws in new[]{true,false}) {
            doc=new Doc();doc.SelectSet.Fail=throws;doc.SelectSet.Ignore=!throws;
            auto=new AutoSketchSelection(2,true);auto.Prepare(doc,2);auto.Prepare(doc,2);
            Check(!auto.Enabled && doc.SelectSet.Count==0 && doc.SelectSet.Adds==1,"Rejected selection did not fall back once.");
        }

        // Verify command ordering in the actual prompt: original copied sketch
        // is preselected BEFORE Project to Sketch starts, with no new geometry.
        doc=new Doc();
        using(var form=new ProjectionPrompt(doc,new object(),2,true,2,true)) {
            doc.ProfileSets.Count=2;doc.ActiveSketch=new Profile();Call(form,"StartProjection");
            Check(doc.Application.LastCommand==50011 && doc.Application.CommandSource==doc.Sketches.Item(2),"Solid prompt did not preselect before command.");
            Check(((Profile)doc.ActiveSketch).Lines2d.Count==0,"User-axis path created an axis.");
            var button=form.Controls.OfType<Button>().Single(b=>b.Text=="Select manually");
            typeof(Button).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(button,new object[]{EventArgs.Empty});
            Check(doc.Application.CommandSource==null && doc.SelectSet.Adds==1,"Manual button restarted automatic selection.");
        }
        doc=new Doc();
        using(var form=new BushingSurfacePrompt(doc,2,true)) {
            doc.ActiveSketch=new Profile();Check((bool)Call(form,"StartSurfaceProjection"),"Surface projection not started.");
            Check(doc.Application.LastCommand==50011 && doc.Application.CommandSource==doc.Sketches.Item(2),"Surface prompt did not preselect source.");
            Check(((Profile)doc.ActiveSketch).Lines2d.Count==1,"Surface axis setup lost.");
            Check(!(bool)Call(form,"StartSurfaceProjection") && doc.SelectSet.Adds==1,"Surface timer repeated selection.");
        }
        using(var form=new MainWindow()) {
            var check=(CheckBox)typeof(MainWindow).GetField("autoSelectSketch",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
            Check(!check.Checked,"Automatic mode must default off.");
        }
        Console.WriteLine("PASS: opt-in default, exact copied source, pre-command selection, manual fallback, guards, no retries, surface/solid wiring and retained axis behavior. Live projection consumption not tested.");
    }
}
