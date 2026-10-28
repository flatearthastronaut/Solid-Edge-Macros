using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Part=SolidEdge.Part.Interop;
using Geo=SolidEdge.Geometry.Interop;
using Framework=SolidEdge.Framework.Interop;
using Draft=SolidEdge.Draft.Interop;

partial class BuildFitting {
 const double AF=.6875, Length=1.09, Shoulder=.400, Passage=.188;
 const double PortU=.489, PortK=.093, MouthR=.006, TapDrill=.390625, TapDepth=.560, ThreadDepth=.460;
 static string root;
 static double MaleRadius(double z){return (9.728-4.0/16.0)/(2*25.4)+z/32.0;}
 [STAThread]static int Main(string[] args){
  try{using(var filter=new BusyFilter()){
   root=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,".."));output=Path.Combine(root,"Models");
   if(args.Length>0&&args[0]=="--test"){CheckDesign();return 0;}
   app=Keep(Marshal.GetActiveObject("SolidEdge.Application"));
      if(args.Length>0&&args[0]=="--tidymodel"){doc=(Part.PartDocument)app.Documents.Open(Path.Combine(output,"BSPT-02 to SAE-04 ORB Adapter.par"));foreach(Part.RefAxis ax in doc.RefAxes)ax.Visible=false;foreach(Part.ProfileSet ps in doc.ProfileSets)foreach(Part.Profile pr in ps.Profiles)pr.Visible=false;doc.Save();return 0;}
   if(args.Length>0&&args[0]=="--cleanup"){for(int i=app.Documents.Count;i>=1;i--){var trial=app.Documents.Item(i) as Draft.DraftDocument;if(trial!=null&&trial.Name=="Draft10"&&trial.FullName=="Draft10"&&trial.ModelLinks.Count==0&&trial.ActiveSheet.Name=="Sheet1"){trial.Close(false);Console.WriteLine("Closed unused template trial Draft10");}}return 0;}
   if(args.Length>0&&args[0]=="--verify"){VerifyDeliverables();return 0;}
   if(args.Length>0&&args[0]=="--plane-draft"){CreatePlaneDraft();return 0;}
   if(args.Length>0&&args[0]=="--draft"){CreateDrawing();return 0;}
   if(args.Length>0&&args[0]=="--template"){
    var draft=(Draft.DraftDocument)app.Documents.Add("SolidEdge.DraftDocument",Path.Combine(root,"tmp","company-normal.dft"));Console.WriteLine("DEFAULT DRAFT "+draft.Name);foreach(Draft.Sheet s in draft.Sheets)Console.WriteLine("SHEET "+s.Name+" "+s.SectionType);draft.SaveAs(Path.Combine(root,"tmp","normal-template.dft"));draft.Close(false);return 0;
   }
   CloseTrials();CheckDesign();
   var path=Path.Combine(output,"BSPT-02 to SAE-04 ORB Adapter.par");if(File.Exists(path))throw new IOException("Part already exists; refusing to overwrite.");
   doc=Keep((Part.PartDocument)app.Documents.Add("SolidEdge.PartDocument"));doc.ModelingMode=Part.ModelingModeConstants.seModelingModeOrdered;model=null;drawingDepth=0;
   var vertices=new double[6][];double circum=AF/Math.Sqrt(3);for(int i=0;i<6;i++){double t=i*Math.PI/3;vertices[i]=P(circum*Math.Cos(t),circum*Math.Sin(t),0);}
   Extrude(Polygon(2,0,vertices),2,1,Length,"11-16 HEX STOCK x 1.090");
   var outside=Polygon(1,0,P(.6,0,0),P(.6,0,Shoulder),P(MaleRadius(Shoulder),0,Shoulder),P(MaleRadius(.020),0,.020),P(MaleRadius(.020)-.020,0,0));
   Revolve(outside,"Turn R1-8 tapered blank and tip chamfer");
   double endFace=AF/2-.020;
   Revolve(Polygon(1,0,P(.6,0,Length),P(endFace,0,Length),P(.6,0,Length-(.6-endFace))),"Turn female-end hex edge chamfer");
   // The through passage and female thread remain native Hole features so their
   // drafting callouts evaluate hole and thread size/depth from model metadata.
   Hole(2,1,P(0,0,0),Passage,Length,"DIA .188 THROUGH PASSAGE");
   Hole(2,-1,P(0,0,Length),TapDrill,TapDepth,"SAE -4 ORB: 7-16-20 UNF-2B",thread:"7/16-20 UNF",series:"UNF",threadDepth:ThreadDepth);
   var port=Profile(1,0);double top=PortU/2,cz=Length-MouthR,ang=12*Math.PI/180;
   double tangentX=top-MouthR*Math.Cos(ang),tangentZ=cz+MouthR*Math.Sin(ang);
   double virtualTop=top-MouthR*(1/Math.Cos(ang)-Math.Tan(ang));
   double bottomX=virtualTop-PortK*Math.Tan(ang),leadZ=Length-PortK-(bottomX-TapDrill/2);
   Line(port,P(0,0,Length),P(top,0,Length));
   double mid=(Math.PI/2+Math.PI-ang)/2;
   Arc(port,P(top,0,Length),P(top+MouthR*Math.Cos(mid),0,cz+MouthR*Math.Sin(mid)),P(tangentX,0,tangentZ));
   Line(port,P(tangentX,0,tangentZ),P(bottomX,0,Length-PortK));Line(port,P(bottomX,0,Length-PortK),P(TapDrill/2,0,leadZ));
   Line(port,P(TapDrill/2,0,leadZ),P(0,0,leadZ));Line(port,P(0,0,leadZ),P(0,0,Length));
   Revolve(port,"SAE J1926 entrance: DIA .489, 12deg, R.006");
   // External BSPT uses the exact tapered envelope; the drawing specifies ISO 7-1.
   doc.Recompute();double vol=V();Console.WriteLine("VALID SOLID in3="+vol/Math.Pow(Inch,3));
   SetProperties();
   for(int i=1;i<=doc.RefPlanes.Count;i++)doc.RefPlanes.Item(i).Visible=false;
   doc.SaveAs(path);Console.WriteLine("SAVED "+path);
   app.StartCommand(32810);app.StartCommand(32821);app.DoIdle();var v=(Framework.View)app.ActiveWindow.View;v.Fit();v.SaveAsImage(Path.Combine(output,"Adapter-model.jpg"),1600,1200,Resolution:1,ColorDepth:24,ImageQuality:Framework.SeImageQualityType.seImageQualityHigh,Invert:false);
   VerifyModel();doc.Save();
  }return 0;}catch(Exception e){Console.WriteLine(e);return 1;}
  finally{var seen=new HashSet<object>();for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null&&seen.Add(owned[i])&&Marshal.IsComObject(owned[i]))try{Marshal.ReleaseComObject(owned[i]);}catch(COMException){}}
 }
 static void Revolve(Part.Profile p,string name){
  var a=UV(p,P(0,0,-.05));var b=UV(p,P(0,0,1.15));var axisLine=Keep(p.Lines2d.AddBy2Points(a[0],a[1],b[0],b[1]));AxisConstraints.Ensure(Keep(p.Relations2d),axisLine);p.ToggleConstruction(axisLine);var axis=Keep((Part.RefAxis)p.SetAxisOfRevolution(axisLine));End(p);
  double old=V();var cut=Keep(model.RevolvedCutouts.AddFinite(p,axis,Part.FeaturePropertyConstants.igLeft,Part.FeaturePropertyConstants.igRight,2*Math.PI));cut.Name=name;object status;if(cut.get_Status(out status)!=Part.FeatureStatusConstants.igFeatureOK||V()>=old)throw new Exception(name+" failed: "+status);Console.WriteLine(name+" OK");
 }
 static void CloseTrials(){
  var docs=(Framework.Documents)app.Documents;
  for(int i=docs.Count;i>=1;i--){object item=docs.Item(i);var p=item as Part.PartDocument;if(p==null)continue;
   try{if(p.Name.StartsWith("Part")&&p.Models.Count==1&&p.Models.Item(1).ExtrudedProtrusions.Item(1).Name=="11-16 HEX STOCK x 1.090"){Console.WriteLine("Closing unused fitting trial "+p.Name);p.Close(false);}}catch{}
  }
 } static void CheckDesign(){
  AxisConstraints.Test();
  if(Math.Abs(MaleRadius(4/25.4)*2-9.728/25.4)>1e-10)throw new Exception("BSPT gauge plane diameter");
  if(Math.Abs((MaleRadius(.35)-MaleRadius(0))*2/.35-1.0/16)>1e-10)throw new Exception("BSPT taper");
  if(Length-TapDepth<=Shoulder||PortU<.487||PortU>.492||PortK<.093||MouthR<.004||MouthR>.008||ThreadDepth<.454||TapDepth<.547)throw new Exception("Port/stock depth check");
  Console.WriteLine("PASS: catalog envelope, BSPT gauge/taper, port limits, axial wall separation");
 }
 static void VerifyModel(){
  var body=(Geo.Body)model.Body;Array low=new double[3],high=new double[3];body.GetExactRange(ref low,ref high);
  if(doc.Models.Count!=1||!body.IsSolid||!body.Valid||Math.Abs(((double[])high)[2]/Inch-Length)>1e-7||model.Holes.Count!=2)throw new Exception("Model verification failed");
  var hd=(Part.HoleData)model.Holes.Item(2).HoleData;if(hd.ThreadDescription!="7/16-20 UNF"||Math.Abs(hd.ThreadDepth/Inch-ThreadDepth)>1e-8)throw new Exception("Female thread validation failed");
  File.WriteAllText(Path.Combine(output,"model-validation.txt"),"PASS: single valid solid; overall length 1.090 in; hex 0.6875 in; native 7/16-20 UNF hole, depth .460 in; BSPT taper envelope (thread helix omitted).\r\nCatalog envelope reproduced; undocumented machining details are design choices.\r\n");
 }
 static void SetProperties(){
  var props=(Framework.Properties)((Framework.PropertySets)doc.Properties).Item("Custom");
  foreach(var pair in new Dictionary<string,string>{{"external thread","R1/8-28 BSPT - ISO 7-1"},{"drawing number","HF-02BSPT-04ORB"},{"detail number",""},{"revision letter","A"},{"description","BSPT MALE TO SAE ORB FEMALE"},{"description line 2","1/8 BSPT x -4 SAE ADAPTER"},{"matl","ASTM A108 12L14 HEX"},{"finish","ZINC PLATE"},{"detailer",""},{"date detailed","10/28/2026"},{"designer",""},{"date designed",""},{"checker",""},{"date checked",""},{"heat treat","NONE"}}){try{props.Item(pair.Key).set_Value(pair.Value);}catch{props.Add(pair.Key,pair.Value);}}
 }
}














