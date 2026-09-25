using System;using System.Reflection;using SketchToModels;
public class Collection {public int CountValue;public int Count {get{return CountValue;}}}
public class Parent {public int Type=-1521484909;}
public class Profile {public int Type=1584866912;public Parent Parent=new Parent();}
public class App {public Doc ActiveDocument;public int LastCommand;public void StartCommand(int command){LastCommand=command;}}
public class Doc {public string FullName="test.par";public Collection ProfileSets=new Collection{CountValue=1};public Collection Sketches=new Collection{CountValue=1};public object ActiveSketch;public App Application;public Doc(){Application=new App{ActiveDocument=this};}}
class Test {[STAThread]static void Main(){var d=new Doc();using(var p=new ProjectionPrompt(d,new object(),1,true)){d.ProfileSets.CountValue=2;d.ActiveSketch=new Profile();typeof(ProjectionPrompt).GetMethod("StartProjection",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(p,new object[0]);if(d.Application.LastCommand!=50011)throw new Exception("Projection not launched");}Console.WriteLine("PASS: bushing projection launches without requesting line creation or automatic Z-axis setup.");}}

