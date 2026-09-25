using System;using System.Reflection;using SketchToModels;
public class Items{public int Count=0;}
public class Construction{public Items RevolvedSurfaces=new Items();}
public class Parent{public int Type=-1521484909;}
public class Lines {public int Count;public object AddBy2Points(double a,double b,double c,double d){if(Math.Abs(a)>1e-8||Math.Abs(c)>1e-8||Math.Abs(b+.05)>1e-8||Math.Abs(d-.05)>1e-8)throw new Exception("Wrong axis endpoints");Count++;return new object();}}
public class Relations {public int Count;public void AddFix(object line){Count++;}}
public class Axis {public bool Wrong;public void StartPoint(ref Array a){a=new double[]{Wrong?1:0,0,0};}public void Direction(ref Array a){a=new double[]{0,0,1};}}
public class Profile {public int Type=1584866912;public Parent Parent=new Parent();public Lines Lines2d=new Lines();public Relations Relations2d=new Relations();public Axis Axis=new Axis();public int Calls;public object SetAxisOfRevolution(object line){Calls++;return Axis;}public void Convert3DCoordinate(double x,double y,double z,ref double u,ref double v){u=y;v=z;}public void Convert2DCoordinate(double u,double v,ref double x,ref double y,ref double z){x=0;y=u;z=v;}}

public class App{public int Calls,Last;public void StartCommand(int command){Calls++;Last=command;}}
public class Doc{public string FullName="test.par";public Construction Constructions=new Construction();public object ActiveSketch;public App Application=new App();}
class Test{[STAThread]static void Main(){var d=new Doc();using(var f=new BushingSurfacePrompt(d)){
var m=typeof(BushingSurfacePrompt).GetMethod("StartSurfaceProjection",BindingFlags.Instance|BindingFlags.NonPublic);
Func<bool> run=()=> (bool)m.Invoke(f,null);
if(run()||d.Application.Calls!=0)throw new Exception("Started outside sketch");
d.ActiveSketch=new Profile{Parent=new Parent{Type=123}};
if(run()||d.Application.Calls!=0)throw new Exception("Started in source sketch");
d.ActiveSketch=new Profile();
if(!run()||d.Application.Last!=50011||d.Application.Calls!=1)throw new Exception("Did not start projection");
if(((Profile)d.ActiveSketch).Calls!=1)throw new Exception("Axis not assigned");if(run()||d.Application.Calls!=1)throw new Exception("Restarted projection");
}Console.WriteLine("PASS: waits for feature profile, rejects source sketch, starts Project to Sketch once after assigning the Z axis.");}}

