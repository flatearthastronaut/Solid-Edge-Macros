using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;
using F=SolidEdge.Framework.Interop;

partial class CapDetailTool {
 static List<string> CTexts(S.Dimension dimension){var result=new List<string>();var display=dimension.GetDisplayData();try{for(int i=0;i<display.GetTextCount();i++){string t;double a,b,c,d,e,f,g,h,j;display.GetTextAtIndex(i,out t,out a,out b,out c,out d,out e,out f,out g,out h,out j);result.Add(t);}}finally{Marshal.ReleaseComObject(display);}return result;}
 static void CRequire(bool condition,string message){if(!condition)throw new Exception(message);}
 static void Verify19679(){
  LoadCap();AxisConstraints.Test();BaseOriginConstraints.Test();
  object projection;app.GetGlobalParameter(F.ApplicationGlobalConstants.seApplicationGlobalProjectionAngleDraft,out projection);CRequire(Convert.ToInt32(projection)==(int)D.DrawingViewProjectionAngleConstants.ProjectionAngleThird,"Projection setting is not third angle");
  // Verify the actual alignment relationship by moving the parent, then restore
  // it in a finally block. Reopening below proves that the result is persistent.
  double fx,fy,sx,sy;cface.GetOrigin(out fx,out fy);csection.GetOrigin(out sx,out sy);
  CRequire(Math.Abs(fy-sy)<1e-8,"Section origins are not aligned");
  try{cface.SetOrigin(fx,fy+.001);double x,y;csection.GetOrigin(out x,out y);CRequire(Math.Abs(y-sy-.001)<1e-8,"Section did not follow parent");CRequire(BaseOriginConstraints.Ensure(cface.CuttingPlanes.Item(1),true)==1,"Base constraint not verified after movement");}
  finally{cface.SetOrigin(fx,fy);}
  csection.SetCaptionPosition(.112,.061);cdoc.Save();cdoc.Close(false);Marshal.ReleaseComObject(cdoc);LoadCap();
  var report=new XElement("CAP19679Verification",new XAttribute("file",CapPath),new XAttribute("thirdAngle",true),new XAttribute("alignmentMovementTest",true));
  CRequire(csh.DrawingViews.Count==3,"Expected three views");
  foreach(D.DrawingView view in csh.DrawingViews){CRequire(view.IsUpToDate,"Drawing view out of date");report.Add(Scalars(view,"Draft","DrawingView","View"));}
  CRequire(cdoc.MaintainAlignmentSet,"Maintain alignment is disabled");cface.GetOrigin(out fx,out fy);csection.GetOrigin(out sx,out sy);CRequire(Math.Abs(fy-sy)<1e-8&&sx<fx,"Saved section alignment differs");
  var cut=cface.CuttingPlanes.Item(1);CRequire(BaseOriginConstraints.Ensure(cut,true)==1,"Missing saved Base-origin constraint");foreach(S.Line2d line in cut.Profile.Lines2d){double x,y,a,b;line.GetStartPoint(out x,out y);line.GetEndPoint(out a,out b);int kind=AxisConstraints.Direction(x,y,a,b);CRequire(kind!=0&&AxisConstraints.Has(line,kind),"Cutting line lacks native axis constraint");}
  var dims=(S.Dimensions)csh.Dimensions;CRequire(dims.Count==19,"Expected 19 native dimensions");
  foreach(S.Dimension dimension in dims){CRequire(dimension.StatusOfDimension==S.DimStatusConstants.seDimStatusDriven,"Detached dimension "+dimension.Index);CRequire(string.IsNullOrEmpty(dimension.OverrideString),"Dimension has overridden value");CRequire(dimension.Style.DrivenColor==8421376&&dimension.Style.Font=="Arial"&&Math.Abs(dimension.Style.Height-.003175)<1e-9,"Dimension presentation differs");var item=Scalars(dimension,"FrameworkSupport","Dimension","Dimension");item.Add(CTexts(dimension).Select(t=>new XElement("DisplayedText",t)));report.Add(item);}
  int[] ids={1,2,3,4,5,8,9,10,11};double[] tolerances={.001,.001,.002,.001,.002,.001,.0025,.001,.0025};
  for(int i=0;i<ids.Length;i++){var d=dims.Item(ids[i]);var texts=CTexts(d);var numbers=new List<double>();foreach(var t in texts){double number;if(double.TryParse(t,NumberStyles.Float,CultureInfo.InvariantCulture,out number))numbers.Add(number);}CRequire(numbers.Count>=4,"Missing native limit text");double nominal=d.Value/Inch,tol=tolerances[i];CRequire(Math.Abs(numbers[0]-(nominal+tol)*25.4)<.0051&&Math.Abs(numbers[1]-(nominal-tol)*25.4)<.0051,"Metric limit text mismatch "+ids[i]);double precision=ids[i]==10?.000051:.000501;CRequire(Math.Abs(numbers[2]-nominal-tol)<precision&&Math.Abs(numbers[3]-nominal+tol)<precision,"Inch limit text mismatch "+ids[i]);CRequire(d.DisplayType==S.DimDispTypeConstants.igDimDisplayTypeLimits,"Dimension is not native limits");}
  CRequire(dims.Cast<S.Dimension>().Count(d=>d.DimensionType==S.DimTypeConstants.igDimTypeChamfer)==3,"Chamfer dimension count");
  var hole=((S.Balloons)csh.Balloons).Item(1);CRequire(hole.IsTerminatorAttachedToEntity&&hole.BalloonText.Contains("%HS")&&hole.BalloonText.Contains("%ZH")&&hole.BalloonText.Contains("%QC"),"Hole callout lost feature references");string holeText=hole.BalloonDisplayedText;CRequire(holeText.Contains("6.60")&&holeText.Contains("10.41")&&holeText.Contains("7.62")&&holeText.Contains("(8)"),"Hole callout values differ");report.Add(Scalars(hole,"FrameworkSupport","Balloon","HoleCallout"));
  CRequire(((S.CenterLines)csh.CenterLines).Count==3&&((S.CenterMarks)csh.CenterMarks).Count==9,"Center geometry count differs");CRequire(((S.SurfaceFinishSymbols)csh.SurfaceFinishSymbols).Count==5,"Finish symbol count differs");foreach(S.SurfaceFinishSymbol f in (S.SurfaceFinishSymbols)csh.SurfaceFinishSymbols)CRequire(f.IsTerminatorAttachedToEntity&&f.MaximumRoughnessValue=="32"&&f.MinimumRoughnessValue=="TURN","Finish symbol association differs");
  // Compare the model's complete audited feature definitions and profile points,
  // plus exact body range and volume, against the snapshot taken before drafting.
  string work=CapWork,oldRoot=root,oldPart=partPath;var baseline=XDocument.Load(Path.Combine(work,"part-audit.xml"));
  try{root=Path.Combine(work,"Verified Model");Directory.CreateDirectory(root);partPath=Path.Combine(folder,"CAP_19679.par");AuditPart(false);var current=XDocument.Load(Path.Combine(root,"part-audit.xml"));CRequire(XNode.DeepEquals(new XElement("Geometry",baseline.Root.Elements().Where(e=>e.Name=="Model"||e.Name=="Profile")),new XElement("Geometry",current.Root.Elements().Where(e=>e.Name=="Model"||e.Name=="Profile"))),"Model geometry audit differs from original");}
  finally{root=oldRoot;partPath=oldPart;}
  report.SetAttributeValue("modelGeometryUnchanged",true);new XDocument(report).Save(Path.Combine(work,"final-verification.xml"));CPreview();Console.WriteLine("PASS: 3 current views; aligned section; Base and vertical constraints; 19 driven dimensions; verified displayed limits; native hole callout; 5 attached finishes; unchanged model geometry.");
 }
}
