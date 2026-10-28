using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Xml.Linq;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;
using F=SolidEdge.Framework.Interop;

class DimensionStandardTool {
 static dynamic app;
 static Type styleType;
 static string root;
 [STAThread]static int Main(string[] args){
  try{using(var filter=new BusyFilter()){
   root=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,".."));
   if(args.Length>0 && args[0]=="--test"){AxisConstraints.Test();Console.WriteLine("PASS: horizontal, vertical, diagonal and zero-length classification");return 0;}
   styleType=Assembly.LoadFrom(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Siemens","Solid Edge 2026","Program","interop.SolidEdgeFrameworkSupportLib.dll")).GetType("SolidEdge.FrameworkSupport.Interop.DimStyle");
   app=Marshal.GetActiveObject("SolidEdge.Application");
   if(args.Length>0 && args[0]=="--apply"){ApplyAndVerify();return 0;}
   if(args.Length>0 && args[0]=="--constraints"){ConstrainCuts();return 0;}
   var audit=new XElement("DimensionAudit");
   Audit(Path.Combine(root,"detailing example 1.dft"),"Reference",audit);
   Audit(Path.GetFullPath(Path.Combine(root,"..","HydraulicFitting","Models","BSPT-02 to SAE-04 ORB Adapter.dft")),"Fitting",audit);
   new XDocument(audit).Save(Path.Combine(root,"dimension-property-audit.xml"));
   Console.WriteLine("Saved dimension-property-audit.xml");
  }return 0;}catch(Exception ex){Console.WriteLine(ex);return 1;}
 }
 static void ConstrainCuts(){
  var report=new System.Text.StringBuilder();
  foreach(string file in new[]{"BSPT-02 to SAE-04 ORB Adapter.dft","BSPT-02 to SAE-04 ORB Adapter - Plane Sections.dft"}){
   string path=Path.GetFullPath(Path.Combine(root,"..","HydraulicFitting","Models",file));
   var draft=(D.DraftDocument)app.Documents.Open(path);int added=0,verified=0;
   foreach(D.Sheet sheet in draft.Sheets){if(sheet.SectionType!=D.SheetSectionTypeConstants.igWorkingSection)continue;
    foreach(D.DrawingView view in sheet.DrawingViews)foreach(D.CuttingPlane cp in view.CuttingPlanes){
     var profile=cp.Profile;profile.BeginEdit();
     try{var relations=profile.Relations2d;
      try{foreach(S.Line2d line in profile.Lines2d){try{if(AxisConstraints.Ensure(relations,line))added++;}finally{Marshal.ReleaseComObject(line);}}}
      finally{Marshal.ReleaseComObject(relations);}
     }finally{profile.EndEdit();Marshal.ReleaseComObject(profile);}
    }
   }
   draft.Save();draft.Close(false);Marshal.ReleaseComObject(draft);
   draft=(D.DraftDocument)app.Documents.Open(path);
   foreach(D.Sheet sheet in draft.Sheets){if(sheet.SectionType!=D.SheetSectionTypeConstants.igWorkingSection)continue;
    foreach(D.DrawingView view in sheet.DrawingViews)foreach(D.CuttingPlane cp in view.CuttingPlanes){
     var profile=cp.Profile;
     try{foreach(S.Line2d line in profile.Lines2d){try{
      double x,y,u,v;line.GetStartPoint(out x,out y);line.GetEndPoint(out u,out v);int kind=AxisConstraints.Direction(x,y,u,v);
      if(kind!=0){if(!AxisConstraints.Has(line,kind))throw new Exception("Saved cutting-plane constraint missing");verified++;}
     }finally{Marshal.ReleaseComObject(line);}}}finally{Marshal.ReleaseComObject(profile);}
    }
   }
   report.AppendLine(file+": added "+added+"; verified "+verified+" axis constraints after save/reopen.");
   Marshal.ReleaseComObject(draft);
  }
  File.WriteAllText(Path.Combine(root,"cutting-plane-constraint-verification.txt"),report.ToString());Console.WriteLine(report);
 }
 // Change only presentation on existing associated dimensions. Saving the open
 // document avoids SaveAs name collisions and retains all native references.
 static void ApplyAndVerify(){
  string path=Path.GetFullPath(Path.Combine(root,"..","HydraulicFitting","Models","BSPT-02 to SAE-04 ORB Adapter.dft"));
  D.DraftDocument draft=(D.DraftDocument)app.Documents.Open(path);
  var standard=XDocument.Load(Path.Combine(root,"dimension-presentation-standard.xml")).Root;
  var before=new XElement("Before");Audit(path,"Fitting",before);
  foreach(D.Sheet sheet in draft.Sheets){if(sheet.SectionType!=D.SheetSectionTypeConstants.igWorkingSection)continue;
   var dimensions=(S.Dimensions)sheet.Dimensions;
   for(int i=1;i<=dimensions.Count;i++){
    var dim=dimensions.Item(i);var style=dim.Style;
    double value=dim.Value,second=dim.SecondValue;var kind=dim.DimensionType;var display=dim.DisplayType;
    string upper=dim.PrimaryUpperTolerance,lower=dim.PrimaryLowerTolerance,prefix=dim.PrefixString,suffix=dim.SuffixString;
    ReferenceDimensionStyle.Apply(style);
    foreach(var property in standard.Elements("Property")){
     string name=(string)property.Attribute("name"),expected=(string)property.Attribute("value");
     string actual=Value(styleType.GetProperty(name).GetValue(style,null));
     if(actual!=expected)throw new Exception("Style readback failed: "+i+" "+name+" "+actual+" != "+expected);
    }
    if(dim.Value!=value||dim.SecondValue!=second||dim.DimensionType!=kind||dim.DisplayType!=display||dim.PrimaryUpperTolerance!=upper||dim.PrimaryLowerTolerance!=lower||dim.PrefixString!=prefix||dim.SuffixString!=suffix)throw new Exception("Dimension meaning changed: "+i);
    Marshal.ReleaseComObject(style);Marshal.ReleaseComObject(dim);
   }
  }
  draft.Save();draft.Close(false);Marshal.ReleaseComObject(draft);
  var after=new XElement("After");Audit(path,"Fitting",after);
  var oldDims=before.Element("Fitting").Elements("Dimension").GetEnumerator();
  int count=0;
  foreach(var dim in after.Element("Fitting").Elements("Dimension")){
   if(!oldDims.MoveNext())throw new Exception("Dimension count changed");
   var previous=oldDims.Current;
   if((string)dim.Attribute("valueMeters")!=(string)previous.Attribute("valueMeters")||(string)dim.Attribute("kind")!=(string)previous.Attribute("kind"))throw new Exception("Saved dimension changed");
   foreach(var p in previous.Elements("Property")){
    string name=(string)p.Attribute("name");
    XElement wanted=null;foreach(var q in standard.Elements("Property"))if((string)q.Attribute("name")==name)wanted=q;
    if(wanted==null)wanted=p;
    foreach(var actual in dim.Elements("Property"))if((string)actual.Attribute("name")==name&&(string)actual.Attribute("value")!=(string)wanted.Attribute("value"))throw new Exception("Saved style mismatch: "+name);
   }
   count++;
  }
  if(oldDims.MoveNext())throw new Exception("Dimension count changed");
  new XDocument(new XElement("Verification",before,after)).Save(Path.Combine(root,"dimension-style-verification.xml"));
  draft=(D.DraftDocument)app.Documents.Open(path);draft.ActiveSheet.Activate();app.DoIdle();
  var window=(D.SheetWindow)app.ActiveWindow;window.FitEx(D.SheetFitConstants.igFitSheet);
  window.SaveAsImage(Path.Combine(Path.GetDirectoryName(path),"Adapter-drawing.jpg"),3060,1980,1,24,F.SeImageQualityType.seImageQualityHigh,false);
  Console.WriteLine("PASS: "+count+" dimensions match 142 presentation properties after save/reopen; values, units, precision, tolerances and native types retained.");
  Marshal.ReleaseComObject(window);Marshal.ReleaseComObject(draft);
 }
 static void Audit(string path,string label,XElement audit){
  bool open=false;for(int i=1;i<=app.Documents.Count;i++)if(string.Equals((string)app.Documents.Item(i).FullName,path,StringComparison.OrdinalIgnoreCase))open=true;
  D.DraftDocument draft=(D.DraftDocument)app.Documents.Open(path);
  var group=new XElement(label,new XAttribute("file",path));audit.Add(group);
  try{
   foreach(D.Sheet sheet in draft.Sheets){if(sheet.SectionType!=D.SheetSectionTypeConstants.igWorkingSection)continue;
    S.Dimensions dimensions=(S.Dimensions)sheet.Dimensions;Console.WriteLine(label+" "+sheet.Name+" dimensions="+dimensions.Count);
    for(int i=1;i<=dimensions.Count;i++){
     var dim=dimensions.Item(i);var item=new XElement("Dimension",new XAttribute("sheet",sheet.Name),new XAttribute("index",i),new XAttribute("kind",dim.DimensionType),new XAttribute("valueMeters",dim.Value.ToString("R",CultureInfo.InvariantCulture)));group.Add(item);
     var style=dim.Style;
     foreach(PropertyInfo property in styleType.GetProperties()){
      if(!property.CanRead||property.GetIndexParameters().Length!=0)continue;
      try{object value=property.GetValue(style,null);if(value==null)continue;var p=new XElement("Property",new XAttribute("name",property.Name),new XAttribute("value",Value(value)),new XAttribute("type",property.PropertyType.FullName));if(property.PropertyType.IsEnum)p.Add(new XAttribute("label",value.ToString()));item.Add(p);}
      catch(Exception ex){item.Add(new XElement("Unavailable",new XAttribute("name",property.Name),new XAttribute("reason",ex.GetBaseException().Message)));}
     }
     Console.WriteLine("  "+i+" "+dim.DimensionType+" "+dim.Value+" style="+style.Name+" driven="+style.DrivenColor);
     Marshal.ReleaseComObject(style);Marshal.ReleaseComObject(dim);
    }
   }
  }finally{if(!open)draft.Close(false);Marshal.ReleaseComObject(draft);}
 }
 static string Value(object value){if(value.GetType().IsEnum)return Convert.ToInt32(value).ToString(CultureInfo.InvariantCulture);if(value is double)return ((double)value).ToString("R",CultureInfo.InvariantCulture);return Convert.ToString(value,CultureInfo.InvariantCulture);}
}
[ComImport,Guid("00000016-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMessageFilter{
 [PreserveSig]int HandleInComingCall(int type,IntPtr caller,int ticks,IntPtr info);
 [PreserveSig]int RetryRejectedCall(IntPtr callee,int ticks,int type);
 [PreserveSig]int MessagePending(IntPtr callee,int ticks,int type);
}
sealed class BusyFilter:IMessageFilter,IDisposable{
 IMessageFilter previous;
 public BusyFilter(){Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(this,out previous));}
 public int HandleInComingCall(int type,IntPtr caller,int ticks,IntPtr info){return 0;}
 public int RetryRejectedCall(IntPtr callee,int ticks,int type){return type==2&&ticks<30000?250:-1;}
 public int MessagePending(IntPtr callee,int ticks,int type){return 2;}
 public void Dispose(){IMessageFilter removed;Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(previous,out removed));GC.KeepAlive(this);}
 [DllImport("ole32.dll")]static extern int CoRegisterMessageFilter(IMessageFilter current,out IMessageFilter previous);
}
