using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml.Linq;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;
using F=SolidEdge.Framework.Interop;

partial class CapDetailTool {
 static string OutputPath {get{return Path.Combine(folder,"End Cap_21612-100.dft");}}
 static string Digest(string path){using(var sha=SHA256.Create())using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))return BitConverter.ToString(sha.ComputeHash(stream));}
 // The closest supplied reference already describes this exact model. Preserve its
 // associative objects and documented fit limits instead of tracing or redimensioning.
 static void CreateDrawing(bool resume=false){
  AxisConstraints.Test();
  if(!resume&&(File.Exists(OutputPath)||IsOpen(OutputPath)))throw new Exception("Output already exists; use --verify, never overwrite an open document.");
  string source=Path.Combine(folder,"21612-104_Cap.dft"), before=Digest(source), partBefore=Digest(partPath);
  if(!resume){bool wasOpen=IsOpen(source);var original=(D.DraftDocument)app.Documents.Open(source);
  try{original.SaveCopyAs(OutputPath);}finally{if(!wasOpen)original.Close(false);Marshal.ReleaseComObject(original);}}
  var draft=(D.DraftDocument)app.Documents.Open(OutputPath);
  try{
   var sheet=draft.Sheets.Item("Sheet1");sheet.Activate();
   // Refresh only the company border from the requested normal template. The
   // working sheet's dimensions, callouts, symbols and views remain native objects.
   string normal=Path.GetFullPath(Path.Combine(root,"..","HydraulicFitting","tmp","company-normal.dft"));
   sheet.Background.ReplaceBackgroundEx(normal,"B-Size",true);
   sheet.BackgroundVisible=true;
   for(int i=1;i<=draft.ModelLinks.Count;i++){
    var link=draft.ModelLinks.Item(i);Console.WriteLine("Model link: "+link.FileName);
    if(!string.Equals(Path.GetFullPath(link.FileName),partPath,StringComparison.OrdinalIgnoreCase))link.ChangeSource(partPath);
    Marshal.ReleaseComObject(link);
   }
   app.SetGlobalParameter(F.ApplicationGlobalConstants.seApplicationGlobalProjectionAngleDraft,(int)D.DrawingViewProjectionAngleConstants.ProjectionAngleThird);
   draft.MaintainAlignmentSet=true;
   foreach(D.DrawingView view in sheet.DrawingViews)view.Update();
   var face=sheet.DrawingViews.Item(1);var section=sheet.DrawingViews.Item(3);
   var selection=draft.SelectSet;selection.RemoveAll();selection.Add(face);selection.Add(section);sheet.DrawingViews.Align();selection.RemoveAll();
   int axes=0,origins=0;
   foreach(D.DrawingView view in sheet.DrawingViews)foreach(D.CuttingPlane cut in view.CuttingPlanes){
    var profile=cut.Profile;profile.BeginEdit();
    try{foreach(S.Line2d line in profile.Lines2d)if(AxisConstraints.Ensure(profile.Relations2d,line))axes++;}finally{profile.EndEdit();}
    origins+=BaseOriginConstraints.Ensure(cut);
   }
   Console.WriteLine("Constraints: added axis="+axes+", base origin verified="+origins);
   draft.UpdatePropertyTextCacheAndDisplay();
   VerifyDrawing(draft,true);
   // View movement can reset automatically positioned cutting-plane captions.
   // Restore the reference's deliberately separated label positions afterwards.
   var newCut=face.CuttingPlanes.Item(1);var cutLine=newCut.Profile.Lines2d.Item(1);
   double ax,ay,bx,by;cutLine.GetStartPoint(out ax,out ay);cutLine.GetEndPoint(out bx,out by);
   newCut.SetCaptionPosition(true,ax+.025,ay);newCut.SetCaptionPosition(false,bx+.014,by-.007);
   sheet.Activate();VerifyDrawing(draft,false);
   draft.Save();
   if(Digest(source)!=before)throw new Exception("Reference drawing changed unexpectedly.");
   Console.WriteLine("Reference drawing unchanged; linked model file hash unchanged this run="+(Digest(partPath)==partBefore));
  }finally{Marshal.ReleaseComObject(draft);}
 }
 static void VerifySavedDrawing(){
  // A real close/reopen validates persisted constraints, not only session state.
  var draft=(D.DraftDocument)app.Documents.Open(OutputPath);draft.Close(false);Marshal.ReleaseComObject(draft);
  draft=(D.DraftDocument)app.Documents.Open(OutputPath);try{VerifyDrawing(draft,false);}finally{Marshal.ReleaseComObject(draft);}
 }
 static void VerifyDrawing(D.DraftDocument draft,bool testAlignment){
  var sheet=draft.Sheets.Item("Sheet1");sheet.Activate();
  var report=new XElement("CapDrawingVerification",new XAttribute("file",OutputPath));
  if(sheet.DrawingViews.Count!=3)throw new Exception("Expected face, section, and isometric views.");
  foreach(D.DrawingView view in sheet.DrawingViews){if(!view.IsUpToDate)throw new Exception("View is not up to date");report.Add(Scalars(view,"Draft","DrawingView","View"));
   foreach(D.CuttingPlane cut in view.CuttingPlanes){
    BaseOriginConstraints.Ensure(cut,true);
    foreach(S.Line2d line in cut.Profile.Lines2d){double a,b,c,d;line.GetStartPoint(out a,out b);line.GetEndPoint(out c,out d);int kind=AxisConstraints.Direction(a,b,c,d);if(kind!=0&&!AxisConstraints.Has(line,kind))throw new Exception("Missing axis constraint");}
   }
  }
  var face=sheet.DrawingViews.Item(1);var section=sheet.DrawingViews.Item(3);double fx,fy,sx,sy;face.GetOrigin(out fx,out fy);section.GetOrigin(out sx,out sy);
  if(Math.Abs(fy-sy)>1e-8||sx>=fx)throw new Exception("Section is not horizontally aligned to the left of the face view.");
  if(testAlignment){
   // Prove that the association actually follows a moved source, then restore it.
   try{face.SetOrigin(fx,fy+.001);double x,y;section.GetOrigin(out x,out y);if(Math.Abs(y-sy-.001)>1e-8)throw new Exception("Section did not follow source alignment.");}
   finally{face.SetOrigin(fx,fy);}
  }
  var dims=(S.Dimensions)sheet.Dimensions;
  if(dims.Count!=14)throw new Exception("Dimension count changed");
  foreach(S.Dimension dim in dims){if(dim.StatusOfDimension!=S.DimStatusConstants.seDimStatusDriven)throw new Exception("Dimension lost its driven association");Console.WriteLine("Dimension "+dim.DimensionType+" value="+dim.Value+" status="+dim.StatusOfDimension);report.Add(Scalars(dim,"FrameworkSupport","Dimension","Dimension"));}
  var balloons=(S.Balloons)sheet.Balloons;if(balloons.Count!=4)throw new Exception("Callout count changed");
  foreach(S.Balloon callout in balloons){Console.WriteLine("Callout: "+callout.BalloonDisplayedText.Replace('\r',' '));report.Add(Scalars(callout,"FrameworkSupport","Balloon","Callout"));}
  report.SetAttributeValue("centerlines",((S.CenterLines)sheet.CenterLines).Count);report.SetAttributeValue("centermarks",((S.CenterMarks)sheet.CenterMarks).Count);
  if(((S.CenterLines)sheet.CenterLines).Count!=4||((S.CenterMarks)sheet.CenterMarks).Count!=13)throw new Exception("Center geometry count changed");
  new XDocument(report).Save(Path.Combine(root,"final-verification.xml"));
  app.DoIdle();var window=(D.SheetWindow)app.ActiveWindow;window.FitEx(D.SheetFitConstants.igFitSheet);window.SaveAsImage(Path.Combine(root,"End Cap-final.jpg"),3000,2000,1,24,F.SeImageQualityType.seImageQualityHigh,false);Marshal.ReleaseComObject(window);
  Console.WriteLine("Verified: 3 views, 14 dimensions, 4 callouts, 4 centerlines, 13 center marks; section aligned.");
 }
}

