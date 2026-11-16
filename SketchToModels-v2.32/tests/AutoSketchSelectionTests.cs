using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using SketchToModels;

public class Sketch {public int Type=1689979564;public object Profile=new object();}
public class Sketches {public Sketch[] Values={new Sketch(),new Sketch()};public int Count{get{return Values.Length;}}public Sketch Item(int i){return Values[i-1];}}
public class Selection {public int Clears;public void RemoveAll(){Clears++;}public void Add(object value){throw new Exception("Preselection must not be used.");}}
public class CountCollection {public int Count;}
public class Constructions {public CountCollection RevolvedSurfaces=new CountCollection();}
public class Parent {public int Type=-1521484909;}
public class Lines {public int Count;public object AddBy2Points(double a,double b,double c,double d){Count++;return new object();}}
public class Relations {public void AddFix(object line){}}
public class Axis {public void StartPoint(ref Array a){a=new double[]{0,0,0};}public void Direction(ref Array a){a=new double[]{0,0,1};}}
public class Profile
{
    public int Type=1584866912;public Parent Parent=new Parent();public Lines Lines2d=new Lines();public Relations Relations2d=new Relations();
    public object SetAxisOfRevolution(object line){return new Axis();}
    public void Convert3DCoordinate(double x,double y,double z,ref double u,ref double v){u=y;v=z;}
    public void Convert2DCoordinate(double u,double v,ref double x,ref double y,ref double z){x=0;y=u;z=v;}
}
public class App {public Doc ActiveDocument;public int LastCommand=45000;public Viewport ActiveWindow;public int ProcessID{get{return System.Diagnostics.Process.GetCurrentProcess().Id;}}public void Activate(){}public void StartCommand(int command){LastCommand=command;}public int GetActiveCommand(){return LastCommand;}}
public class Viewport {public long DrawHwnd;public View View=new View();}
public class View
{
    public void TransformModelToDC(double x,double y,double z,ref int px,ref int py)
    {if(x!=.02 || y!=.03 || z!=.04)throw new Exception("Wrong model point.");px=80;py=90;}
}
public class Geometry {public bool Construction;}
public class GeometryCollection
{
    public Geometry[] Values=new Geometry[0];public int Count{get{return Values.Length;}}public Geometry Item(int i){return Values[i-1];}
}
public class Curve
{
    public void GetParamExtents(ref double lo,ref double hi){lo=0;hi=1;}
    public void GetPointAtParam(int count,ref Array parameter,ref Array points)
    {if(count!=1 || (double)parameter.GetValue(0)!=.5)throw new Exception("Expected an interior curve point.");points=new double[]{.02,.03,.04};}
}
public class CurveBody {public CountCollection Curves=new CountCollection{Count=2};}
public class SourceProfile
{
    public CurveBody CurveBody=new CurveBody();
    public GeometryCollection Lines2d=new GeometryCollection{Values=new[]{new Geometry{Construction=true},new Geometry()}};
    public GeometryCollection Arcs2d=new GeometryCollection(),Circles2d=new GeometryCollection(),Ellipses2d=new GeometryCollection(),EllipticalArcs2d=new GeometryCollection(),BSplineCurves2d=new GeometryCollection();
    public bool IsConstructionElement(object value){return ((Geometry)value).Construction;}
    public void MapGeometryToCurves(object value,ref int count,ref object mapped)
    {if(((Geometry)value).Construction)throw new Exception("Construction geometry was mapped.");count=1;mapped=new object[]{new Curve()};}
}
public class Doc
{
    public string FullName="graphical-pick-test.par";public Sketches Sketches=new Sketches();public Selection SelectSet=new Selection();
    public CountCollection ProfileSets=new CountCollection{Count=1};public Constructions Constructions=new Constructions();public object ActiveSketch=new Profile();public App Application;
    public Doc(){Application=new App{ActiveDocument=this};}
}
public class Picker : IOutlinePicker
{
    public int Calls;public object Source;public bool Fail;
    public void Pick(object document,object sourceProfile){Calls++;Source=sourceProfile;if(Fail)throw new InvalidOperationException("Covered viewport");}
}
class AutoSketchSelectionTests
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static object Call(object target,string name){return target.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null);}
    static void Set(object target,string name,object value){target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);}
    [STAThread] static void Main()
    {
        var doc=new Doc();var picker=new Picker();var auto=new AutoSketchSelection(2,true,picker);
        auto.Prepare(doc,2);Check(picker.Calls==0 && doc.SelectSet.Clears==0,"Launch must not preselect or click.");
        doc.Application.LastCommand=50011;auto.PickAfterChain(doc,2);auto.PickAfterChain(doc,2);
        Check(picker.Calls==1 && picker.Source==doc.Sketches.Item(2).Profile,"Wrong source or repeated pick.");
        Check(auto.Message.Contains("Check the highlighted preview"),"Click must not claim completed projection.");
        auto.UseManual(doc);auto.PickAfterChain(doc,2);Check(picker.Calls==1 && !auto.Enabled,"Manual handoff retried click.");
        picker=new Picker();auto=new AutoSketchSelection(2,false,picker);auto.PickAfterChain(doc,2);Check(picker.Calls==0,"Manual default clicked.");
        foreach(int badIndex in new[]{0,3}){picker=new Picker();auto=new AutoSketchSelection(badIndex,true,picker);auto.PickAfterChain(doc,2);Check(!auto.Enabled && picker.Calls==0,"Invalid index clicked.");}
        picker=new Picker();auto=new AutoSketchSelection(2,true,picker);auto.PickAfterChain(doc,3);Check(picker.Calls==0,"Changed sketch collection clicked.");
        doc.Application.LastCommand=45000;picker=new Picker();auto=new AutoSketchSelection(2,true,picker);auto.PickAfterChain(doc,2);Check(picker.Calls==0,"Wrong native command clicked.");
        doc.Application.LastCommand=50011;doc.ActiveSketch=new Profile{Parent=new Parent{Type=123}};picker=new Picker();auto=new AutoSketchSelection(2,true,picker);auto.PickAfterChain(doc,2);Check(picker.Calls==0,"Source sketch editing was accepted.");
        doc.ActiveSketch=new Profile();picker=new Picker{Fail=true};auto=new AutoSketchSelection(2,true,picker);auto.PickAfterChain(doc,2);auto.PickAfterChain(doc,2);Check(!auto.Enabled && picker.Calls==1 && auto.Message.Contains("Covered viewport"),"Failed click retried or lost manual fallback.");

        // Exercise each actual timer handoff. The dropdown worker has completed
        // successfully before the injected picker is allowed to see the source.
        doc=new Doc();picker=new Picker();
        using(var form=new ProjectionPrompt(doc,new object(),2,true,2,true)) {
            Set(form,"autoSelection",new AutoSketchSelection(2,true,picker));doc.ProfileSets.Count=2;Call(form,"StartProjection");
            Check(picker.Calls==0 && doc.Application.LastCommand==50011,"Solid prompt picked before command/chain.");
            var job=new ChainSelectionJob(stop=>true);SpinWait.SpinUntil(()=>job.IsCompleted,2000);Set(form,"selectionJob",job);
            var timer=typeof(ProjectionPrompt).GetField("timer",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
            typeof(System.Windows.Forms.Timer).GetMethod("OnTick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(timer,new object[]{EventArgs.Empty});
            Check(picker.Calls==1 && picker.Source==doc.Sketches.Item(2).Profile,"Solid did not pick after Wireframe Chain.");
            Check(((Profile)doc.ActiveSketch).Lines2d.Count==0,"User-axis path created an axis.");
        }
        doc=new Doc();picker=new Picker();
        using(var form=new BushingSurfacePrompt(doc,2,true)) {
            Set(form,"autoSelection",new AutoSketchSelection(2,true,picker));Call(form,"StartSurfaceProjection");
            var job=new ChainSelectionJob(stop=>true);SpinWait.SpinUntil(()=>job.IsCompleted,2000);Set(form,"projectionJob",job);Call(form,"AssistProjection");
            Check(picker.Calls==1 && ((Profile)doc.ActiveSketch).Lines2d.Count==1,"Surface pick/axis order failed.");
        }
        using(var form=new MainWindow()) {
            var check=(CheckBox)typeof(MainWindow).GetField("autoSelectSketch",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
            Check(!check.Checked,"Automatic mode must default off.");
        }
        // Real Windows messages, restricted to this test's own visible panel.
        // This checks routing and guards; it does not claim CAD command success.
        using(var window=new Form{Width=360,Height=260,TopMost=true,StartPosition=FormStartPosition.Manual,Location=new System.Drawing.Point(80,80)}) {
            var panel=new Panel{Dock=DockStyle.Fill};window.Controls.Add(panel);
            int downs=0,ups=0;panel.MouseDown+=(s,e)=>{if(e.X==80 && e.Y==90)downs++;};panel.MouseUp+=(s,e)=>{if(e.X==80 && e.Y==90)ups++;};
            window.Show();window.Activate();Application.DoEvents();uint pid=(uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            Check(!NativeOutlinePicker.CanClick(panel.Handle,pid+1,80,90),"Wrong process accepted.");
            Check(!NativeOutlinePicker.CanClick(panel.Handle,pid,-1,90),"Offscreen point accepted.");
            NativeOutlinePicker.Click(panel.Handle,pid,80,90);Check(downs==1 && ups==1,"Native click was not delivered once at its target.");
            int picked=0;panel.MouseDown+=(s,e)=>picked++;
            doc=new Doc();doc.Application.LastCommand=50011;doc.Application.ActiveWindow=new Viewport{DrawHwnd=panel.Handle.ToInt64()};
            new NativeOutlinePicker().Pick(doc,new SourceProfile());
            Check(picked==1,"Curve-to-view mapping and native picker did not deliver a click.");
            window.Hide();Check(!NativeOutlinePicker.CanClick(panel.Handle,pid,80,90),"Hidden viewport accepted.");
        }
        Console.WriteLine("PASS: post-chain graphical pick, no preselection/retries, exact source, manual fallback, prompt timing, retained axes and native viewport click guards. Live CAD selection still requires operator validation.");
    }
}
