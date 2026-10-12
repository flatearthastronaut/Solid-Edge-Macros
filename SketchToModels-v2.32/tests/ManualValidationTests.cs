using System;using SketchToModels;
public class Collection {public int Count;public object Value;public object Item(int i){return Value;}}
public class Part {public Collection Sketches=new Collection{Count=1};public Collection Models=new Collection();}
public class Model {public Collection RevolvedProtrusions=new Collection();public Body Body=new Body();}
public class Body {public bool IsSolid=true;public double Volume=1;}
public class Feature {public int Status=1216476310;public double Angle=2*Math.PI;}
class Tests {static int n;static void Reject(Part p,string text){try{Engine.ValidateManualRevolve(p,1);throw new Exception("Expected error");}catch(InvalidOperationException e){if(!e.Message.Contains(text))throw;n++;}}
static void Main(){var p=new Part();p.Sketches.Count=2;Reject(p,"standalone sketch");p=new Part();Reject(p,"Finish the revolve");var m=new Model();p.Models.Count=1;p.Models.Value=m;Reject(p,"one completed");m.RevolvedProtrusions.Count=1;var f=new Feature{Status=0};m.RevolvedProtrusions.Value=f;Reject(p,"valid solid");f.Status=1216476310;f.Angle=Math.PI;Reject(p,"360 degrees");Console.WriteLine(n+" incomplete/manual-feature guard checks passed; no CAD geometry tested.");}}
