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
 const string Bushing21931Name="bushing 21931";
 static void Bushing21931Context(){capName=Bushing21931Name;}
 static void Bushing21931Spacing(){
  Bushing21931Context();app.Interactive=true;app.DelayCompute=false;LoadCap();var dims=(S.Dimensions)csh.Dimensions;CRequire(dims.Count==4,"Expected completed dimensional stage");
  // A reopened document can retain read-only access even when the file has
  // no read-only attribute. Request normal write access once; fail before any
  // geometry or annotation changes if Solid Edge cannot grant it.
  if(cdoc.ReadOnly){bool available=false;cdoc.SeekWriteAccess(out available);CRequire(available&&!cdoc.ReadOnly,"Solid Edge could not grant write access; ask Greg before further recovery attempts.");}
  var before=dims.Cast<S.Dimension>().Select(EngineeringFingerprint).OrderBy(s=>s).ToArray();
  // At 1:1 the 7.112 mm bore leaves less room than the dual-unit text needs.
  // The standing rule permits enlarging views; 2:1 also matches 21930-04.
  cface.ScaleDimensionsAndAnnotations=false;csection.ScaleDimensionsAndAnnotations=false;ciso.ScaleDimensionsAndAnnotations=false;
  Console.WriteLine("Initial scales: end="+cface.ScaleFactor+" section="+csection.ScaleFactor+" iso="+ciso.ScaleFactor+"; locked="+cface.LockView+","+csection.LockView+" readonly="+cdoc.ReadOnly);
  if(cface.ScaleFactor!=2){Console.WriteLine("Scaling end view");cface.ScaleFactor=2;}
  if(csection.ScaleFactor!=2){Console.WriteLine("Scaling section view");csection.ScaleFactor=2;}
  Console.WriteLine("Updating scaled views");cface.Update();csection.Update();BushingPlaceView(cface,.285,.145);BushingPlaceView(csection,.150,.145);
  foreach(S.Dimension dim in dims){switch(dim.GetComment()){
   case "BUSHING21931-OD":dim.SetKeyPoint(8,.085,.145,0);break;
   case "BUSHING21931-ID":dim.SetKeyPoint(8,.111,.145,0);break;
   case "BUSHING21931-LENGTH":dim.SetKeyPoint(8,.150,.190,0);break;
   case "BUSHING21931-CHAMFER":PlaceChamferText(dim,.202,.149);break;
  }}
  var cp=cface.CuttingPlanes.Item(1);var line=cp.Profile.Lines2d.Item(1);try{double a,b,c,d;line.GetStartPoint(out a,out b);line.GetEndPoint(out c,out d);cp.SetCaptionPosition(true,a+.025,b);cp.SetCaptionPosition(false,c+.025,d);CRequire(AxisConstraints.Has(line,(int)F.ObjectType.igVerticalRelation2d),"Cutting-plane constraint changed during scale adjustment");}finally{Release(line);Release(cp);}
  cdoc.Activate();csh.Activate();cdoc.UpdatePropertyTextCacheAndDisplay();app.DoIdle();
  cdoc.Save();cdoc.Close(false);Release(cdoc);LoadCap();dims=(S.Dimensions)csh.Dimensions;
  CRequire(before.SequenceEqual(dims.Cast<S.Dimension>().Select(EngineeringFingerprint).OrderBy(s=>s)),"Scale adjustment changed dimensional content");
  var aCenter=CMap(cface,0,.06731,.0534543);var bCenter=CMap(csection,0,.06731,.0534543);CRequire(Math.Abs(aCenter[0]-.285)+Math.Abs(bCenter[0]-.150)+Math.Abs(aCenter[1]-.145)+Math.Abs(bCenter[1]-.145)<1e-8,"Approved view centers or alignment changed");
  CRequire(cface.ScaleFactor==2&&csection.ScaleFactor==2&&ciso.ScaleFactor==1&&ciso.Shading&&csh.SheetSetup.SheetSizeOption==D.PaperSizeConstants.igAnsiBWide,"Scale or sheet configuration differs");
  var report=new XElement("BushingMainDimensionVerification",new XAttribute("reference","21930-04 bushing.dft"),new XAttribute("diameterInchPlaces",2),new XAttribute("lengthToleranceInch",.001),new XAttribute("orthographicScale","2:1"),new XAttribute("sheet","B"));
  foreach(S.Dimension dim in dims){CRequire(dim.StatusOfDimension==S.DimStatusConstants.seDimStatusDriven&&string.IsNullOrEmpty(dim.OverrideString),"Scaled dimension lost model link");if(dim.DimensionType==S.DimTypeConstants.igDimTypeSDiameter){var edge=BushingBodyEdge(.06731+dim.Value/2,.0534543);try{BushingDiameterParents(dim,edge);}finally{Release(edge);}}
   var item=Scalars(dim,"FrameworkSupport","Dimension","Dimension");item.Add(CTexts(dim).Select(t=>new XElement("DisplayedText",t)));report.Add(item);
  }
  new XDocument(report).Save(Path.Combine(CapWork,"main-dimension-verification.xml"));CPreview();Console.WriteLine("PASS: 2:1 end/section views retain native alignment and exact dimension content; shaded ISO 1:1; B sheet unchanged.");
 }
 static void Bushing21931DimensionInit(){
  Bushing21931Context();LoadCap();var dims=(S.Dimensions)csh.Dimensions;var axis=((S.CenterLines)csh.CenterLines).Item(1);var edge=BushingBodyEdge(.0752475,.0534543);var p=BushingSegment(edge);int j=p[0]<p[2]?0:2;
  try{foreach(bool normal in new[]{false,true}){var init=dims.DimInitData;S.Dimension dim=null;try{init.ClearCreationData();init.SetType(S.DimTypeConstants.igDimTypeSDiameter);init.SetAxisMode(S.DimAxisModeConstants.igDimAxisModeDefault);init.SetNormalDiameterMode(normal);init.SetNumberOfParents(2);init.SetParentByIndex(0,axis,false,false,false,false,p[j],.145,0);init.SetParentByIndex(1,edge.Reference,true,false,false,false,p[j],p[j+1],0);init.SetDimPositionMode(true);init.SetDimPosition(.098,.145,0);dim=dims.AddDimension(init);dim.DisplayHalf=true;dim.MeasurementAxisDirection=true;Console.WriteLine("INIT normal="+normal+" value="+dim.Value+" keys="+dim.KeyPointCount+" axis="+dim.MeasurementAxisEx);}catch(Exception ex){Console.WriteLine("INIT normal="+normal+" "+ex.Message);}finally{if(dim!=null){dim.Delete();Release(dim);}Release(init);}}}finally{Release(edge);Release(axis);}
 }
 static void Bushing21931ReferenceParents(){
  string path=Path.Combine(folder,"21930-04 bushing.dft");bool open=IsOpen(path);var doc=(D.DraftDocument)app.Documents.Open(path);
  try{foreach(S.Dimension dim in (S.Dimensions)doc.Sheets.Item("Sheet1").Dimensions){if(dim.DimensionType!=S.DimTypeConstants.igDimTypeSDiameter)continue;int count;dim.GetRelatedCount(out count);Console.WriteLine("REF DIM "+dim.Value+" parents="+count);
   for(int i=0;i<count;i++){object related=null,target=null;double x,y,z;bool key;try{dim.GetRelated(i,out related,out x,out y,out z,out key);var reference=related as F.Reference;target=reference==null?related:reference.Object;Console.WriteLine("PARENT "+i+" ref="+(reference!=null)+" key="+key+" pick="+x+","+y+","+z+" DVLine="+(target is D.DVLine2d)+" CenterLine="+(target is S.CenterLine)+" Line2d="+(target is S.Line2d)+" DVCircle="+(target is D.DVCircle2d));var line=target as D.DVLine2d;if(line!=null){double a,b,c,d;line.GetStartPoint(out a,out b);line.GetEndPoint(out c,out d);Console.WriteLine("LINE "+a+","+b+" / "+c+","+d+" member="+line.ModelMember.ComponentName);}}finally{if(!object.ReferenceEquals(target,related))Release(target);Release(related);}}
  }}finally{if(!open)doc.Close(false);Release(doc);}
 }
 static D.DVLine2d BushingBodyEdge(double y,double z){
  var point=CMap(csection,0,y,z);D.DVLine2d selected=null;
  foreach(D.DVLine2d edge in csection.DVLines2d){bool keep=false;try{
   var member=edge.ModelMember;bool physical;try{physical=member.ComponentType==D.ModelMemberComponentTypeConstants.seSolidBodyMemberType&&member.DisplayType!=D.ModelMemberDisplayTypeConstants.seHidePart;}finally{Release(member);}
   if(!physical||edge.ShowHideEdgeOverride==D.DVShowHideEdgeOverrideType.DVShowHideEdgeOverrideHide)continue;
   var p=BushingSegment(edge);double dx=p[2]-p[0],dy=p[3]-p[1],l=dx*dx+dy*dy;if(l<1e-18)continue;
   double t=Math.Max(0,Math.Min(1,((point[0]-p[0])*dx+(point[1]-p[1])*dy)/l));
   if(Math.Abs(p[0]+t*dx-point[0])+Math.Abs(p[1]+t*dy-point[1])<1e-8){CRequire(selected==null,"Ambiguous physical dimension edge");selected=edge;keep=true;}
  }finally{if(!keep)Release(edge);}}
  CRequire(selected!=null,"Physical model edge not found for dimension");return selected;
 }
 static S.Dimension BushingSizeDimension(bool diameter,double radius,double x,double y,string role){
  D.DVLine2d a,b;double[] p,q;
  if(diameter){
   a=BushingBodyEdge(.06731-radius,.0534543);b=BushingBodyEdge(.06731+radius,.0534543);
   // Use each silhouette's left endpoint so the extension gaps start at the
   // nearest physical feature, not at the middle of the section or a bevel.
   var pa=BushingSegment(a);var pb=BushingSegment(b);int ia=pa[0]<pa[2]?0:2,ib=pb[0]<pb[2]?0:2;p=new[]{pa[ia],pa[ia+1]};q=new[]{pb[ib],pb[ib+1]};
  }else{
   a=BushingBodyEdge(.07305675,.0423418);b=BushingBodyEdge(.07305675,.0645668);
   p=CMap(csection,0,.0752475,.0423418);q=CMap(csection,0,.0752475,.0645668);
  }
  try{
   var dims=(S.Dimensions)csh.Dimensions;
   var axis=((S.CenterLines)csh.CenterLines).Item(1);S.Dimension dim;
   try{
    if(diameter){
     // The legacy AddSymmetricalDiameter overload forces an explicit axis
     // for a CenterLine parent. Initialize the default axis explicitly to
     // reproduce the reference's nine-keypoint, two-sided native dimension.
     var init=dims.DimInitData;try{init.ClearCreationData();init.SetType(S.DimTypeConstants.igDimTypeSDiameter);init.SetAxisMode(S.DimAxisModeConstants.igDimAxisModeDefault);init.SetNormalDiameterMode(false);init.SetNumberOfParents(2);init.SetParentByIndex(0,axis,false,false,false,false,p[0],(p[1]+q[1])/2,0);init.SetParentByIndex(1,b.Reference,true,false,false,false,q[0],q[1],0);init.SetDimPositionMode(true);init.SetDimPosition(x,y,0);dim=dims.AddDimension(init);}finally{Release(init);}
    }else dim=dims.AddDistanceBetweenObjects(a.Reference,p[0],p[1],0,true,b.Reference,q[0],q[1],0,true);
   }finally{Release(axis);}
   try{
    var style=dim.Style;try{CStyle(style);style.PrimaryDecimalRoundOff=diameter?S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_1:S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_2;style.SecondaryDecimalRoundOff=diameter?S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_2:S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_3;
     if(!diameter){style.PrimaryLinearToleranceUnits=S.DimLinearUnitConstants.igDimStyleLinearInches;dim.DisplayType=S.DimDispTypeConstants.igDimDisplayTypeUnitTolerance;dim.PrimaryUpperTolerance=".001";dim.PrimaryLowerTolerance="-.001";dim.DisplayType=S.DimDispTypeConstants.igDimDisplayTypeLimits;}
     // The user explicitly selected .63 for the .625 OD. RoundUpOdd from the
     // general reference would produce .62 at this tie; use native half-up
     // rounding for this OD only, without changing or overriding its value.
     if(role=="BUSHING21931-OD")style.RoundUp=S.DimRoundUpTypeConstants.igDimStyleRoundUpAll;
    }finally{Release(style);}
    dim.SetComment(role);if(diameter)dim.DisplayHalf=true;else dim.MeasurementAxisEx=S.DimAxisModeConstants.igDimAxisModeDefault;dim.MeasurementAxisDirection=diameter;
    // Native symmetric diameters measure twice the distance from the linked
    // centerline to a silhouette; the vertical axis excludes endpoint X offsets.
    dim.TrackDistance=diameter?x-p[0]:p[1]-y;dim.SetKeyPoint(8,x,y,0);
    CRequire(Math.Abs(dim.Value-(diameter?2*radius:.022225))<1e-9&&dim.StatusOfDimension==S.DimStatusConstants.seDimStatusDriven,"Native dimension differs from model: "+role+" measured="+dim.Value+" status="+dim.StatusOfDimension);if(diameter)BushingDiameterParents(dim,b);else VerifyParents(dim,new object[]{a,b});return dim;
   }catch{dim.Delete();Release(dim);throw;}
  }finally{Release(a);Release(b);}
 }
 static void BushingDiameterParents(S.Dimension dim,D.DVLine2d expected){
  int count;dim.GetRelatedCount(out count);CRequire(count==2,"Symmetric diameter must retain centerline and physical edge");int axes=0,edges=0;
  var axis=((S.CenterLines)csh.CenterLines).Item(1);
  try{for(int i=0;i<count;i++){object related=null,target=null;double x,y,z;bool key;
   try{dim.GetRelated(i,out related,out x,out y,out z,out key);var reference=related as F.Reference;target=reference==null?related:reference.Object;
    var line=target as D.DVLine2d;var center=target as S.CenterLine;
    if(line!=null){CRequire(NativeEdgeKey(line)==NativeEdgeKey(expected),"Symmetric diameter attached to wrong physical edge");edges++;}
    else{CRequire(center!=null&&SameNativeObject(center,axis),"Symmetric diameter lost native centerline association");axes++;}
   }finally{if(!object.ReferenceEquals(target,related))Release(target);Release(related);}
  }}finally{Release(axis);}CRequire(axes==1&&edges==1,"Incorrect symmetric diameter parent roles");
 }
 static void Bushing21931DimensionLayout(){
  Bushing21931Context();LoadCap();var dims=(S.Dimensions)csh.Dimensions;CRequire(dims.Count==4,"Expected four dimensions");
  var before=dims.Cast<S.Dimension>().Select(EngineeringFingerprint).OrderBy(s=>s).ToArray();
  var oldDiameters=dims.Cast<S.Dimension>().Where(d=>d.DimensionType==S.DimTypeConstants.igDimTypeSDiameter).ToArray();var replacements=new System.Collections.Generic.List<S.Dimension>();
  try{foreach(S.Dimension old in oldDiameters){bool outer=old.GetComment()=="BUSHING21931-OD";var fresh=BushingSizeDimension(true,old.Value/2,outer?.098:.123,.145,old.GetComment());replacements.Add(fresh);CRequire(fresh.KeyPointCount==9&&EngineeringFingerprint(old)==EngineeringFingerprint(fresh),"Reference-style replacement changed dimension content");}
   foreach(var old in oldDiameters)old.Delete();
  }catch{foreach(var fresh in replacements)fresh.Delete();throw;}finally{foreach(var fresh in replacements)Release(fresh);foreach(var old in oldDiameters)Release(old);}
  cdoc.Save();cdoc.Close(false);Release(cdoc);LoadCap();dims=(S.Dimensions)csh.Dimensions;
  CRequire(before.SequenceEqual(dims.Cast<S.Dimension>().Select(EngineeringFingerprint).OrderBy(s=>s)),"Dimension content changed during full-display placement");
  var report=new XElement("BushingMainDimensionVerification",new XAttribute("reference","21930-04 bushing.dft"),new XAttribute("diameterInchPlaces",2),new XAttribute("lengthToleranceInch",.001));
  foreach(S.Dimension dim in dims){if(dim.DimensionType==S.DimTypeConstants.igDimTypeSDiameter){CRequire(dim.DisplayHalf&&dim.KeyPointCount==9&&dim.StatusOfDimension==S.DimStatusConstants.seDimStatusDriven,"Reference diameter display did not persist: half="+dim.DisplayHalf+" keys="+dim.KeyPointCount+" status="+dim.StatusOfDimension);var edge=BushingBodyEdge(.06731+dim.Value/2,.0534543);try{BushingDiameterParents(dim,edge);}finally{Release(edge);}}
   var item=Scalars(dim,"FrameworkSupport","Dimension","Dimension");item.Add(CTexts(dim).Select(t=>new XElement("DisplayedText",t)));report.Add(item);
  }
  new XDocument(report).Save(Path.Combine(CapWork,"main-dimension-verification.xml"));CPreview();Console.WriteLine("PASS: full symmetric diameter display matches reference; content and model links preserved after reopening.");
 }
 static void Bushing21931MainDimensions(){
  Bushing21931Context();LoadCap();var dims=(S.Dimensions)csh.Dimensions;CRequire(dims.Count==1&&dims.Item(1).GetComment()=="BUSHING21931-CHAMFER","Expected approved chamfer stage");
  string chamfer=EngineeringFingerprint(dims.Item(1));var notes=AnnotationContent(csh);
  var positions=csh.DrawingViews.Cast<D.DrawingView>().Select(v=>{double x,y;v.GetOrigin(out x,out y);return new[]{x,y,v.ScaleFactor};}).ToArray();
  var added=new System.Collections.Generic.List<S.Dimension>();bool saved=false;
  try{
   // Match 21930-04: nominal two-place diameters in nested left banks, and
   // three-place inch length limits above the external faces. Keep the exact
   // .625-inch model diameter even though the approved display rounds to .63.
   added.Add(BushingSizeDimension(true,.0079375,.098,.145,"BUSHING21931-OD"));
   added.Add(BushingSizeDimension(true,.003556,.123,.145,"BUSHING21931-ID"));
   var length=BushingSizeDimension(false,0,.150,.182,"BUSHING21931-LENGTH");added.Add(length);
   var limits=CTexts(length);CRequire(limits.Contains(".876")&&limits.Contains(".874")&&limits.Contains("22.25")&&limits.Contains("22.20"),"Length limits do not match the reference's +/- .001 inch tolerance");
   CRequire(CTexts(added[0]).Contains(".63")&&CTexts(added[1]).Contains(".28"),"Diameter precision does not match reference: OD="+string.Join("|",CTexts(added[0]))+" ID="+string.Join("|",CTexts(added[1])));
   cdoc.Save();saved=true;
  }catch{if(!saved)foreach(var dim in added)dim.Delete();throw;}finally{foreach(var dim in added)Release(dim);}
  cdoc.Close(false);Release(cdoc);LoadCap();dims=(S.Dimensions)csh.Dimensions;CRequire(dims.Count==4,"Saved dimension count differs");
  var expected=new System.Collections.Generic.Dictionary<string,double>{{"BUSHING21931-OD",.015875},{"BUSHING21931-ID",.007112},{"BUSHING21931-LENGTH",.022225}};
  var report=new XElement("BushingMainDimensionVerification",new XAttribute("reference","21930-04 bushing.dft"),new XAttribute("diameterInchPlaces",2),new XAttribute("lengthToleranceInch",.001));
  foreach(S.Dimension dim in dims){
   string role=dim.GetComment();if(role=="BUSHING21931-CHAMFER"){CRequire(EngineeringFingerprint(dim)==chamfer,"Approved chamfer changed");continue;}
   CRequire(expected.ContainsKey(role)&&Math.Abs(dim.Value-expected[role])<1e-9&&dim.StatusOfDimension==S.DimStatusConstants.seDimStatusDriven&&string.IsNullOrEmpty(dim.OverrideString),"Saved nominal or association changed");
   bool diameter=role!="BUSHING21931-LENGTH";CRequire(dim.DimensionType==(diameter?S.DimTypeConstants.igDimTypeSDiameter:S.DimTypeConstants.igDimTypeLinear),"Incorrect native dimension type");
   var style=dim.Style;try{CRequire(style.PrimaryDecimalRoundOff==(diameter?S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_1:S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_2)&&style.SecondaryDecimalRoundOff==(diameter?S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_2:S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_3)&&style.DrivenColor==8421376&&Math.Abs(style.ProjectionOffsetGap-.5)<1e-9,"Reference dimension presentation differs");}finally{Release(style);}
   D.DVLine2d a,b;if(diameter){double r=dim.Value/2;a=BushingBodyEdge(.06731-r,.0534543);b=BushingBodyEdge(.06731+r,.0534543);}else{a=BushingBodyEdge(.07305675,.0423418);b=BushingBodyEdge(.07305675,.0645668);}
   try{if(diameter)BushingDiameterParents(dim,b);else VerifyParents(dim,new object[]{a,b});}finally{Release(a);Release(b);}
   var item=Scalars(dim,"FrameworkSupport","Dimension","Dimension");item.Add(CTexts(dim).Select(t=>new XElement("DisplayedText",t)));report.Add(item);
   if(!diameter)CRequire(TextPoint(dim)[1]>.1529375&&dim.DisplayType==S.DimDispTypeConstants.igDimDisplayTypeLimits&&CTexts(dim).Contains(".876")&&CTexts(dim).Contains(".874"),"External length placement or limits changed");
  }
  for(int i=1;i<=3;i++){var view=csh.DrawingViews.Item(i);try{double x,y;view.GetOrigin(out x,out y);var p=positions[i-1];CRequire(Math.Abs(x-p[0])+Math.Abs(y-p[1])+Math.Abs(view.ScaleFactor-p[2])<1e-9,"Approved view layout changed");}finally{Release(view);}}
  CRequire(ciso.Shading&&((S.CenterLines)csh.CenterLines).Count==1&&((S.CenterMarks)csh.CenterMarks).Count==1&&notes.SequenceEqual(AnnotationContent(csh)),"Approved centerlines or notes changed");
  new XDocument(report).Save(Path.Combine(CapWork,"main-dimension-verification.xml"));CPreview();Console.WriteLine("PASS: native OD, bore and overall length dimensions saved/reopened; exact physical parents checked; reference precision and length limits retained. Paused for review.");
 }
 static double[] BushingSegment(D.DVLine2d line){double a,b,c,d;line.GetStartPoint(out a,out b);line.GetEndPoint(out c,out d);csection.ViewToSheet(a,b,out a,out b);csection.ViewToSheet(c,d,out c,out d);return new[]{a,b,c,d};}
 static bool BushingJoined(double[] a,double[] b){for(int i=0;i<4;i+=2)for(int j=0;j<4;j+=2)if(Math.Abs(a[i]-b[j])+Math.Abs(a[i+1]-b[j+1])<1e-8)return true;return false;}
 static void Bushing21931EdgeDimensions(){
  Bushing21931Context();LoadCap();var dims=(S.Dimensions)csh.Dimensions;
  CRequire(dims.Count==0,"Expected the approved centerline stage without dimensions");
  var positions=csh.DrawingViews.Cast<D.DrawingView>().Select(v=>{double x,y;v.GetOrigin(out x,out y);return new[]{x,y,v.ScaleFactor};}).ToArray();
  var notes=AnnotationContent(csh);P.PartDocument part=null;string path=Path.Combine(folder,Bushing21931Name+".par");
  for(int i=1;i<=app.Documents.Count;i++){object item=app.Documents.Item(i);if(string.Equals(((dynamic)item).FullName,path,StringComparison.OrdinalIgnoreCase)){part=(P.PartDocument)item;break;}Release(item);}
  if(part==null)part=(P.PartDocument)app.Documents.Open(path);
  int count;double setback;
  try{
   var model=part.Models.Item(1);try{
    CRequire(model.Chamfers.Count==1&&model.Rounds.Count==0,"Model feature groups differ from the initial audit; inspect before dimensioning");
    var feature=model.Chamfers.Item(1);var faces=(G.Faces)feature.get_Faces(G.FeatureTopologyQueryTypeConstants.igQueryAll);
    try{
     // Each continuous conical face is one physical chamfer. Its two section
     // silhouettes must not double the TYP count.
     count=faces.Count;setback=feature.ChamferSetbackValue1;CRequire(count>0&&Math.Abs(setback-.000762)<1e-9,"Chamfer size changed");
     for(int i=1;i<=faces.Count;i++){var face=(G.Face)faces.Item(i);var cone=face.Geometry as G.Cone;try{CRequire(cone!=null&&Math.Abs(Math.Abs(cone.HalfAngle)-Math.PI/4)<1e-7,"Chamfer is not a 45 degree cone");}finally{Release(cone);Release(face);}}
    }finally{Release(faces);Release(feature);}
   }finally{Release(model);}
  }finally{Release(part);}
  cdoc.Activate();csh.Activate();Console.WriteLine("Physical chamfers="+count+" setback inch="+setback/Inch+"; no rounds/fillets");
  var center=CMap(csection,0,.06731,.0534543);
  var edges=csection.DVLines2d.Cast<D.DVLine2d>().Where(l=>l.ShowHideEdgeOverride!=D.DVShowHideEdgeOverrideType.DVShowHideEdgeOverrideHide).ToArray();
  try{
   var bevel=edges.Single(l=>{var p=BushingSegment(l);return (p[0]+p[2])/2>center[0]&&(p[1]+p[3])/2<center[1]&&Math.Abs(Math.Abs(p[2]-p[0])-setback)<1e-8&&Math.Abs(Math.Abs(p[3]-p[1])-setback)<1e-8;});
   var bevelPoints=BushingSegment(bevel);
   var adjacent=edges.Where(l=>{var p=BushingSegment(l);return Math.Abs(p[3]-p[1])<1e-9&&l.Length>setback*2&&BushingJoined(p,bevelPoints);}).OrderByDescending(l=>l.Length).First();
   var p1=BushingSegment(adjacent);
   var dim=dims.AddChamfer(adjacent.Reference,(p1[0]+p1[2])/2,(p1[1]+p1[3])/2,0,false,bevel.Reference,(bevelPoints[0]+bevelPoints[2])/2,(bevelPoints[1]+bevelPoints[3])/2,0,false,S.DimChamferModeConstants.igDimChamferModePerpendicular);
   try{
    var style=dim.Style;try{CStyle(style);style.PrimaryDecimalRoundOff=S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_1;style.SecondaryDecimalRoundOff=EdgeInchPrecision(setback);style.ChamferUse45DegreeCharacter=false;}finally{Release(style);}
    dim.SetComment("BUSHING21931-CHAMFER");dim.SubfixString=count>1?"TYP("+count+")":"";dim.BreakPosition=S.DimBreakPositionConstants.igDimBreakRight;dim.TrackDistance=Math.PI/4;dim.BreakDistance=.012;dim.LeaderDistance=.0047625;
    PlaceChamferText(dim,.183,.150);
    CRequire(Math.Abs(dim.Value-setback)<1e-9&&Math.Abs(dim.SecondValue-Math.PI/4)<1e-8&&dim.StatusOfDimension==S.DimStatusConstants.seDimStatusDriven,"Native chamfer dimension disagrees with the model");
   }finally{Release(dim);}
  }finally{foreach(var edge in edges)Release(edge);}
  cdoc.Save();cdoc.Close(false);Release(cdoc);LoadCap();dims=(S.Dimensions)csh.Dimensions;
  CRequire(dims.Count==1,"Expected exactly one chamfer dimension");var saved=dims.Item(1);var report=new XElement("BushingEdgeDimensionVerification",new XAttribute("physicalChamfers",count),new XAttribute("setbackInch",setback/Inch),new XAttribute("radiusFeatures",0));
  try{
   CRequire(saved.DimensionType==S.DimTypeConstants.igDimTypeChamfer&&saved.StatusOfDimension==S.DimStatusConstants.seDimStatusDriven&&string.IsNullOrEmpty(saved.OverrideString)&&Math.Abs(saved.Value-setback)<1e-9&&Math.Abs(saved.SecondValue-Math.PI/4)<1e-8,"Saved chamfer lost model association or size");
   CRequire(saved.SubfixString==(count>1?"TYP("+count+")":""),"Physical repetition count changed");
   var style=saved.Style;try{CRequire(style.PrimaryDecimalRoundOff==S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_1&&style.SecondaryDecimalRoundOff==S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_2&&style.DrivenColor==8421376&&Math.Abs(style.Height-.003175)<1e-9,"Saved chamfer presentation differs");}finally{Release(style);}
   var item=Scalars(saved,"FrameworkSupport","Dimension","Chamfer");item.Add(CTexts(saved).Select(t=>new XElement("DisplayedText",t)));report.Add(item);
  }finally{Release(saved);}
  for(int i=1;i<=3;i++){var view=csh.DrawingViews.Item(i);try{double x,y;view.GetOrigin(out x,out y);var p=positions[i-1];CRequire(Math.Abs(x-p[0])+Math.Abs(y-p[1])+Math.Abs(view.ScaleFactor-p[2])<1e-9,"Approved view layout changed");}finally{Release(view);}}
  CRequire(ciso.Shading&&((S.CenterLines)csh.CenterLines).Count==1&&((S.CenterMarks)csh.CenterMarks).Count==1&&notes.SequenceEqual(AnnotationContent(csh)),"Approved center annotations or notes changed");
  new XDocument(report).Save(Path.Combine(CapWork,"edge-dimension-verification.xml"));CPreview();
  Console.WriteLine("PASS: one native .03 inch x 45 degree chamfer dimension with TYP("+count+"); reference metric precision retained; no radius features. Saved and reopened for review.");
 }
 static void Bushing21931Centers(){
  Bushing21931Context();LoadCap();
  CRequire(csh.DrawingViews.Count==3&&((S.CenterLines)csh.CenterLines).Count==0&&((S.CenterMarks)csh.CenterMarks).Count==0,"Expected approved views without center annotations");
  var positions=csh.DrawingViews.Cast<D.DrawingView>().Select(v=>{double x,y;v.GetOrigin(out x,out y);return new[]{x,y,v.ScaleFactor};}).ToArray();
  var content=AnnotationContent(csh);
  // Attach to the two physical bore silhouettes and the end-view OD circle.
  // The model axis is offset from Base, so use its audited axial center.
  var lower=DrawbarPhysicalEdge(CMap(csection,0,.06731-.003556,.0534543));
  var upper=DrawbarPhysicalEdge(CMap(csection,0,.06731+.003556,.0534543));
  var circle=cface.DVCircles2d.Cast<D.DVCircle2d>().First(c=>Math.Abs(c.Radius-.0079375)<1e-9);
  try{
   var line=((S.CenterLines)csh.CenterLines).AddBetweenTwoParallelLines(lower.Reference,upper.Reference);
   try{ReferenceAnnotationStyle.Apply("CenterLine",line);}finally{Release(line);}
   var center=CMap(cface,0,.06731,.0534543);
   var mark=((S.CenterMarks)csh.CenterMarks).AddByCenter(circle.Reference,center[0],center[1],0,true);
   try{ReferenceAnnotationStyle.Apply("CenterMark",mark);}finally{Release(mark);}
  }finally{Release(lower);Release(upper);Release(circle);}
  cdoc.Save();cdoc.Close(false);Release(cdoc);LoadCap();
  CRequire(((S.CenterLines)csh.CenterLines).Count==1&&((S.CenterMarks)csh.CenterMarks).Count==1,"Saved center annotation count differs");
  var savedLine=((S.CenterLines)csh.CenterLines).Item(1);var savedMark=((S.CenterMarks)csh.CenterMarks).Item(1);
  var report=new XElement("BushingCenterVerification",new XAttribute("sectionCenterlines",1),new XAttribute("endViewCenterMarks",1),new XAttribute("isoCenterAnnotations",0));
  try{
   ReferenceAnnotationStyle.Apply("CenterLine",savedLine,true);ReferenceAnnotationStyle.Apply("CenterMark",savedMark,true);
   double a,b,c,d;savedLine.Range(out a,out b,out c,out d);var center=CMap(csection,0,.06731,.0534543);
   CRequire(Math.Abs((b+d)/2-center[1])<1e-8&&a<center[0]&&c>center[0],"Section centerline is not on the bore axis");
   report.Add(Scalars(savedLine,"FrameworkSupport","CenterLine","CenterLine"));report.Add(Scalars(savedMark,"FrameworkSupport","CenterMark","CenterMark"));
  }finally{Release(savedLine);Release(savedMark);}
  for(int i=1;i<=3;i++){var v=csh.DrawingViews.Item(i);try{double x,y;v.GetOrigin(out x,out y);var p=positions[i-1];CRequire(Math.Abs(x-p[0])+Math.Abs(y-p[1])+Math.Abs(v.ScaleFactor-p[2])<1e-9,"Approved view placement or scale changed");}finally{Release(v);}}
  CRequire(ciso.Shading&&cface.Defaults_ShowHiddenEdges&&csh.SheetSetup.SheetSizeOption==D.PaperSizeConstants.igAnsiBWide,"Approved view presentation changed");
  var cp=cface.CuttingPlanes.Item(1);try{CRequire(cp.ViewSide==F.SectionViewProfileSide.igRightProfileSide,"Section direction changed");BaseOriginConstraints.Ensure(cp,true);foreach(S.Line2d line in cp.Profile.Lines2d){try{CRequire(AxisConstraints.Has(line,(int)F.ObjectType.igVerticalRelation2d),"Cutting line lost native Vertical constraint");}finally{Release(line);}}}finally{Release(cp);}
  CRequire(((S.Dimensions)csh.Dimensions).Count==0&&content.SequenceEqual(AnnotationContent(csh)),"Detailing advanced beyond the centerline stage");
  new XDocument(report).Save(Path.Combine(CapWork,"center-verification.xml"));CPreview();
  Console.WriteLine("PASS: native section centerline and end-view center mark saved/reopened; reference properties verified; approved view layout preserved; ISO has no center annotation.");
 }
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
