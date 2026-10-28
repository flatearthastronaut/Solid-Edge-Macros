using System;
using System.IO;
using System.Runtime.InteropServices;
using D=SolidEdge.Draft.Interop;
using F=SolidEdge.Framework.Interop;

partial class DimensionStandardTool {
 static int CheckOrigins(D.DraftDocument draft,bool verify){
  int count=0;
  foreach(D.DrawingView view in draft.ActiveSheet.DrawingViews)
   foreach(D.CuttingPlane cp in view.CuttingPlanes)count+=BaseOriginConstraints.Ensure(cp,verify);
  return count;
 }
 static void ConnectOrigins(){
  var report=new System.Text.StringBuilder();BaseOriginConstraints.Test();
  foreach(string name in new[]{"BSPT-02 to SAE-04 ORB Adapter.dft","BSPT-02 to SAE-04 ORB Adapter - Plane Sections.dft"}){
   string path=Path.GetFullPath(Path.Combine(root,"..","HydraulicFitting","Models",name));
   var draft=(D.DraftDocument)app.Documents.Open(path);int expected=name.Contains("Plane Sections")?2:1;
   string before=AnnotationMeaning(draft);
   if(CheckOrigins(draft,false)!=expected)throw new Exception("Not all expected cuts pass through Base");
   if(before!=AnnotationMeaning(draft))throw new Exception("Cut geometry or content changed");
   // Exercise the actual associative relation, not merely stored coordinates.
   foreach(D.DrawingView view in draft.ActiveSheet.DrawingViews){if(view.CuttingPlanes.Count==0)continue;
    double x,y;view.GetOrigin(out x,out y);
    try{view.SetOrigin(x+.001,y+.001);app.DoIdle();if(CheckOrigins(draft,true)!=expected)throw new Exception("Cut lost Base connection when source view moved");}
    finally{view.SetOrigin(x,y);app.DoIdle();}
   }
   if(before!=AnnotationMeaning(draft))throw new Exception("View movement was not fully restored");
   draft.Save();draft.Close(false);Marshal.ReleaseComObject(draft);
   draft=(D.DraftDocument)app.Documents.Open(path);
   if(CheckOrigins(draft,true)!=expected||before!=AnnotationMeaning(draft))throw new Exception("Saved Base-origin constraint verification failed");
   ReferenceAnnotationStyle.Document(draft,true);
   draft.ActiveSheet.Activate();app.DoIdle();var window=(D.SheetWindow)app.ActiveWindow;window.FitEx(D.SheetFitConstants.igFitSheet);
   window.SaveAsImage(Path.Combine(Path.GetDirectoryName(path),name.Contains("Plane Sections")?"Adapter-plane-sections.jpg":"Adapter-drawing.jpg"),3060,1980,1,24,F.SeImageQualityType.seImageQualityHigh,false);
   Marshal.ReleaseComObject(window);Marshal.ReleaseComObject(draft);
   report.AppendLine(name+": PASS "+expected+" native point-on relations to the model Base CSYS origin; source-view movement and saved/reopened relationship targets verified. Geometry, content, existing Horizontal/Vertical constraints and annotation styles retained.");
  }
  File.WriteAllText(Path.Combine(root,"base-origin-constraint-verification.txt"),report.ToString());Console.WriteLine(report);
 }
}
