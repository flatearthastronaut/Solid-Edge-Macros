// Regression checks use fake CAD objects; they verify rules without modifying Solid Edge.
using System;using System.Collections.Generic;using SketchToModels;
public class Style{public string StyleName;public float R,G,B,Opacity,TextureWeight;public void SetDiffuse(float r,float g,float b){R=r;G=g;B=b;}public void SetAmbient(float r,float g,float b){}}
public class Styles{public List<Style> Values=new List<Style>();public int Added;public int Count{get{return Values.Count;}}public Style Item(int i){return Values[i-1];}public Style Add(string name,string parent){Added++;var s=new Style{StyleName=name};Values.Add(s);return s;}}
public class Body{public object Style;}
public class Feature{public string Name="Protrusion 6",EdgebarName="Protrusion 6";public object Style;public void SetStyle(object style){Style=style;}}
public class Features{public Feature F=new Feature();public int Count=1;public Feature Item(int i){return F;}}
public class Model{public Body Body=new Body();public Features RevolvedProtrusions=new Features();}
public class Models{public Model M=new Model();public int Count=1;public Model Item(int i){return M;}}
public class Doc{public Styles FaceStyles=new Styles();public Models Models=new Models();public Features DesignEdgebarFeatures=new Features();public object Applied;public void SetBaseStyle(object kind,object style){Applied=style;}}
class Test{static void Main(){
string[] names={"COLLET1","part stop","PartStop","part-stop","ADAPTER","puller 21930","body","bushing","Diaphragms","INSERT","inserts","insert 21930"};
string[] colors={"Yellow","Green","Green","Green","Khaki","Orange","Bronze",null,"Yellow","Blue","Blue","Blue"};
for(int i=0;i<names.Length;i++)if(PartColors.StyleForName(names[i])!=colors[i])throw new Exception(names[i]);
var d=new Doc();var existing=new Style{StyleName="yellow",R=.75f};d.FaceStyles.Values.Add(existing);
PartColors.Apply(d,"collet1");
if(d.FaceStyles.Added!=0||d.Applied!=existing||d.Models.M.Body.Style!=existing||d.Models.M.RevolvedProtrusions.F.Style!=existing||existing.R!=.75f)throw new Exception("Reuse/application failure");
d=new Doc();d.FaceStyles.Values.Add(new Style{StyleName="Default"});
PartColors.Apply(d,"adapter");
if(d.FaceStyles.Added!=0||d.Applied!=null||d.Models.M.Body.Style!=null)throw new Exception("Missing style must be skipped");
d=new Doc();PartColors.Apply(d,"bushing");if(d.Applied!=null||d.FaceStyles.Added!=0)throw new Exception("Unmatched changed");
// All existing name rules, including unmatched names, must yield to the
// workflow override. A mixed-case existing Black style must be reused intact.
foreach(string name in names) {
    d=new Doc(); var black=new Style{StyleName="bLaCk",Opacity=.8f};
    d.FaceStyles.Values.Add(new Style{StyleName="Yellow"}); d.FaceStyles.Values.Add(black);
    PartColors.Apply(d,name,true);
    if(d.Applied!=black || d.Models.M.Body.Style!=black || d.Models.M.RevolvedProtrusions.F.Style!=black || d.DesignEdgebarFeatures.F.Style!=black || d.FaceStyles.Added!=0 || black.Opacity!=.8f)
        throw new Exception("O-ring Black override failed: "+name);
}
// Missing Black must skip entirely, even when the conflicting name's color
// exists. Preserve an already assigned template appearance, without fallback.
d=new Doc(); var original=new Style{StyleName="Yellow"}; d.FaceStyles.Values.Add(original);
d.Applied=original; d.Models.M.Body.Style=original; d.DesignEdgebarFeatures.F.Style=original;
PartColors.Apply(d,"oring-collet",true);
if(d.Applied!=original || d.Models.M.Body.Style!=original || d.DesignEdgebarFeatures.F.Style!=original || d.FaceStyles.Added!=0)
    throw new Exception("Missing Black must preserve existing appearance.");
Console.WriteLine("PASS: existing name rules and style preservation; O-ring Black overrides every name, paints the template feature, and skips missing Black without fallback.");
}}
