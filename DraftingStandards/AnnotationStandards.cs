using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Collections;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;

partial class DimensionStandardTool {
 static string AnnotationMeaning(D.DraftDocument draft){
  var s=new System.Text.StringBuilder();
  foreach(D.Sheet sheet in draft.Sheets){if(sheet.SectionType!=D.SheetSectionTypeConstants.igWorkingSection)continue;
   foreach(S.Dimension dim in (S.Dimensions)sheet.Dimensions)s.AppendLine("DIM "+dim.DimensionType+" "+Value(dim.Value)+" "+dim.PrimaryUpperTolerance+" "+dim.PrimaryLowerTolerance+" "+dim.Style.PrimaryUnits+" "+dim.Style.PrimaryDecimalRoundOff);
   foreach(S.Balloon b in (S.Balloons)sheet.Balloons)s.AppendLine("CALLOUT "+b.BalloonText+" attached="+b.IsTerminatorAttachedToEntity);
   foreach(S.TextBox t in (S.TextBoxes)sheet.TextBoxes)s.AppendLine("TEXT "+t.Text);
   foreach(D.DrawingView view in sheet.DrawingViews){double x,y;view.GetOrigin(out x,out y);s.AppendLine("VIEW "+view.Name+" "+x.ToString("F9")+" "+y.ToString("F9"));
    foreach(D.CuttingPlane cp in view.CuttingPlanes){s.AppendLine("CUT "+cp.Caption+" "+cp.ViewSide);var p=cp.Profile;
     foreach(S.Line2d line in p.Lines2d){double a,b,c,d;line.GetStartPoint(out a,out b);line.GetEndPoint(out c,out d);int kind=AxisConstraints.Direction(a,b,c,d);if(kind!=0&&!AxisConstraints.Has(line,kind))throw new Exception("Cutting-plane constraint missing");s.AppendLine("LINE "+a.ToString("F9")+" "+b.ToString("F9")+" "+c.ToString("F9")+" "+d.ToString("F9"));}
    }
   }
  }return s.ToString();
 }
 static void ApplyAnnotations(){
  var report=new System.Text.StringBuilder();
  foreach(string name in new[]{"BSPT-02 to SAE-04 ORB Adapter.dft","BSPT-02 to SAE-04 ORB Adapter - Plane Sections.dft"}){
   string path=Path.GetFullPath(Path.Combine(root,"..","HydraulicFitting","Models",name));
   var draft=(D.DraftDocument)app.Documents.Open(path);string before=AnnotationMeaning(draft);
   int properties=ReferenceAnnotationStyle.Document(draft);
   draft.UpdatePropertyTextCacheAndDisplay();
   if(before!=AnnotationMeaning(draft))throw new Exception("Annotation meaning or view geometry changed");
   foreach(S.Balloon b in (S.Balloons)draft.ActiveSheet.Balloons){if(b.BalloonText.Contains("%ZT")&&(!b.BalloonDisplayedText.Contains(".460 DP.")||System.Text.RegularExpressions.Regex.Matches(b.BalloonDisplayedText,"[.]460").Count!=1))throw new Exception("Thread depth units or repetition changed");}
   draft.Save();draft.Close(false);Marshal.ReleaseComObject(draft);
   draft=(D.DraftDocument)app.Documents.Open(path);
   ReferenceAnnotationStyle.Document(draft,true);
   if(before!=AnnotationMeaning(draft))throw new Exception("Saved annotation meaning or view geometry changed");
   draft.ActiveSheet.Activate();app.DoIdle();var window=(D.SheetWindow)app.ActiveWindow;window.FitEx(D.SheetFitConstants.igFitSheet);
   window.SaveAsImage(Path.Combine(Path.GetDirectoryName(path),name.Contains("Plane Sections")?"Adapter-plane-sections.jpg":"Adapter-drawing.jpg"),3060,1980,1,24,SolidEdge.Framework.Interop.SeImageQualityType.seImageQualityHigh,false);
   Marshal.ReleaseComObject(window);Marshal.ReleaseComObject(draft);
   report.AppendLine(name+": PASS "+properties+" presentation properties after save/reopen; content, dimensions, tolerances, attachment states, view positions and constrained cut geometry unchanged.");
  }
  File.WriteAllText(Path.Combine(root,"annotation-verification.txt"),report.ToString());Console.WriteLine(report);
 }
 static readonly string[] annotationGroups={"CenterLines","CenterMarks","Balloons","Leaders","TextBoxes","SurfaceFinishSymbols","FeatureControlFrames"};
 static readonly string[] annotationTypes={"CenterLine","CenterMark","Balloon","Leader","TextBox","SurfaceFinishSymbol","FeatureControlFrame"};
 static Type ApiType(string name,bool draft=false){return Assembly.LoadFrom(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Siemens","Solid Edge 2026","Program","interop.SolidEdge"+(draft?"Draft":"FrameworkSupport")+"Lib.dll")).GetType("SolidEdge."+(draft?"Draft":"FrameworkSupport")+".Interop."+name);}
 static XElement ScalarSnapshot(object obj,Type type,string name){
  var result=new XElement(name);
  foreach(var p in type.GetProperties()){
   if(!p.CanRead||p.GetIndexParameters().Length!=0||!(p.PropertyType.IsPrimitive||p.PropertyType.IsEnum||p.PropertyType==typeof(string)))continue;
   try{object value=p.GetValue(obj,null);if(value!=null)result.Add(new XElement("Property",new XAttribute("name",p.Name),new XAttribute("value",Value(value)),new XAttribute("type",p.PropertyType.FullName),new XAttribute("writable",p.CanWrite)));}catch{}
  }
  return result;
 }
 static XElement AnnotationSnapshot(object obj,string kind){
  var type=ApiType(kind,kind=="CuttingPlane");var entry=ScalarSnapshot(obj,type,kind);
  foreach(string property in new[]{"Style","Edit"}){
   var p=type.GetProperty(property);if(p==null)continue;object sub=p.GetValue(obj,null);
   try{entry.Add(ScalarSnapshot(sub,ApiType(property=="Style"?"DimStyle":"TextEdit"),property));}finally{Marshal.ReleaseComObject(sub);}
  }
  return entry;
 }
 static void AuditAnnotations(){
  var result=new XElement("AnnotationAudit");
  foreach(string name in new[]{"Reference","Fitting","PlaneSections"}){
   string path=name=="Reference"?Path.Combine(root,"detailing example 1.dft"):Path.GetFullPath(Path.Combine(root,"..","HydraulicFitting","Models",name=="Fitting"?"BSPT-02 to SAE-04 ORB Adapter.dft":"BSPT-02 to SAE-04 ORB Adapter - Plane Sections.dft"));
   bool open=false;for(int i=1;i<=app.Documents.Count;i++)if(string.Equals((string)app.Documents.Item(i).FullName,path,StringComparison.OrdinalIgnoreCase))open=true;
   var draft=(D.DraftDocument)app.Documents.Open(path);var document=new XElement(name);result.Add(document);
   try{foreach(D.Sheet sheet in draft.Sheets){
    if(sheet.SectionType!=D.SheetSectionTypeConstants.igWorkingSection&&sheet.Name!="B-Size"&&!(name=="Reference"&&(sheet.Name=="Notes To Copy"||sheet.Name=="EXPANSION FORM")))continue;
    var sh=new XElement("Sheet",new XAttribute("name",sheet.Name),new XAttribute("section",sheet.SectionType));document.Add(sh);
    for(int i=0;i<annotationGroups.Length;i++){
     object group=ApiType("Sheet",true).GetProperty(annotationGroups[i]).GetValue(sheet,null);int count=0;
     try{foreach(object obj in (IEnumerable)group){try{sh.Add(AnnotationSnapshot(obj,annotationTypes[i]));count++;}finally{Marshal.ReleaseComObject(obj);}}}finally{Marshal.ReleaseComObject(group);}
     if(count>0)Console.WriteLine(name+" "+sheet.Name+" "+annotationGroups[i]+"="+count);
    }
    foreach(D.DrawingView view in sheet.DrawingViews)foreach(D.CuttingPlane cp in view.CuttingPlanes){sh.Add(AnnotationSnapshot(cp,"CuttingPlane"));Console.WriteLine(name+" "+sheet.Name+" cutting plane "+cp.Caption);}
   }}finally{if(!open)draft.Close(false);Marshal.ReleaseComObject(draft);}
  }
  new XDocument(result).Save(Path.Combine(root,"annotation-property-audit.xml"));
 }
}
