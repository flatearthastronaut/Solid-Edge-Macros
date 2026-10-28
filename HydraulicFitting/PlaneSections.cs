using System;
using System.IO;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;
using F=SolidEdge.Framework.Interop;
using Part=SolidEdge.Part.Interop;

// Reuses the SDK's STA/message-filter workflow. All sections retain their
// source view and model link; no drafting geometry substitutes for a section.
partial class BuildFitting {
 static string planeDraftPath;
 static void CreatePlaneDraft(){
  string partPath=Path.Combine(output,"BSPT-02 to SAE-04 ORB Adapter.par");
  planeDraftPath=Path.Combine(output,"BSPT-02 to SAE-04 ORB Adapter - Plane Sections.dft");
  doc=Keep((Part.PartDocument)app.Documents.Open(partPath));
  double[] frontNormal=null,rightNormal=null;
  for(int i=1;i<=3;i++){
   var plane=doc.RefPlanes.Item(i);Array normal=new double[3],origin=new double[3];plane.GetNormal(ref normal);plane.GetRootPoint(ref origin);
   Console.WriteLine("PLANE "+plane.Name+" "+plane.DisplayName+" "+plane.EdgebarName+" normal="+string.Join(",",(double[])normal));
   if(Math.Abs((double)normal.GetValue(1))>.99999)frontNormal=(double[])normal;
   if(Math.Abs((double)normal.GetValue(0))>.99999)rightNormal=(double[])normal;
   if(Math.Abs((double)origin.GetValue(0))+Math.Abs((double)origin.GetValue(1))+Math.Abs((double)origin.GetValue(2))>1e-8)throw new Exception("Reference planes are offset");
  }
  if(frontNormal==null||rightNormal==null||Math.Abs(frontNormal[2])>.00001||Math.Abs(rightNormal[2])>.00001)throw new Exception("Front and Right planes could not be resolved");
  bool existing=File.Exists(planeDraftPath);
  drawing=Keep(existing?(D.DraftDocument)app.Documents.Open(planeDraftPath):(D.DraftDocument)app.Documents.Add("SolidEdge.DraftDocument",Path.Combine(root,"tmp","company-normal.dft")));
  sheet=Keep(drawing.ActiveSheet);
  dynamic[] groups={sheet.Groups,sheet.DrawingViews,sheet.Dimensions,sheet.Balloons,sheet.TextBoxes,sheet.Leaders,sheet.CenterLines,sheet.CenterMarks,sheet.SurfaceFinishSymbols,sheet.BlockOccurrences,sheet.Symbols,sheet.Lines2d,sheet.Arcs2d,sheet.Circles2d};
  foreach(dynamic group in groups)for(int i=group.Count;i>=1;i--)group.Item(i).Delete();
  sheet.Name="PLANE SECTIONS";sheet.Background=drawing.Sheets.Item("B-Size");sheet.BackgroundVisible=true;sheet.SheetSetup.SheetSizeOption=D.PaperSizeConstants.igAnsiBWide;sheet.SheetSetup.SetScale(2.5,1);app.SetGlobalParameter(F.ApplicationGlobalConstants.seApplicationGlobalProjectionAngleDraft,(int)D.DrawingViewProjectionAngleConstants.ProjectionAngleThird);drawing.MaintainAlignmentSet=true;
  var link=Keep(drawing.ModelLinks.Count>0?drawing.ModelLinks.Item(1):drawing.ModelLinks.Add(partPath));
  CleanBackground();
  var top=View(link,D.ViewOrientationConstants.igTopView,2.5,5.2,8.3);top.SetViewOrientation(0,0,1,1,0,0);top.Update();Center(top,5.2,8.3);
  var front=PlaneSection(top,frontNormal,"A",5.2,5.25,false);
  var right=PlaneSection(front,rightNormal,"B",11.6,5.25,true);
  // Regeneration can update source placement. Set all three final positions
  // before attaching centerlines and save only to this new draft's path.
  Center(top,5.2,8.45);Center(front,5.2,5.35);Center(right,11.6,5.35);
  var selection=drawing.SelectSet;selection.RemoveAll();selection.Add(top);selection.Add(front);selection.Add(right);sheet.DrawingViews.Align();selection.RemoveAll();Center(top,5.2,8.45);Center(front,5.2,5.35);Center(right,11.6,5.35);
  SectionCenterline(front,frontNormal);SectionCenterline(right,rightNormal);
  D.DVCircle2d circle=null;
  foreach(D.DVCircle2d c in top.DVCircles2d)if(circle==null||c.Radius>circle.Radius)circle=c;
  if(circle==null)throw new Exception("Top view has no circular edge for center mark");
  var center=Map(top,P(0,0,Length));var mark=Keep(((S.CenterMarks)sheet.CenterMarks).AddByCenter(circle.Reference,center[0],center[1],0,true));
  mark.Style.DrivenColor=0;mark.Style.DrivingColor=0;mark.Style.CenterMarkSize=.10*Inch;mark.Style.CenterMarkExtension=.4*Inch;mark.Style.AutoCenterProjectionLine=true;mark.Style.LineWidth=.005*Inch;
  Text("TOP VIEW",6.8,8.45,2,.14);
  Text("SECTION A-A  /  FRONT PLANE",3.1,3.2,5,.13);
  Text("SECTION B-B  /  RIGHT PLANE",9.3,3.65,5,.13);
  Text("TOP VIEW AND SECTIONS THROUGH MODEL REFERENCE PLANES\nTHIRD-ANGLE PROJECTION  |  SCALE 2.5:1  |  DIMENSIONS IN INCHES",.8,1.25,9,.11);
  Text("NONE / ZINC PLATE",12.27,1.65,1.38,.11);
  Text("BSPT TO SAE ORB ADAPTER",13.74,2.17,2.5,.105);Text("1/8 BSPT x -4 SAE",13.74,2.02,2.5,.105);Text("HF-02BSPT-04ORB",14.68,.72,1.2,.085);Text("A",16.02,.72,.2,.10);drawing.UpdatePropertyTextCacheAndDisplay();
  if(existing)drawing.Save();else drawing.SaveAs(planeDraftPath);
  CheckPlaneDraft(frontNormal,rightNormal);
  drawing.Close(false);drawing=Keep((D.DraftDocument)app.Documents.Open(planeDraftPath));sheet=drawing.Sheets.Item("PLANE SECTIONS");
  CheckPlaneDraft(frontNormal,rightNormal);
  var moved=sheet.DrawingViews.Item(2);double ox,oy;moved.GetOrigin(out ox,out oy);try{moved.SetOrigin(ox+.08*Inch,oy+.08*Inch);app.DoIdle();CheckPlaneDraft(frontNormal,rightNormal);Console.WriteLine("PASS: moving Front section preserves view alignment");}finally{moved.SetOrigin(ox,oy);app.DoIdle();}CheckPlaneDraft(frontNormal,rightNormal);drawing.Save();PlanePreview();
  Console.WriteLine("SAVED "+planeDraftPath);
 }
 static D.DrawingView PlaneSection(D.DrawingView top,double[] normal,string caption,double x,double y,bool fromFront){
  // Plane intersects the XY top view along cross(normal, Z). Both lines pass
  // through the model origin, so these are exact principal-plane sections.
  double dx=normal[1],dy=-normal[0];
  var a=Map(top,fromFront?P(0,0,-.13):P(-.52*dx,-.52*dy,Length));var b=Map(top,fromFront?P(0,0,Length+.13):P(.52*dx,.52*dy,Length));
  var cp=Keep(top.CuttingPlanes.Add());cp.Caption=caption;cp.TextFont="Arial";cp.TextHeight=.14*Inch;cp.TextColor=0;cp.TerminatorRatio=.7;cp.LineWidth=.008*Inch;cp.LineDisplayStyle=S.CuttingPlaneLineDisplayStyleConstants.seThickCornersOnly;
  var profile=Keep(cp.Profile);profile.BeginEdit();var cutLine=Keep(profile.Lines2d.AddBy2Points(a[0],a[1],b[0],b[1]));AxisConstraints.Ensure(Keep(profile.Relations2d),cutLine);profile.EndEdit();
  Console.WriteLine("CUT "+caption+" "+profile.Validate());
  cp.ViewSide=fromFront?F.SectionViewProfileSide.igLeftProfileSide:F.SectionViewProfileSide.igRightProfileSide;
  var v=Keep(cp.CreateView(D.DraftSectionViewType.seDraftSectionViewTypeStandard));v.Sheet=sheet;v.ScaleFactor=2.5;v.Crop=false;v.CroppedVHL=false;v.LockView=false;v.SectionOnly=false;v.SectionFullModel=true;SetDisplay(v);v.Defaults_ShowSectionHatch=true;
  for(int i=1;i<=v.ModelMembers.Count;i++)v.ModelMembers.Item(i).DisplayType=D.ModelMemberDisplayTypeConstants.seSectionPart;
  v.ForceUpdate();var p=Map(v,P(0,0,0));var q=Map(v,P(0,0,1));v.SetRotationAngle(Math.PI/2-Math.Atan2(q[1]-p[1],q[0]-p[0]));v.Update();Center(v,x,y);
  // The section generator includes source cutting-plane extension graphics.
  // Suppress only lines extending beyond the actual part's projected bounds.
  var c0=Map(v,P(0,0,0));var c1=Map(v,P(0,0,Length));double left=c0[0]-AF/Math.Sqrt(3)*2.5*Inch,right=c0[0]+AF/Math.Sqrt(3)*2.5*Inch,cy=(c0[1]+c1[1])/2,rad=Length/2*2.5*Inch;
  foreach(D.DVLine2d line in v.DVLines2d){double ax,ay,bx,by;line.GetStartPoint(out ax,out ay);line.GetEndPoint(out bx,out by);v.ViewToSheet(ax,ay,out ax,out ay);v.ViewToSheet(bx,by,out bx,out by);if(Math.Min(ax,bx)<left-1e-6||Math.Max(ax,bx)>right+1e-6||Math.Min(ay,by)<cy-rad-1e-6||Math.Max(ay,by)>cy+rad+1e-6)line.ShowHideEdgeOverride=D.DVShowHideEdgeOverrideType.DVShowHideEdgeOverrideHide;}
  return v;
 }
 static void SectionCenterline(D.DrawingView v,double[] normal){
  var p=Point(v,P(0,0,0));var q=Point(v,P(0,0,Length));
  var cl=Keep(((S.CenterLines)sheet.CenterLines).AddByStartEnd(p.curve.Reference,p.x,p.y,0,true,q.curve.Reference,q.x,q.y,0,true));
  cl.Style.DrivenColor=0;cl.Style.DrivingColor=0;cl.Style.LineWidth=.005*Inch;
 }
 static void CheckPlaneDraft(double[] fn,double[] rn){
  if(drawing.ModelLinks.Count!=1||sheet.DrawingViews.Count!=3||((S.CenterLines)sheet.CenterLines).Count!=2||((S.CenterMarks)sheet.CenterMarks).Count!=1)throw new Exception("Unexpected view or centerline count");
  var top=sheet.DrawingViews.Item(1);if(top.CuttingPlanes.Count!=1||sheet.DrawingViews.Item(2).CuttingPlanes.Count!=1)throw new Exception("Expected two cutting planes");
  double[][] normals={fn,rn};
  for(int i=0;i<2;i++){var v=sheet.DrawingViews.Item(i+2);double nx,ny,nz,xx,xy,xz;D.ViewOrientationConstants orientation;v.ViewOrientation(out nx,out ny,out nz,out xx,out xy,out xz,out orientation);double dot=nx*normals[i][0]+ny*normals[i][1]+nz*normals[i][2];if(Math.Abs(Math.Abs(dot)-1)>1e-6)throw new Exception("Section is not normal to requested model plane");var z0=Map(v,P(0,0,0));var z1=Map(v,P(0,0,1));Console.WriteLine("SECTION "+(i+1)+" plane normal "+nx+","+ny+","+nz+" Z delta="+(z1[0]-z0[0])+","+(z1[1]-z0[1]));}
  var tc=Map(top,P(0,0,Length/2));var fc=Map(sheet.DrawingViews.Item(2),P(0,0,Length/2));var rc=Map(sheet.DrawingViews.Item(3),P(0,0,Length/2));Console.WriteLine("CENTERS top="+tc[0]/Inch+","+tc[1]/Inch+" front="+fc[0]/Inch+","+fc[1]/Inch+" right="+rc[0]/Inch+","+rc[1]/Inch);if(Math.Abs(tc[0]-fc[0])>1e-6||Math.Abs(fc[1]-rc[1])>1e-6||tc[1]<=fc[1]||rc[0]<=fc[0])throw new Exception("Third-angle view alignment failed");
  Console.WriteLine("PASS: top + Front and Right plane sections; associated section centerlines and top center mark");
 }
 static void PlanePreview(){sheet.Activate();app.DoIdle();var w=(D.SheetWindow)app.ActiveWindow;w.FitEx(D.SheetFitConstants.igFitSheet);w.SaveAsImage(Path.Combine(output,"Adapter-plane-sections.jpg"),3060,1980,1,24,F.SeImageQualityType.seImageQualityHigh,false);}
}






