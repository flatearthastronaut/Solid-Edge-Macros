using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Collections.Generic;
using System.Xml.Linq;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;

// Role-specific presentation extracted from the user's reference. Values,
// references, section direction, geometric construction and text stay intact.
internal static class ReferenceAnnotationStyle {
 static XElement standard;
 static readonly Dictionary<string,Type> types=new Dictionary<string,Type>();
 static Type Api(string name){
  if(!types.ContainsKey(name)){
   bool draft=name=="CuttingPlane";
   types[name]=Assembly.LoadFrom(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Siemens","Solid Edge 2026","Program","interop.SolidEdge"+(draft?"Draft":"FrameworkSupport")+"Lib.dll")).GetType("SolidEdge."+(draft?"Draft":"FrameworkSupport")+".Interop."+name);
  }return types[name];
 }
 static XElement Category(string name){
  if(standard==null){string folder=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","..","DraftingStandards"));standard=XDocument.Load(Path.Combine(folder,"annotation-presentation-standard.xml")).Root;}
  foreach(var c in standard.Elements("Category"))if((string)c.Attribute("name")==name)return c;
  throw new InvalidOperationException("Missing annotation role: "+name);
 }
 static bool Equal(object actual,object expected){
  if(actual is double)return Math.Abs((double)actual-(double)expected)<1e-12;
  return object.Equals(actual,expected);
 }
 static int Properties(object obj,Type type,XElement properties,bool verify){
  int count=0;if(properties==null)return count;
  foreach(var p in properties.Elements("Property")){
   string name=(string)p.Attribute("name"),raw=(string)p.Attribute("value");var info=type.GetProperty(name);
   object wanted=info.PropertyType.IsEnum?Enum.ToObject(info.PropertyType,int.Parse(raw,CultureInfo.InvariantCulture)):Convert.ChangeType(raw,info.PropertyType,CultureInfo.InvariantCulture);
   object current=info.GetValue(obj,null);
   if(!Equal(current,wanted)){
    if(verify)throw new Exception(type.Name+"."+name+" did not persist: "+current+" expected "+wanted);
    info.SetValue(obj,wanted,null);
    if(!Equal(info.GetValue(obj,null),wanted))throw new Exception(type.Name+"."+name+" readback failed");
   }
   count++;
  }return count;
 }
 internal static int Apply(string role,object obj,bool verify=false){
  var c=Category(role);var type=Api((string)c.Attribute("api"));int count=0;
  // Set text-format properties over the entire text without replacing content.
  foreach(string scope in new[]{"Style","Edit","Object"}){
   var settings=c.Element(scope);if(settings==null||!settings.HasElements)continue;
   object target=scope=="Object"?obj:type.GetProperty(scope).GetValue(obj,null);
   try{
    if(scope=="Edit"){
     var edit=(S.TextEdit)target;
     edit.SetSelect(0,0,S.TextSelectConstants.seTextSelectAll);
    }
    count+=Properties(target,scope=="Object"?type:Api(scope=="Style"?"DimStyle":"TextEdit"),settings,verify);
   }finally{if(scope!="Object")Marshal.ReleaseComObject(target);}
  }return count;
 }
 internal static int Document(D.DraftDocument draft,bool verify=false){
  int count=0;
  foreach(D.Sheet sheet in draft.Sheets){if(sheet.SectionType!=D.SheetSectionTypeConstants.igWorkingSection)continue;
   foreach(S.CenterLine c in (S.CenterLines)sheet.CenterLines)count+=Apply("CenterLine",c,verify);
   foreach(S.CenterMark c in (S.CenterMarks)sheet.CenterMarks)count+=Apply("CenterMark",c,verify);
   foreach(S.Balloon c in (S.Balloons)sheet.Balloons)count+=Apply("Balloon",c,verify);
   foreach(S.Leader c in (S.Leaders)sheet.Leaders)count+=Apply("Leader",c,verify);
   foreach(S.SurfaceFinishSymbol c in (S.SurfaceFinishSymbols)sheet.SurfaceFinishSymbols)count+=Apply("SurfaceFinishSymbol",c,verify);
   foreach(S.FeatureControlFrame c in (S.FeatureControlFrames)sheet.FeatureControlFrames){
    string text=c.PrimaryFrame??"";count+=Apply(text.StartsWith("+")||text.StartsWith("-")?"ToleranceNote":"FeatureControlFrame",c,verify);
   }
   foreach(S.TextBox text in (S.TextBoxes)sheet.TextBoxes){
    double x0,y0,x1,y1;text.Range(out x0,out y0,out x1,out y1);
    string value=text.Text??"";
    string role=x0>11*.0254&&y1<2.6*.0254?"TextField":value.StartsWith("SECTION ")||value.StartsWith("TOP VIEW")&&!value.Contains("\r")||value.StartsWith("ISOMETRIC")?"TextCaption":"TextNote";
    count+=Apply(role,text,verify);
    if(!verify){
     // Increase the existing fitting text boxes to accommodate the reference
     // font; do not shrink text or edit engineering content to make it fit.
     if(role=="TextNote")text.Width=10.4*.0254;
     if(role=="TextCaption")text.Width=(value.StartsWith("ISOMETRIC")?1.6:5)*.0254;
     if(role=="TextField"){
      text.Width=(value=="NONE / ZINC PLATE"?1.38:value=="A"?.2:value=="HF-02BSPT-04ORB"?1.35:2.5)*.0254;
      if(value=="HF-02BSPT-04ORB")text.SetOrigin(14.68*.0254,.72*.0254,0);
     }
    }
   }
   foreach(D.DrawingView view in sheet.DrawingViews)foreach(D.CuttingPlane cp in view.CuttingPlanes)count+=Apply("CuttingPlane",cp,verify);
   // Existing B-size title block is the same template as the reference. Match
   // its individual field styles, keeping its static labels black and fields teal.
   var background=sheet.Background;
   if(background!=null&&background.Name=="B-Size"){
    var balloons=(S.Balloons)background.Balloons;
    if(balloons.Count!=41)throw new Exception("B-size title block differs from reference; review field mapping");
    for(int i=1;i<=balloons.Count;i++)count+=Apply("BackgroundBalloon"+i,balloons.Item(i),verify);
    foreach(S.SurfaceFinishSymbol symbol in (S.SurfaceFinishSymbols)background.SurfaceFinishSymbols)count+=Apply("BackgroundSurfaceFinishSymbol",symbol,verify);
   }
  }return count;
 }
}
