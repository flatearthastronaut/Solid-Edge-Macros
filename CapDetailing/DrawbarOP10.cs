using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;
using F=SolidEdge.Framework.Interop;
using P=SolidEdge.Part.Interop;
using G=SolidEdge.Geometry.Interop;

partial class CapDetailTool {
 const string DrawbarName="drawbar adapter OP10";
 static void DrawbarContext(){capName=DrawbarName;}
 static void DrawbarViews(){
  DrawbarContext();Directory.CreateDirectory(CapWork);
  CRequire(!File.Exists(CapPath),"Drawing exists; review it instead of rebuilding.");
  string source=Path.Combine(folder,"CAP_C.dft"),hash=Digest(source);bool wasOpen=IsOpen(source);
  var original=(D.DraftDocument)app.Documents.Open(source);
  try{if(!File.Exists(CapPath))original.SaveCopyAs(CapPath);}finally{if(!wasOpen)original.Close(false);Release(original);}
  cdoc=(D.DraftDocument)app.Documents.Open(CapPath);csh=cdoc.Sheets.Item("Sheet1");csh.Activate();
  // Retain only the normal company border and its audited annotation styles.
  // Different topology requires fresh model-linked views and dimensions.
  dynamic[] groups={csh.Dimensions,csh.Balloons,csh.TextBoxes,csh.SurfaceFinishSymbols,csh.CenterLines,csh.CenterMarks,csh.DrawingViews};
  foreach(dynamic group in groups)for(int i=group.Count;i>=1;i--){object item=group.Item(i);((dynamic)item).Delete();Release(item);}
  var link=cdoc.ModelLinks.Add(Path.Combine(folder,DrawbarName+".par"));
  app.SetGlobalParameter(F.ApplicationGlobalConstants.seApplicationGlobalProjectionAngleDraft,(int)D.DrawingViewProjectionAngleConstants.ProjectionAngleThird);
  cdoc.MaintainAlignmentSet=true;
  cface=csh.DrawingViews.AddPartView(link,D.ViewOrientationConstants.igTopView,1,.32,.18,D.PartDrawingViewTypeConstants.sePartDesignedView);
  cface.SetViewOrientation(0,0,1,1,0,0);cface.DisplayCaption=false;cface.DisplayScale=false;cface.Defaults_ShowHiddenEdges=true;cface.Defaults_HiddenEdgeStyleName="Hidden";SetHiddenMembers(cface.ModelMembers,true);cface.Update();
  var cp=cface.CuttingPlanes.Add();cp.Caption="A";ReferenceAnnotationStyle.Apply("CuttingPlane",cp);
  var a=CMap(cface,0,-.053,0);var b=CMap(cface,0,.053,0);var pr=cp.Profile;pr.BeginEdit();
  try{var line=pr.Lines2d.AddBy2Points(a[0],a[1],b[0],b[1]);AxisConstraints.Ensure(pr.Relations2d,line);}finally{pr.EndEdit();}
  BaseOriginConstraints.Ensure(cp);cp.ViewSide=F.SectionViewProfileSide.igRightProfileSide;
  csection=cp.CreateView(D.DraftSectionViewType.seDraftSectionViewTypeStandard);csection.Sheet=csh;csection.ScaleFactor=1;csection.Crop=false;csection.CroppedVHL=false;csection.LockView=false;csection.SectionOnly=false;csection.SectionFullModel=true;csection.Defaults_ShowSectionHatch=true;csection.Defaults_ShowHiddenEdges=false;
  foreach(D.ModelMember m in csection.ModelMembers)m.DisplayType=D.ModelMemberDisplayTypeConstants.seSectionPart;
  csection.ForceUpdate();csection.SetOrigin(.15,.18);csection.DisplayCaption=true;csection.Caption="SECTION";csection.DisplayScale=false;
  var select=cdoc.SelectSet;select.RemoveAll();select.Add(cface);select.Add(csection);csh.DrawingViews.Align();select.RemoveAll();
  ciso=csh.DrawingViews.AddPartView(link,D.ViewOrientationConstants.igTopFrontRightView,.7,.23,.075,D.PartDrawingViewTypeConstants.sePartDesignedView);ciso.DisplayCaption=false;ciso.DisplayScale=false;ciso.Defaults_ShowHiddenEdges=false;ciso.Update();
  csh.SheetSetup.DrawingViewForSheetScale=cface;
  csection.SetCaptionPosition(.125,.105);
  cdoc.UpdatePropertyTextCacheAndDisplay();cdoc.Save();CRequire(Digest(source)==hash,"Source drawing modified");
  DrawbarDump();CPreview();
 }
 static void DrawbarDump(){
  using(var w=new StreamWriter(Path.Combine(CapWork,"projected-geometry.txt"))){
   foreach(D.DrawingView v in csh.DrawingViews){w.WriteLine("VIEW "+v.Index);w.WriteLine("MODEL origin="+string.Join(",",CMap(v,0,0,0))+" z-="+string.Join(",",CMap(v,0,0,-.0508))+" y+="+string.Join(",",CMap(v,0,.0425,0)));
    foreach(D.DVLine2d l in v.DVLines2d){double x,y,a,b;l.GetStartPoint(out x,out y);l.GetEndPoint(out a,out b);v.ViewToSheet(x,y,out x,out y);v.ViewToSheet(a,b,out a,out b);w.WriteLine("LINE "+x+","+y+","+a+","+b);}
    foreach(D.DVArc2d arc in v.DVArcs2d){double x,y;arc.GetCenterPoint(out x,out y);v.ViewToSheet(x,y,out x,out y);w.WriteLine("ARC radius="+arc.Radius+" center="+x+","+y);}
   }
  }
 }
}




partial class CapDetailTool {
 static S.DimDecimalRoundOffTypeConstants EdgeInchPrecision(double meters){return meters<=.015*Inch+1e-10?S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_3:S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_2;}
 static S.Dimension DrawbarLinear(double y1,double z1,double y2,double z2,double tx,double ty,double expected,bool vertical,string prefix,string role){
  var p=CMap(csection,0,y1,z1);var q=CMap(csection,0,y2,z2);var a=CEdge(csection,p);var b=CEdge(csection,q);
  try{
   // Model-edge endpoints retain association when the drawing regenerates.
   var dim=((S.Dimensions)csh.Dimensions).AddDistanceBetweenObjects(a.Reference,p[0],p[1],0,true,b.Reference,q[0],q[1],0,true);
   CStyle(dim.Style);dim.MeasurementAxisEx=S.DimAxisModeConstants.igDimAxisModeDefault;dim.MeasurementAxisDirection=vertical;dim.PrefixString=prefix;dim.SetComment(role);
   dim.TrackDistance=vertical?tx-p[0]:p[1]-ty;dim.SetKeyPoint(8,tx,ty,0);
   CRequire(Math.Abs(dim.Value-expected)<1e-8&&dim.StatusOfDimension==S.DimStatusConstants.seDimStatusDriven,"Incorrect "+role+": "+dim.Value);return dim;
  }finally{Release(a);Release(b);}
 }
 static void DrawbarLinearDimensions(){
  DrawbarContext();LoadCap();var dims=(S.Dimensions)csh.Dimensions;
  CRequire(dims.Count==2||dims.Count==5,"Unexpected dimension count; inspect before editing.");
  CRequire(EdgeInchPrecision(.015*Inch)==S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_3&&EdgeInchPrecision(.0151*Inch)==S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_2,"Inch threshold regression");
  CRequire(csh.SheetSetup.SheetSizeOption==D.PaperSizeConstants.igAnsiBWide&&Math.Abs(csection.ScaleFactor-1)<1e-9&&Math.Abs(cface.ScaleFactor-1)<1e-9,"Expected approved 1:1 B-size layout.");
  foreach(S.Dimension d in dims){if(d.GetComment()=="OP10-CHAMFER"||d.GetComment()=="OP10-RADIUS"){
   var metric=d.Style.PrimaryDecimalRoundOff;d.Style.SecondaryDecimalRoundOff=EdgeInchPrecision(d.Value);CRequire(d.Style.PrimaryDecimalRoundOff==metric,"Metric precision changed");}}
  if(dims.Count==2){
   // Both axial dimensions originate from the lower silhouette, in nested
   // rows. This keeps their extension lines away from the existing leaders.
   DrawbarLinear(-.040976,0,-.040976,-.0247135861985738,.163,.123,.0247135861985738,false,"","OP10-FLANGE");
   DrawbarLinear(-.040976,0,-.0251298696020322,-.0508,.150,.109,.0508,false,"","OP10-OVERALL");
   DrawbarLinear(-.0266538696020322,-.049276,.0266538696020322,-.049276,.105,.180,.0533077392040644,true,"%DI ","OP10-PILOT");
  }
  csection.SetCaptionPosition(.150,.090);
  cdoc.Save();cdoc.Close(false);Release(cdoc);LoadCap();dims=(S.Dimensions)csh.Dimensions;
  var expected=new System.Collections.Generic.Dictionary<string,double>{{"OP10-CHAMFER",.001524},{"OP10-RADIUS",.00635},{"OP10-FLANGE",.0247135861985738},{"OP10-OVERALL",.0508},{"OP10-PILOT",.0533077392040644}};
  CRequire(dims.Count==expected.Count,"Incorrect saved dimension count");
  var report=new XElement("DrawbarLinearDimensionVerification",new XAttribute("sheet","B"),new XAttribute("orthographicScale","1:1"));
  foreach(S.Dimension d in dims){string role=d.GetComment();CRequire(expected.ContainsKey(role)&&Math.Abs(d.Value-expected[role])<1e-8&&d.StatusOfDimension==S.DimStatusConstants.seDimStatusDriven&&string.IsNullOrEmpty(d.OverrideString),"Saved dimension differs: "+role);
   CRequire(d.Style.PrimaryDecimalRoundOff==S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_2,"Metric precision differs");
   CRequire(d.Style.SecondaryDecimalRoundOff==(role=="OP10-CHAMFER"||role=="OP10-RADIUS"?EdgeInchPrecision(d.Value):S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_3),"Inch precision differs");
   CRequire(d.Style.DrivenColor==8421376&&Math.Abs(d.Style.Height-.003175)<1e-9,"Dimension styling differs");
   var item=Scalars(d,"FrameworkSupport","Dimension","Dimension");item.Add(CTexts(d).Select(t=>new XElement("DisplayedText",t)));report.Add(item);
   if(role=="OP10-CHAMFER")CRequire(d.SubfixString=="TYP(5)"&&Math.Abs(d.SecondValue-Math.PI/4)<1e-8,"Chamfer count or angle changed");
  }
  CRequire(csh.SheetSetup.SheetSizeOption==D.PaperSizeConstants.igAnsiBWide&&ciso.Shading,"Sheet or ISO changed");
  double x,y,sx,sy;cface.GetOrigin(out x,out y);csection.GetOrigin(out sx,out sy);CRequire(Math.Abs(y-sy)<1e-9&&sx<x&&cface.ScaleFactor==1&&csection.ScaleFactor==1,"View alignment or scale changed");
  CRequire(((S.CenterLines)csh.CenterLines).Count==1&&((S.CenterMarks)csh.CenterMarks).Count==1&&BaseOriginConstraints.Ensure(cface.CuttingPlanes.Item(1),true)==1,"Centerlines or Base constraint changed");
  new XDocument(report).Save(Path.Combine(CapWork,"linear-dimension-verification.xml"));CPreview();Console.WriteLine("PASS: corrected inch-only chamfer/radius precision and three native dimensions; 1:1 B-size retained.");
 }
 static void DrawbarEdgeDimensions(){
  DrawbarContext();LoadCap();
  var dims=(S.Dimensions)csh.Dimensions;CRequire(dims.Count==0||(dims.Count==2&&dims.Cast<S.Dimension>().All(d=>d.GetComment().StartsWith("OP10-"))),"Unexpected dimensions; inspect before changing.");
  var positions=csh.DrawingViews.Cast<D.DrawingView>().Select(v=>{double x,y;v.GetOrigin(out x,out y);return new[]{x,y,v.ScaleFactor};}).ToArray();
  // Count manufactured annular faces, not the two silhouettes of each face
  // in a longitudinal section. This model has a single size in each feature.
  P.PartDocument part=null;
  for(int i=1;i<=app.Documents.Count;i++){object candidate=app.Documents.Item(i);if(string.Equals(((dynamic)candidate).FullName,Path.Combine(folder,DrawbarName+".par"),StringComparison.OrdinalIgnoreCase)){part=(P.PartDocument)candidate;break;}Release(candidate);}
  if(part==null)part=(P.PartDocument)app.Documents.Open(Path.Combine(folder,DrawbarName+".par"));
  int chamferCount,radiusCount;double setback,radiusValue;
  try{
   var model=part.Models.Item(1);CRequire(model.Chamfers.Count==1&&model.Rounds.Count==1,"Feature groups changed; re-audit sizes.");
   var cf=model.Chamfers.Item(1);var rf=model.Rounds.Item(1);
   var faces=(G.Faces)cf.get_Faces(G.FeatureTopologyQueryTypeConstants.igQueryAll);
   var rounds=(G.Faces)rf.get_Faces(G.FeatureTopologyQueryTypeConstants.igQueryAll);
   try{chamferCount=faces.Count;radiusCount=rounds.Count;setback=cf.ChamferSetbackValue1;
    CRequire(chamferCount==5&&radiusCount==1,"Unexpected physical edge count; review feature faces.");
    for(int i=1;i<=faces.Count;i++){var face=(G.Face)faces.Item(i);var cone=face.Geometry as G.Cone;try{CRequire(cone!=null&&Math.Abs(Math.Abs(cone.HalfAngle)-Math.PI/4)<1e-7,"Chamfer surface angle differs");}finally{Release(cone);Release(face);}}
    var roundedFace=(G.Face)rounds.Item(1);var torus=(G.Torus)roundedFace.Geometry;try{radiusValue=torus.MinorRadius;}finally{Release(torus);Release(roundedFace);}
   }finally{Release(faces);Release(rounds);Release(cf);Release(rf);Release(model);}
  }finally{Release(part);}
  CRequire(Math.Abs(setback-.001524)<1e-9&&Math.Abs(radiusValue-.00635)<1e-9,"Model sizes changed");
  cdoc.Activate();csh.Activate();
  if(dims.Count==0){
  var pa=CMap(csection,0,.0425,-.01);var pc=CMap(csection,0,.041738,-.000762);var adjacent=CEdge(csection,pa);var bevel=CEdge(csection,pc);
  var chamfer=dims.AddChamfer(adjacent.Reference,pa[0],pa[1],0,false,bevel.Reference,pc[0],pc[1],0,false,S.DimChamferModeConstants.igDimChamferModePerpendicular);
  CStyle(chamfer.Style);chamfer.Style.ChamferUse45DegreeCharacter=false;chamfer.SetComment("OP10-CHAMFER");chamfer.SubfixString="TYP("+chamferCount+")";
  chamfer.BreakPosition=S.DimBreakPositionConstants.igDimBreakRight;chamfer.LeaderDistance=.008;chamfer.BreakDistance=.015;chamfer.TrackDistance=.785398;PlaceChamferText(chamfer,.205,.237);
  var arc=csection.DVArcs2d.Cast<D.DVArc2d>().OrderByDescending(a=>{double x,y;a.GetCenterPoint(out x,out y);return y;}).First();
  var radius=dims.AddRadius(arc.Reference);CStyle(radius.Style);radius.SetComment("OP10-RADIUS");radius.SubfixString=radiusCount>1?"TYP("+radiusCount+")":"";
  radius.BreakPosition=S.DimBreakPositionConstants.igDimBreakLeft;radius.TrackDistance=.7;radius.LeaderDistance=.006;radius.BreakDistance=.012;PlaceChamferText(radius,.088,.227);
  CRequire(Math.Abs(chamfer.Value-setback)<1e-9&&Math.Abs(chamfer.SecondValue-Math.PI/4)<1e-7&&Math.Abs(radius.Value-radiusValue)<1e-9,"Native dimensions disagree with model features");
  }
  cdoc.Save();cdoc.Close(false);Release(cdoc);LoadCap();dims=(S.Dimensions)csh.Dimensions;
  CRequire(dims.Count==2,"Expected only one chamfer and one radius dimension");
  foreach(S.Dimension d in dims){CRequire(d.StatusOfDimension==S.DimStatusConstants.seDimStatusDriven&&string.IsNullOrEmpty(d.OverrideString),"Dimension lost model link");CRequire(d.Style.DrivenColor==8421376&&Math.Abs(d.Style.Height-.003175)<1e-9,"Dimension appearance differs");
   if(d.DimensionType==S.DimTypeConstants.igDimTypeChamfer)CRequire(Math.Abs(d.Value-setback)<1e-9&&d.SubfixString=="TYP(5)","Chamfer size/count changed");
   else CRequire(Math.Abs(d.Value-radiusValue)<1e-9&&string.IsNullOrEmpty(d.SubfixString),"Radius size/count changed");}
  for(int i=1;i<=3;i++){var v=csh.DrawingViews.Item(i);double x,y;v.GetOrigin(out x,out y);var p=positions[i-1];CRequire(Math.Abs(x-p[0])+Math.Abs(y-p[1])+Math.Abs(v.ScaleFactor-p[2])<1e-9,"Approved view layout changed");}
  CRequire(ciso.Shading&&((S.CenterLines)csh.CenterLines).Count==1&&((S.CenterMarks)csh.CenterMarks).Count==1,"Approved shading or centers changed");
  CRequire(cface.CuttingPlanes.Item(1).ViewSide==F.SectionViewProfileSide.igRightProfileSide&&BaseOriginConstraints.Ensure(cface.CuttingPlanes.Item(1),true)==1,"Cutting plane changed");
  CPreview();File.WriteAllText(Path.Combine(CapWork,"edge-dimension-verification.txt"),"PASS after save/reopen: five distinct conical chamfer faces, 0.060 inch x 45 degrees, one native chamfer dimension with Subfix TYP(5); one toroidal fillet, R0.250 inch, one native radius dimension with no repetition subfix. Both driven, no value overrides. Approved view positions, centerlines, shaded ISO and Base-linked cutting plane retained.");
  Console.WriteLine("PASS: 5 chamfers, 1 continuous fillet; two native dimensions saved. Paused for review.");
 }
 static void DrawbarCenters(){
  DrawbarContext();LoadCap();
  // Preserve the user-approved view positions and section direction. Native
  // center annotations stay associated with projected model edges.
  var positions=csh.DrawingViews.Cast<D.DrawingView>().Select(v=>{double x,y;v.GetOrigin(out x,out y);return new[]{x,y,v.ScaleFactor};}).ToArray();
  CRequire(((S.CenterLines)csh.CenterLines).Count==0&&((S.CenterMarks)csh.CenterMarks).Count==0,"Center annotations already exist; inspect before adding duplicates.");
  var lower=CEdge(csection,CMap(csection,0,-.016688,-.02));
  var upper=CEdge(csection,CMap(csection,0,.016688,-.02));
  var line=((S.CenterLines)csh.CenterLines).AddBetweenTwoParallelLines(lower.Reference,upper.Reference);
  ReferenceAnnotationStyle.Apply("CenterLine",line);
  var circle=cface.DVCircles2d.Cast<D.DVCircle2d>().OrderByDescending(c=>c.Radius).First();
  CRequire(Math.Abs(circle.Radius-.0425)<1e-8,"Unexpected end-view circle");
  var center=CMap(cface,0,0,0);
  var mark=((S.CenterMarks)csh.CenterMarks).AddByCenter(circle.Reference,center[0],center[1],0,true);
  ReferenceAnnotationStyle.Apply("CenterMark",mark);
  ciso.Shading=true;ciso.ShadingShowVisibleEdges=true;ciso.ShadingGrayscale=false;ciso.ShadingAllowPartFaceColors=true;ciso.Update();
  cdoc.Save();cdoc.Close(false);Release(cdoc);LoadCap();
  CRequire(ciso.Shading&&ciso.ShadingShowVisibleEdges,"Isometric shading did not persist");
  CRequire(((S.CenterLines)csh.CenterLines).Count==1&&((S.CenterMarks)csh.CenterMarks).Count==1,"Unexpected center annotation count");
  ReferenceAnnotationStyle.Apply("CenterLine",((S.CenterLines)csh.CenterLines).Item(1),true);
  ReferenceAnnotationStyle.Apply("CenterMark",((S.CenterMarks)csh.CenterMarks).Item(1),true);
  for(int i=1;i<=3;i++){var v=csh.DrawingViews.Item(i);double x,y;v.GetOrigin(out x,out y);var p=positions[i-1];CRequire(Math.Abs(x-p[0])+Math.Abs(y-p[1])+Math.Abs(v.ScaleFactor-p[2])<1e-9,"Approved view layout changed");}
  CRequire(cface.CuttingPlanes.Item(1).ViewSide==F.SectionViewProfileSide.igRightProfileSide&&BaseOriginConstraints.Ensure(cface.CuttingPlanes.Item(1),true)==1,"Cutting plane changed");
  CRequire(((S.Dimensions)csh.Dimensions).Count==0&&((S.Balloons)csh.Balloons).Count==0,"Expected no detailing before user review");
  CPreview();Console.WriteLine("PASS: shaded ISO, native section centerline and end-view center mark; saved styles and approved view layout preserved. Paused for review.");
 }
 static void DrawbarReviewViews(){
  DrawbarContext();LoadCap();cdoc.Close(false);Release(cdoc);LoadCap();
  CRequire(((S.Dimensions)csh.Dimensions).Count==0,"Saved drawing already contains dimensions; inspect before resetting.");
  var cp=cface.CuttingPlanes.Item(1);cp.ViewSide=F.SectionViewProfileSide.igRightProfileSide;
  csection.ForceUpdate();csection.Defaults_ShowHiddenEdges=false;SetHiddenMembers(csection.ModelMembers,false);csection.Update();
  foreach(D.DVLine2d l in csection.DVLines2d)if(l.Length>.09)l.ShowHideEdgeOverride=D.DVShowHideEdgeOverrideType.DVShowHideEdgeOverrideHide;
  csection.SetCaptionPosition(.15,.105);cp.SetCaptionPosition(true,.352,.127);cp.SetCaptionPosition(false,.352,.233);
  double x,y,sx,sy;cface.GetOrigin(out x,out y);csection.GetOrigin(out sx,out sy);CRequire(sx<x&&Math.Abs(y-sy)<1e-8,"Section alignment differs");
  CRequire(BaseOriginConstraints.Ensure(cp,true)==1,"Base constraint missing");
  cdoc.Save();cdoc.Close(false);Release(cdoc);LoadCap();
  CRequire(cface.CuttingPlanes.Item(1).ViewSide==F.SectionViewProfileSide.igRightProfileSide,"Cutting direction did not persist");
  CRequire(((S.Dimensions)csh.Dimensions).Count==0&&((S.Balloons)csh.Balloons).Count==0,"Expected views only");
  DrawbarDump();CPreview();Console.WriteLine("Saved views only for user review; cutting arrows point right and section remains aligned on the left.");
 }
}
