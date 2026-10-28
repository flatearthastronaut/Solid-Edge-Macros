using System;
using System.IO;
using System.Collections.Generic;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;
using F=SolidEdge.Framework.Interop;

partial class BuildFitting {
 static D.DraftDocument drawing;static D.Sheet sheet;static S.Dimensions dims;
 class DP {public dynamic curve;public double x,y;}
 static void CreateDrawing(){
  string path=Path.Combine(output,"BSPT-02 to SAE-04 ORB Adapter.dft");
  bool existing=File.Exists(path);
  drawing=Keep(existing ? (D.DraftDocument)app.Documents.Open(path) : (D.DraftDocument)app.Documents.Add("SolidEdge.DraftDocument",Path.Combine(root,"tmp","company-normal.dft")));
  sheet=Keep(drawing.ActiveSheet);dynamic[] items={sheet.Groups,sheet.DrawingViews,sheet.Dimensions,sheet.Balloons,sheet.TextBoxes,sheet.Leaders,sheet.CenterLines,sheet.CenterMarks,sheet.SurfaceFinishSymbols,sheet.BlockOccurrences,sheet.Symbols,sheet.Lines2d,sheet.Arcs2d,sheet.Circles2d};foreach(dynamic group in items)for(int j=group.Count;j>=1;j--){dynamic obj=group.Item(j);obj.Delete();}sheet.Name="FITTING";sheet.Background=drawing.Sheets.Item("B-Size");sheet.BackgroundVisible=true;
  sheet.SheetSetup.SheetSizeOption=D.PaperSizeConstants.igAnsiBWide;sheet.SheetSetup.SetScale(4,1);
  CleanBackground();foreach(dynamic st in (System.Collections.IEnumerable)drawing.DimensionStyles){st.PrimaryUnits=S.DimLinearUnitConstants.igDimStyleLinearInches;st.PrimaryDecimalRoundOff=S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_3;st.PrimaryLeadingZeros=false;st.PrimaryTrailingZeros=true;st.DualDisplay=false;}
  dims=Keep((S.Dimensions)sheet.Dimensions);dims.AxisMode=S.DimAxisModeConstants.igDimAxisModeDefault;
  var link=Keep(drawing.ModelLinks.Count>0?drawing.ModelLinks.Item(1):drawing.ModelLinks.Add(Path.Combine(output,"BSPT-02 to SAE-04 ORB Adapter.par")));
  var front=View(link,D.ViewOrientationConstants.igRightView,4,5.2,7.5);front.SetViewOrientation(-1,0,0,0,0,1);front.Update();Center(front,5.2,7.5);
  var end=View(link,D.ViewOrientationConstants.igTopView,4,12.4,7.5);end.SetViewOrientation(0,0,1,1,0,0);end.Update();Center(end,12.4,7.5);
  var cp=Keep(end.CuttingPlanes.Add());cp.Caption="A";cp.TextHeight=.12*Inch;cp.TextColor=0;
  var p0=Map(end,P(0,-.41,Length));var p1=Map(end,P(0,.41,Length));
  var profile=Keep(cp.Profile);profile.BeginEdit();var cutLine=Keep(profile.Lines2d.AddBy2Points(p0[0],p0[1],p1[0],p1[1]));AxisConstraints.Ensure(Keep(profile.Relations2d),cutLine);profile.EndEdit();BaseOriginConstraints.Ensure(cp);Console.WriteLine("Section profile: "+profile.Validate());
  cp.ViewSide=F.SectionViewProfileSide.igLeftProfileSide;
  var section=Keep(cp.CreateView(D.DraftSectionViewType.seDraftSectionViewTypeStandard));section.Sheet=sheet;section.ScaleFactor=4;section.Crop=false;section.CroppedVHL=false;section.LockView=false;section.SectionOnly=false;
  SetDisplay(section);section.Defaults_ShowSectionHatch=true;for(int i=1;i<=section.ModelMembers.Count;i++)section.ModelMembers.Item(i).DisplayType=D.ModelMemberDisplayTypeConstants.seSectionPart;
  section.ForceUpdate();foreach(D.DVLine2d l in section.DVLines2d){if(l.Length>.80*Inch)l.ShowHideEdgeOverride=D.DVShowHideEdgeOverrideType.DVShowHideEdgeOverrideHide;}var ma=Map(section,P(0,0,0));var mb=Map(section,P(0,0,1));section.SetRotationAngle(-Math.Atan2(mb[1]-ma[1],mb[0]-ma[0]));section.Update();Center(section,5.2,3.8);
  var iso=View(link,D.ViewOrientationConstants.igTopFrontRightView,2.2,13.4,4.65);
  front.SetViewOrientation(-1,0,0,0,0,1);front.Update();Center(front,5.2,7.5);Center(end,12.4,7.5);
  Text("SECTION A-A",4.45,1.97,3,.14);Text("ISOMETRIC\n2.2:1",14.55,4.6,1.6,.11);
  if(existing)drawing.Save();else drawing.SaveAs(path);Preview();Dump(front,"front");Dump(end,"end");Dump(section,"section");
  Linear(front,P(0,MaleRadius(.020)-.020,0),P(0,AF/2-.020,Length),5.2,9.55,Length);
  Linear(front,P(0,MaleRadius(.020)-.020,0),P(0,AF/2,Shoulder),3.82,9.1,Shoulder);
  var hex=Linear(end,P(AF/(2*Math.Sqrt(3)),-AF/2,.5),P(AF/(2*Math.Sqrt(3)),AF/2,.5),14.7,7.5,AF,true);hex.SuffixString=" HEX";hex.Style.PrimaryDecimalRoundOff=S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_4;
  Callout(front,P(0,-MaleRadius(.20),.20),"R1/8-28 BSPT%RTISO 7-1",.9,6.4);
  Callout(section,P(0,TapDrill/2,.78),"%TS-2B%RT%ZT%RTSAE J1926-1, -4 PORT",8.4,5.6);
  Callout(section,P(0,Passage/2,.2),"%DI %HS THRU",.9,3.0);
  Linear(section,P(0,TapDrill/2,Length-TapDepth),P(0,PortU/2,Length),6.25,5.65,TapDepth);
  Linear(section,P(0,-PortU/2,Length),P(0,PortU/2,Length),8.1,3.8,PortU,true,"%DI ");
  Chamfer(front,P(0,(AF/2-.020+Passage/2)/2,Length),P(0,AF/2-.010,Length-.010),8.0,8.9,.020);
  Chamfer(front,P(0,(MaleRadius(.020)-.020+Passage/2)/2,0),P(0,MaleRadius(.020)-.010,.010),1.2,6.2,.020);
  Text("CATALOG BASIS: PARKER 1/8-4F3HG5. ENVELOPE: 11/16 HEX x 1.09 LONG.\nUNDIMENSIONED CATALOG DETAILS RECONSTRUCTED. THREAD HELICES OMITTED.\nSAE PORT ENTRANCE: 12° ±1°, .093 +.015/-.000 DEEP, R.004-.008 AT MOUTH.\nSEALING SURFACE 100 µin Ra MAX. REMOVE BURRS; KEEP SEALING EDGES SMOOTH.\nDESIGN REVIEW REQUIRED BEFORE MANUFACTURE; PRESSURE RATING NOT ASSIGNED.",.7,1.65,10.4,.105);
  drawing.UpdatePropertyTextCacheAndDisplay();foreach(S.Balloon b in (S.Balloons)sheet.Balloons)Console.WriteLine("CALLOUT: "+b.BalloonDisplayedText);
  foreach(S.Dimension dim in dims){if(dim.DimensionType==S.DimTypeConstants.igDimTypeChamfer){Console.WriteLine("CHAMFER KEYS "+dim.KeyPointCount);for(int k=0;k<dim.KeyPointCount;k++){double x,y,z;F.KeyPointType kt;int kh;dim.GetKeyPoint(k,out x,out y,out z,out kt,out kh);Console.WriteLine(k+" "+x/Inch+","+y/Inch+" "+kt);}}}
  Text("NONE / ZINC PLATE",12.27,1.65,1.38,.11);ReferenceAnnotationStyle.Document(drawing);drawing.Save();Preview();Console.WriteLine("DRAFT SAVED "+path);
 }
 static void CleanBackground(){
  for(int ti=drawing.Tables.Count;ti>=1;ti--){var table=drawing.Tables.Item(ti);bool remove=false;foreach(D.TableTitle title in table.Titles)if((title.value??"").Contains("INSTALL THE FOLLOWING ITEMS"))remove=true;if(remove)table.Delete();}var bg=sheet.Background;using(var writer=new StreamWriter(Path.Combine(root,"tmp","background.txt"))){foreach(S.TextBox t in (S.TextBoxes)bg.TextBoxes){double a,b,c,d;t.Range(out a,out b,out c,out d);writer.WriteLine("TEXT "+a/Inch+","+b/Inch+" "+c/Inch+","+d/Inch+" "+t.Text);}foreach(S.Balloon t in (S.Balloons)bg.Balloons){double a,b,c,d;t.Range(out a,out b,out c,out d);writer.WriteLine("BALLOON "+a/Inch+","+b/Inch+" "+c/Inch+","+d/Inch+" "+t.BalloonText);}foreach(S.Line2d t in bg.Lines2d){double a,b,c,d;t.Range(out a,out b,out c,out d);writer.WriteLine("LINE "+a/Inch+","+b/Inch+" "+c/Inch+","+d/Inch);}}dynamic[] groups={bg.Groups,bg.BlockOccurrences,bg.Symbols,bg.TextBoxes,bg.Balloons,bg.Lines2d,bg.SurfaceFinishSymbols,bg.Leaders};
  foreach(dynamic group in groups)for(int i=group.Count;i>=1;i--){dynamic obj=group.Item(i);try{double a,b,c,d;obj.Range(out a,out b,out c,out d);if(c<10.5*Inch&&d<2.6*Inch&&b>.51*Inch)obj.Delete();}catch{}}
  foreach(S.Balloon b in (S.Balloons)bg.Balloons){if((b.BalloonText??"").ToLowerInvariant().Contains("matl|r1")){b.BalloonText="12L14";b.Style.Height=.11*Inch;}if((b.BalloonDisplayedText??"").Contains("Error: No reference"))b.BalloonText="";b.Style.DrivenColor=0;b.Style.DrivingColor=0;}
 } static D.DrawingView View(D.ModelLink link,D.ViewOrientationConstants orientation,double scale,double x,double y){var v=Keep(sheet.DrawingViews.AddPartView(link,orientation,scale,x*Inch,y*Inch,D.PartDrawingViewTypeConstants.sePartDesignedView));SetDisplay(v);Center(v,x,y);return v;}
 static void SetDisplay(D.DrawingView v){v.DisplayCaption=false;v.DisplayScale=false;v.Defaults_ShowHiddenEdges=false;v.Defaults_ShowTangentEdges=true;for(int i=1;i<=v.ModelMembers.Count;i++){var m=v.ModelMembers.Item(i);m.ShowHiddenEdges=false;m.ShowTangentEdges=true;}v.Update();}
 static void Center(D.DrawingView v,double x,double y){double ox,oy;var mid=Map(v,P(0,0,Length/2));v.GetOrigin(out ox,out oy);v.SetOrigin(ox+x*Inch-mid[0],oy+y*Inch-mid[1]);v.Update();}
 static double[] Map(D.DrawingView v,double[] p){double x,y;v.ModelToView(p[0]*Inch,p[1]*Inch,p[2]*Inch,out x,out y);v.ViewToSheet(x,y,out x,out y);return new[]{x,y};}
 static List<DP> Points(D.DrawingView v){var list=new List<DP>();dynamic[] groups={v.DVLines2d,v.DVArcs2d,v.DVCircles2d,v.DVBSplineCurves2d};foreach(dynamic group in groups)for(int i=1;i<=group.Count;i++){dynamic c=Keep((object)group.Item(i));for(int k=0;k<c.KeyPointCount;k++)try{double x,y,z;F.KeyPointType type;int handle;c.GetKeyPoint(k,out x,out y,out z,out type,out handle);v.ViewToSheet(x,y,out x,out y);list.Add(new DP{curve=c,x=x,y=y});}catch{}}return list;}
 static DP Point(D.DrawingView v,double[] model){var target=Map(v,model);DP best=null;double min=1;foreach(var p in Points(v)){double d=Math.Pow(p.x-target[0],2)+Math.Pow(p.y-target[1],2);if(d<min){min=d;best=p;}}if(min>1e-10)throw new Exception("Missing model keypoint "+string.Join(",",model)+" in "+v.Name+" gap="+Math.Sqrt(min)/Inch);return best;}
 static D.DVLine2d Edge(D.DrawingView v,double[] model){var target=Map(v,model);foreach(D.DVLine2d l in v.DVLines2d){double ax,ay,bx,by;l.GetStartPoint(out ax,out ay);l.GetEndPoint(out bx,out by);v.ViewToSheet(ax,ay,out ax,out ay);v.ViewToSheet(bx,by,out bx,out by);double dx=bx-ax,dy=by-ay,t=((target[0]-ax)*dx+(target[1]-ay)*dy)/(dx*dx+dy*dy);if(t>=-.00001&&t<=1.00001&&Math.Abs(ax+t*dx-target[0])+Math.Abs(ay+t*dy-target[1])<1e-6)return Keep(l);}throw new Exception("No edge at "+string.Join(",",model)+" in "+v.Name);}
 static void Style(dynamic s){
  ReferenceDimensionStyle.Apply(s);
  // The fitting is an inch drawing; retain its required dimensional precision.
  s.PrimaryUnits=S.DimLinearUnitConstants.igDimStyleLinearInches;
  s.PrimaryDecimalRoundOff=S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_3;
  s.DualDisplay=false;
 }

 static S.Dimension Linear(D.DrawingView v,double[] a,double[] b,double tx,double ty,double expected,bool vertical=false,string prefix=""){
  var p=Point(v,a);var q=Point(v,b);var d=Keep(dims.AddDistanceBetweenObjects(p.curve.Reference,p.x,p.y,0,true,q.curve.Reference,q.x,q.y,0,true));d.MeasurementAxisEx=S.DimAxisModeConstants.igDimAxisModeDefault;d.MeasurementAxisDirection=vertical;Style(d.Style);d.PrefixString=prefix;d.TrackDistance=vertical?tx*Inch-p.x:p.y-ty*Inch;d.SetKeyPoint(8,tx*Inch,ty*Inch,0);Console.WriteLine("DIM "+expected+" measured "+d.Value/Inch);if(Math.Abs(d.Value/Inch-expected)>1e-5)throw new Exception("Dimension mismatch");return d;
 }
 static void Chamfer(D.DrawingView v,double[] adjacent,double[] chamfer,double x,double y,double expected){var a=Edge(v,adjacent);var c=Edge(v,chamfer);var ap=Map(v,adjacent);var cp=Map(v,chamfer);var d=Keep(dims.AddChamfer(a.Reference,ap[0],ap[1],0,false,c.Reference,cp[0],cp[1],0,false,S.DimChamferModeConstants.igDimChamferModePerpendicular));Style(d.Style);d.Style.ChamferUse45DegreeCharacter=false;d.TrackDistance=.5*Inch;d.SetKeyPoint(0,x*Inch,y*Inch,0);Console.WriteLine("CHAMFER "+d.Value/Inch+" angle="+d.SecondValue*180/Math.PI);if(Math.Abs(d.Value/Inch-expected)>1e-5||Math.Abs(d.SecondValue-Math.PI/4)>1e-5)throw new Exception("Chamfer mismatch");}
 static void Callout(D.DrawingView v,double[] at,string text,double x,double y){var edge=Edge(v,at);var p=Map(v,at);var b=Keep(((S.Balloons)sheet.Balloons).Add(x*Inch,y*Inch,0));b.Callout=1;b.Leader=true;b.BreakLine=true;b.BreakLineDistance=.12*Inch;b.SetTerminator(edge.Reference,p[0],p[1],0,true);var bs=Keep(b.Style);bs.Font="Arial";bs.Height=.13*Inch;bs.DrivenColor=0;bs.DrivingColor=0;b.Style.SmartDepthThreadFinite="%TD DP.";b.Style.SmartDepthThreadThru="THRU";b.BalloonText=text;b.CalloutHorizontalAlignment=S.TextJustificationConstants.igTextJustifyLeft;double cx,cy,cz,tx,ty,tz;F.KeyPointType kind;int handle;b.GetKeyPoint(2,out cx,out cy,out cz,out kind,out handle);b.GetKeyPoint(4,out tx,out ty,out tz,out kind,out handle);b.SetKeyPoint(2,x*Inch+Math.Abs(cx-tx),y*Inch+cy-ty,0);drawing.UpdatePropertyTextCacheAndDisplay();Console.WriteLine("CALLOUT "+b.BalloonDisplayedText);}
 static void Text(string text,double x,double y,double width,double size){var b=Keep(((S.TextBoxes)sheet.TextBoxes).Add(x*Inch,y*Inch,0));b.Width=width*Inch;b.Text=text;b.Edit.Font="Arial";b.Edit.TextSize=size*Inch;}
 static void Dump(D.DrawingView v,string name){using(var w=new StreamWriter(Path.Combine(root,"tmp",name+"-geometry.txt"))){foreach(D.DVLine2d l in v.DVLines2d){double x,y,u,t;l.GetStartPoint(out x,out y);l.GetEndPoint(out u,out t);v.ViewToSheet(x,y,out x,out y);v.ViewToSheet(u,t,out u,out t);w.WriteLine("LINE "+x/Inch+","+y/Inch+" -> "+u/Inch+","+t/Inch);}}}
 static void Preview(){sheet.Activate();app.DoIdle();var w=(D.SheetWindow)app.ActiveWindow;w.FitEx(D.SheetFitConstants.igFitSheet);w.SaveAsImage(Path.Combine(output,"Adapter-drawing.jpg"),3060,1980,1,24,F.SeImageQualityType.seImageQualityHigh,false);}
}

















