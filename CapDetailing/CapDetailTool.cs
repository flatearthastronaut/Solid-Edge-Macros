using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;
using F=SolidEdge.Framework.Interop;
using P=SolidEdge.Part.Interop;
using G=SolidEdge.Geometry.Interop;

partial class CapDetailTool {
 static dynamic app;
 static string root,folder=@"C:\Users\greg.lentner\Documents\Codex\cap detailing refs";
 static string partPath=Path.Combine(folder,"End Cap_21612-100.par");
 const double Inch=.0254;
 [STAThread]static int Main(string[] args){try{using(var filter=new BusyFilter()){
  root=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,".."));
  app=Marshal.GetActiveObject("SolidEdge.Application");
  if(args.Length==0||args[0]=="--refs")AuditReferences();
  else if(args[0]=="--part")AuditPart();
  else if(args[0]=="--create")CreateDrawing();
  else if(args[0]=="--finish")CreateDrawing(true);
  else if(args[0]=="--verify")VerifySavedDrawing();
  else throw new ArgumentException("Unknown mode");
 }return 0;}catch(Exception ex){Console.WriteLine(ex);return 1;}}
 static Type Api(string library,string name){return Assembly.LoadFrom(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Siemens","Solid Edge 2026","Program","interop.SolidEdge"+library+"Lib.dll")).GetType("SolidEdge."+library+".Interop."+name);}
 static string Val(object value){if(value.GetType().IsEnum)return value.ToString();return Convert.ToString(value,CultureInfo.InvariantCulture);}
 static XElement Scalars(object obj,string library,string type,string tag){var e=new XElement(tag);foreach(var p in Api(library,type).GetProperties()){
  if(!p.CanRead||p.GetIndexParameters().Length!=0||!(p.PropertyType.IsPrimitive||p.PropertyType.IsEnum||p.PropertyType==typeof(string)))continue;
  try{var value=p.GetValue(obj,null);if(value!=null)e.Add(new XAttribute(p.Name,Val(value)));}catch{}
 }return e;}
 static bool IsOpen(string path){for(int i=1;i<=app.Documents.Count;i++){dynamic doc=app.Documents.Item(i);try{if(string.Equals((string)doc.FullName,path,StringComparison.OrdinalIgnoreCase))return true;}finally{Marshal.ReleaseComObject(doc);}}return false;}
 static void AuditReferences(){
  var result=new XElement("CapReferenceAudit");
  foreach(string path in Directory.GetFiles(folder,"*.dft")){
   if(Path.GetFileName(path).StartsWith("End Cap_21612-100",StringComparison.OrdinalIgnoreCase))continue;
   bool open=IsOpen(path);var draft=(D.DraftDocument)app.Documents.Open(path);var doc=new XElement("Drawing",new XAttribute("file",Path.GetFileName(path)));result.Add(doc);
   try{foreach(D.Sheet sheet in draft.Sheets){if(sheet.SectionType!=D.SheetSectionTypeConstants.igWorkingSection)continue;
    var sh=new XElement("Sheet",new XAttribute("name",sheet.Name),new XAttribute("background",sheet.Background.Name));doc.Add(sh);
    int vi=0;foreach(D.DrawingView view in sheet.DrawingViews){var v=Scalars(view,"Draft","DrawingView","View");v.SetAttributeValue("index",++vi);double x,y,a,b,c,d;view.GetOrigin(out x,out y);view.Range(out a,out b,out c,out d);v.SetAttributeValue("origin",x+","+y);v.SetAttributeValue("range",a+","+b+","+c+","+d);sh.Add(v);
     foreach(D.CuttingPlane cp in view.CuttingPlanes){var cut=Scalars(cp,"Draft","CuttingPlane","CuttingPlane");v.Add(cut);foreach(S.Line2d line in cp.Profile.Lines2d){line.GetStartPoint(out a,out b);line.GetEndPoint(out c,out d);cut.Add(new XElement("Segment",new XAttribute("xy",a+","+b+","+c+","+d),new XAttribute("relations",line.Relationships.Count)));}}
    }
    foreach(S.Dimension dim in (S.Dimensions)sheet.Dimensions){var e=Scalars(dim,"FrameworkSupport","Dimension","Dimension");e.Add(Scalars(dim.Style,"FrameworkSupport","DimStyle","Style"));double a,b,c,d;dim.Range(out a,out b,out c,out d);e.SetAttributeValue("range",a+","+b+","+c+","+d);sh.Add(e);}
    foreach(S.Balloon b in (S.Balloons)sheet.Balloons){var e=Scalars(b,"FrameworkSupport","Balloon","Callout");e.Add(Scalars(b.Style,"FrameworkSupport","DimStyle","Style"));double a,c,d,f;b.Range(out a,out c,out d,out f);e.SetAttributeValue("range",a+","+c+","+d+","+f);sh.Add(e);}
    foreach(S.CenterLine line in (S.CenterLines)sheet.CenterLines){var e=Scalars(line,"FrameworkSupport","CenterLine","CenterLine");double a,b,c,d;line.Range(out a,out b,out c,out d);e.SetAttributeValue("range",a+","+b+","+c+","+d);sh.Add(e);}
    sh.SetAttributeValue("centerMarks",((S.CenterMarks)sheet.CenterMarks).Count);
    foreach(S.TextBox text in (S.TextBoxes)sheet.TextBoxes)sh.Add(Scalars(text,"FrameworkSupport","TextBox","Text"));
    foreach(S.SurfaceFinishSymbol symbol in (S.SurfaceFinishSymbols)sheet.SurfaceFinishSymbols)sh.Add(Scalars(symbol,"FrameworkSupport","SurfaceFinishSymbol","Finish"));
    sheet.Activate();app.DoIdle();var window=(D.SheetWindow)app.ActiveWindow;window.FitEx(D.SheetFitConstants.igFitSheet);
    string image=Path.Combine(root,"Reference Previews",Path.GetFileNameWithoutExtension(path)+" - "+sheet.Name+".jpg");window.SaveAsImage(image,3000,2000,1,24,F.SeImageQualityType.seImageQualityHigh,false);Marshal.ReleaseComObject(window);
    Console.WriteLine(Path.GetFileName(path)+" | "+sheet.Name+" views="+sheet.DrawingViews.Count+" dimensions="+((S.Dimensions)sheet.Dimensions).Count+" callouts="+((S.Balloons)sheet.Balloons).Count);
   }}finally{if(!open)draft.Close(false);Marshal.ReleaseComObject(draft);}
   new XDocument(result).Save(Path.Combine(root,"reference-audit.xml"));
  }
 }
 static void AuditPart(){
  var part=(P.PartDocument)app.Documents.Open(partPath);var result=Scalars(part,"Part","PartDocument","Part");
  Console.WriteLine("Part properties");var sets=(F.PropertySets)part.Properties;for(int si=1;si<=sets.Count;si++){var props=sets.Item(si);var group=new XElement("PropertySet",new XAttribute("name",props.Name));result.Add(group);for(int pi=1;pi<=props.Count;pi++){var p=props.Item(pi);try{group.Add(new XElement("Property",new XAttribute("name",p.Name),new XAttribute("value",Val(p.get_Value()))));}catch{}}}
  Console.WriteLine("Models="+part.Models.Count);foreach(P.Model model in part.Models){var m=new XElement("Model",new XAttribute("name",model.Name));result.Add(m);var body=(G.Body)model.Body;Array low=new double[3],high=new double[3];body.GetExactRange(ref low,ref high);m.SetAttributeValue("low",string.Join(",",(double[])low));m.SetAttributeValue("high",string.Join(",",(double[])high));m.SetAttributeValue("volume",body.Volume);m.SetAttributeValue("valid",body.Valid);
   string[] groups={"ExtrudedProtrusions","RevolvedProtrusions","ExtrudedCutouts","RevolvedCutouts","Holes","Rounds","Chamfers","Patterns","Threads","Mirrors"};
   foreach(string group in groups){var property=Api("Part","Model").GetProperty(group);if(property==null)continue;object collection=property.GetValue(model,null);try{int fc=(int)((dynamic)collection).Count;Console.WriteLine(group+"="+fc);if(fc==0)continue;foreach(object feature in (IEnumerable)collection){try{string type=group.Substring(0,group.Length-1);var e=Scalars(feature,"Part",type,type);m.Add(e);if(type=="Hole")e.Add(Scalars(((P.Hole)feature).HoleData,"Part","HoleData","HoleData"));}finally{Marshal.ReleaseComObject(feature);}}}finally{Marshal.ReleaseComObject(collection);}}
  }
  Console.WriteLine("Profiles");foreach(P.ProfileSet set in part.ProfileSets)foreach(P.Profile profile in set.Profiles){var pr=new XElement("Profile",new XAttribute("name",profile.Name));result.Add(pr);
   foreach(S.Line2d line in profile.Lines2d){double a,b,c,d,x,y,z,u,v,w;line.GetStartPoint(out a,out b);line.GetEndPoint(out c,out d);profile.Convert2DCoordinate(a,b,out x,out y,out z);profile.Convert2DCoordinate(c,d,out u,out v,out w);pr.Add(new XElement("Line",new XAttribute("xyz",x+","+y+","+z+","+u+","+v+","+w)));}
   foreach(S.Circle2d circle in profile.Circles2d){double a,b,x,y,z;circle.GetCenterPoint(out a,out b);profile.Convert2DCoordinate(a,b,out x,out y,out z);pr.Add(new XElement("Circle",new XAttribute("xyz",x+","+y+","+z),new XAttribute("radius",circle.Radius)));}
  }
  new XDocument(result).Save(Path.Combine(root,"part-audit.xml"));Console.WriteLine("Part audit saved.");
  app.StartCommand(32810);app.StartCommand(32821);app.DoIdle();var view=(F.View)app.ActiveWindow.View;view.Fit();view.SaveAsImage(Path.Combine(root,"End Cap-model.jpg"),1800,1400,Resolution:1,ColorDepth:24,ImageQuality:F.SeImageQualityType.seImageQualityHigh,Invert:false);
 }
}
