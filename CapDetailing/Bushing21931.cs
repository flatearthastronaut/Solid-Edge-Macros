using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;
using F=SolidEdge.Framework.Interop;
using P=SolidEdge.Part.Interop;

partial class CapDetailTool {
 const string Bushing21931Name="bushing 21931";
 static void Bushing21931Context(){capName=Bushing21931Name;}
 static void Bushing21931Audit(){
  Bushing21931Context();string previousRoot=root;
  try{
   root=CapWork;Directory.CreateDirectory(Path.Combine(root,"Reference Previews"));
   AuditReferences("21930-04 bushing.dft");
   partPath=Path.Combine(folder,Bushing21931Name+".par");AuditPart(false);
  }finally{root=previousRoot;}
 }
 static void BushingPlaceView(D.DrawingView view,double x,double y){
  // This supplied part is offset from its Base CSYS. Locate its actual axial
  // midpoint rather than assuming that the drawing origin is the part center.
  var p=CMap(view,0,.06731,.0534543);double a,b;view.GetOrigin(out a,out b);view.SetOrigin(a+x-p[0],b+y-p[1]);
 }
 static void BushingSectionMembers(D.ModelMembers members){
  foreach(D.ModelMember member in members){try{
   if(member.ComponentType==D.ModelMemberComponentTypeConstants.seSolidBodyMemberType)member.DisplayType=D.ModelMemberDisplayTypeConstants.seSectionPart;
   else if(member.ComponentType!=D.ModelMemberComponentTypeConstants.sePartMemberType)member.DisplayType=D.ModelMemberDisplayTypeConstants.seHidePart;
   var children=member.ModelMembers;try{BushingSectionMembers(children);}finally{Release(children);}
  }finally{Release(member);}}
 }
 static void Bushing21931Review(){
  Bushing21931Context();LoadCap();string source=Path.Combine(folder,"21930-04 bushing.dft"),hash=Digest(source);
  BushingSectionMembers(csection.ModelMembers);csection.ForceUpdate();
  bool wasOpen=IsOpen(source);var reference=(D.DraftDocument)app.Documents.Open(source);
  try{var iso=reference.Sheets.Item("Sheet1").DrawingViews.Item(2);double a,b,c,d,e,f,rotation;D.ViewOrientationConstants type;
   iso.ViewOrientation(out a,out b,out c,out d,out e,out f,out type);iso.GetRotationAngle(out rotation);
   ciso.SetViewOrientation(a,b,c,d,e,f);ciso.SetRotationAngle(rotation);ciso.Update();BushingPlaceView(ciso,.285,.225);Release(iso);
  }finally{if(!wasOpen)reference.Close(false);Release(reference);}
  // Remove the generic optional-hardware placeholder table; no hardware or
  // manufacturing notes have been requested during the view-layout stage.
  for(int i=cdoc.Tables.Count;i>=1;i--){var table=cdoc.Tables.Item(i);try{bool placeholder=false;foreach(D.TableTitle title in table.Titles){try{placeholder|=(title.value??"").Contains("INSTALL THE FOLLOWING ITEMS");}finally{Release(title);}}if(placeholder)table.Delete();}finally{Release(table);}}
  cdoc.Save();cdoc.Close(false);Release(cdoc);LoadCap();
  // Saving a linked draft may update model metadata. Verify the actual solid
  // against the independent pre-drawing audit instead of equating file hashes
  // with geometry identity.
  var baseline=XDocument.Load(Path.Combine(CapWork,"part-audit.xml")).Root.Element("Model");
  P.PartDocument part=null;string path=Path.Combine(folder,Bushing21931Name+".par");
  for(int i=1;i<=app.Documents.Count;i++){object item=app.Documents.Item(i);if(string.Equals(((dynamic)item).FullName,path,StringComparison.OrdinalIgnoreCase)){part=(P.PartDocument)item;break;}Release(item);}
  if(part==null)part=(P.PartDocument)app.Documents.Open(path);
  var model=part.Models.Item(1);var body=(SolidEdge.Geometry.Interop.Body)model.Body;
  try{double expected=double.Parse((string)baseline.Attribute("volume"),System.Globalization.CultureInfo.InvariantCulture);CRequire(body.Valid&&Math.Abs(body.Volume-expected)<1e-15,"Model solid changed");
   Array low=new double[3],high=new double[3];body.GetExactRange(ref low,ref high);
   var ranges=new[]{(double[])low,(double[])high};int index=0;foreach(string attr in new[]{"low","high"}){var values=((string)baseline.Attribute(attr)).Split(',').Select(s=>double.Parse(s,System.Globalization.CultureInfo.InvariantCulture)).ToArray();for(int i=0;i<3;i++)CRequire(Math.Abs(values[i]-ranges[index][i])<1e-10,"Model bounds changed");index++;}
  }finally{Release(body);Release(model);Release(part);}
  CRequire(Digest(source)==hash,"Reference drawing changed");
  var fp=CMap(cface,0,.06731,.0534543);var sp=CMap(csection,0,.06731,.0534543);CRequire(Math.Abs(fp[1]-sp[1])<1e-8&&sp[0]<fp[0],"Section alignment changed");
  CRequire(ciso.Shading&&cface.ScaleFactor==1&&csection.ScaleFactor==1&&((S.Dimensions)csh.Dimensions).Count==0&&((S.CenterLines)csh.CenterLines).Count==0&&((S.CenterMarks)csh.CenterMarks).Count==0,"View-stage scope changed");
  var report=XDocument.Load(Path.Combine(CapWork,"view-verification.xml"));report.Root.SetAttributeValue("modelGeometryUnchanged",true);report.Root.SetAttributeValue("constructionGraphics","hidden");report.Root.Elements("View").Remove();foreach(D.DrawingView v in csh.DrawingViews)report.Root.Add(Scalars(v,"Draft","DrawingView","View"));report.Save(Path.Combine(CapWork,"view-verification.xml"));CPreview();
  Console.WriteLine("PASS: section construction graphics hidden; reference ISO orientation matched; saved solid volume and bounds unchanged; views only, ready for review.");
 }
 static void Bushing21931Views(){
  Bushing21931Context();Directory.CreateDirectory(CapWork);AxisConstraints.Test();
  string source=Path.Combine(folder,"21930-04 bushing.dft"),part=Path.Combine(folder,Bushing21931Name+".par");
  string sourceHash=Digest(source),partHash=Digest(part);
  CRequire(!File.Exists(CapPath)&&!IsOpen(CapPath),"Bushing drawing already exists; inspect it rather than creating another version");
  string normal=Path.GetFullPath(Path.Combine(root,"..","HydraulicFitting","tmp","company-normal.dft"));
  cdoc=(D.DraftDocument)app.Documents.Add("SolidEdge.DraftDocument",normal);csh=cdoc.ActiveSheet;csh.Name="Sheet1";
  // The reference supplies the layout, not its old job's dimensions, grind
  // stock, material, or notes. Begin from the normal company draft template.
  dynamic[] groups={csh.Groups,csh.DrawingViews,csh.Dimensions,csh.Balloons,csh.TextBoxes,csh.Leaders,csh.CenterLines,csh.CenterMarks,csh.SurfaceFinishSymbols,csh.BlockOccurrences,csh.Symbols,csh.Lines2d,csh.Arcs2d,csh.Circles2d};
  foreach(dynamic group in groups)for(int i=group.Count;i>=1;i--){object obj=group.Item(i);((dynamic)obj).Delete();Release(obj);}
  csh.Background=cdoc.Sheets.Item("B-Size");csh.BackgroundVisible=true;csh.SheetSetup.SheetSizeOption=D.PaperSizeConstants.igAnsiBWide;csh.SheetSetup.SetScale(1,1);
  app.SetGlobalParameter(F.ApplicationGlobalConstants.seApplicationGlobalProjectionAngleDraft,(int)D.DrawingViewProjectionAngleConstants.ProjectionAngleThird);cdoc.MaintainAlignmentSet=true;
  var link=cdoc.ModelLinks.Add(part);
  try{
   cface=csh.DrawingViews.AddPartView(link,D.ViewOrientationConstants.igTopView,1,.285,.145,D.PartDrawingViewTypeConstants.sePartDesignedView);
   cface.SetViewOrientation(0,0,1,1,0,0);cface.DisplayCaption=false;cface.DisplayScale=false;cface.Defaults_ShowHiddenEdges=true;cface.Defaults_HiddenEdgeStyleName="Hidden";SetHiddenMembers(cface.ModelMembers,true);cface.Update();BushingPlaceView(cface,.285,.145);
   var cp=cface.CuttingPlanes.Add();cp.Caption="A";ReferenceAnnotationStyle.Apply("CuttingPlane",cp);
   var p=CMap(cface,0,.06731-.016,.0645668);var q=CMap(cface,0,.06731+.016,.0645668);var profile=cp.Profile;
   profile.BeginEdit();try{var line=profile.Lines2d.AddBy2Points(p[0],p[1],q[0],q[1]);try{AxisConstraints.Ensure(profile.Relations2d,line);}finally{Release(line);}}finally{profile.EndEdit();Release(profile);}
   // The finite cut segment surrounds the bushing, away from its offset Base
   // origin. The shared incidence check adds a Base point-on only if applicable.
   int bases=BaseOriginConstraints.Ensure(cp);cp.ViewSide=F.SectionViewProfileSide.igRightProfileSide;
   csection=cp.CreateView(D.DraftSectionViewType.seDraftSectionViewTypeStandard);csection.Sheet=csh;csection.ScaleFactor=1;csection.Crop=false;csection.CroppedVHL=false;csection.LockView=false;csection.SectionOnly=false;csection.SectionFullModel=true;csection.Defaults_ShowSectionHatch=true;csection.Defaults_ShowHiddenEdges=false;
   BushingSectionMembers(csection.ModelMembers);
   csection.ForceUpdate();BushingPlaceView(csection,.150,.145);csection.DisplayCaption=true;csection.Caption="SECTION";csection.DisplayScale=false;
   var selection=cdoc.SelectSet;try{selection.RemoveAll();selection.Add(cface);selection.Add(csection);csh.DrawingViews.Align();selection.RemoveAll();}finally{Release(selection);}
   // Verify real alignment by moving the parent and restoring it. Also verify
   // the cutting line's native Vertical relation after that movement.
   double ox,oy;cface.GetOrigin(out ox,out oy);var before=CMap(csection,0,.06731,.0534543);
   try{cface.SetOrigin(ox,oy+.001);var moved=CMap(csection,0,.06731,.0534543);CRequire(Math.Abs(moved[1]-before[1]-.001)<1e-8,"Section failed native alignment movement check");}finally{cface.SetOrigin(ox,oy);}
   csection.SetCaptionPosition(.150,.111);cp.SetCaptionPosition(true,.310,.129);cp.SetCaptionPosition(false,.310,.161);
   ciso=csh.DrawingViews.AddPartView(link,D.ViewOrientationConstants.igTopFrontRightView,1,.285,.225,D.PartDrawingViewTypeConstants.sePartDesignedView);ciso.DisplayCaption=false;ciso.DisplayScale=false;ciso.Defaults_ShowHiddenEdges=false;ciso.Shading=true;ciso.ShadingShowVisibleEdges=true;ciso.ShadingGrayscale=false;ciso.ShadingAllowPartFaceColors=true;ciso.Update();BushingPlaceView(ciso,.285,.225);
   csh.SheetSetup.DrawingViewForSheetScale=cface;cdoc.UpdatePropertyTextCacheAndDisplay();
   CRequire(BaseOriginConstraints.Ensure(cp,true)==bases,"Base relation changed after moving the view");Release(cp);
  }finally{Release(link);}
  cdoc.SaveAs(CapPath);cdoc.Close(false);Release(cdoc);LoadCap();
  CRequire(csh.DrawingViews.Count==3&&csh.SheetSetup.SheetSizeOption==D.PaperSizeConstants.igAnsiBWide,"Saved views or B-size sheet changed");
  CRequire(cface.ScaleFactor==1&&csection.ScaleFactor==1&&ciso.ScaleFactor==1&&ciso.Shading&&cface.Defaults_ShowHiddenEdges,"Saved scale/shading/hidden lines differ");
  var faceCenter=CMap(cface,0,.06731,.0534543);var sectionCenter=CMap(csection,0,.06731,.0534543);
  CRequire(Math.Abs(faceCenter[1]-sectionCenter[1])<1e-8&&sectionCenter[0]<faceCenter[0],"Saved section alignment differs");
  var cut=cface.CuttingPlanes.Item(1);CRequire(cut.ViewSide==F.SectionViewProfileSide.igRightProfileSide,"Saved cutting direction differs");
  foreach(S.Line2d line in cut.Profile.Lines2d){try{CRequire(AxisConstraints.Has(line,(int)F.ObjectType.igVerticalRelation2d),"Cutting line lost its Vertical constraint");}finally{Release(line);}}
  int baseCount=BaseOriginConstraints.Ensure(cut,true);Release(cut);
  CRequire(((S.Dimensions)csh.Dimensions).Count==0&&((S.Balloons)csh.Balloons).Count==0&&((S.CenterLines)csh.CenterLines).Count==0&&((S.CenterMarks)csh.CenterMarks).Count==0,"Detailing added before view review");
  CRequire(Digest(source)==sourceHash,"Reference drawing changed");
  var report=new XElement("BushingViewVerification",new XAttribute("source","21930-04 bushing.dft"),new XAttribute("sheet","B"),new XAttribute("scale","1:1"),new XAttribute("alignmentMovementCheck","passed"),new XAttribute("baseOriginRelations",baseCount),new XAttribute("sourceUnchanged",true),new XAttribute("partFileUnchanged",Digest(part)==partHash));
  foreach(D.DrawingView view in csh.DrawingViews)report.Add(Scalars(view,"Draft","DrawingView","View"));
  new XDocument(report).Save(Path.Combine(CapWork,"view-verification.xml"));CPreview();
  Console.WriteLine("PASS: three model-linked views, third-angle aligned section, hidden end view, shaded ISO, native Vertical cutting line, B-size 1:1. Saved/reopened; paused for view review.");
 }
}
