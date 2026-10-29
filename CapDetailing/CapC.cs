using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;

partial class CapDetailTool {
 static void ApplyCapCounterborePrecision(){
  // These caps have one counterbored clearance-hole pattern. Change only its
  // callout and bolt-circle dimension; other fits retain their precision.
  var bc=ByRole((S.Dimensions)csh.Dimensions,12);
  var hole=((S.Balloons)csh.Balloons).Item(1);
  foreach(var style in new[]{bc.Style,hole.Style})try{
   style.PrimaryDecimalRoundOff=S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_2;
   style.SecondaryDecimalRoundOff=S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_2;
  }finally{Release(style);}
 }
 static void UpdateCapCHolePrecision(){
  capName="CAP_C";LoadCap();
  var dims=(S.Dimensions)csh.Dimensions;
  var unaffected=dims.Cast<S.Dimension>().Where(d=>d.GetComment()!=ByRole(dims,12).GetComment()).Select(EngineeringFingerprint).OrderBy(s=>s).ToArray();
  var notes=AnnotationContent(csh);
  ApplyCapCounterborePrecision();cdoc.Save();cdoc.Close(false);Release(cdoc);LoadCap();
  dims=(S.Dimensions)csh.Dimensions;var bc=ByRole(dims,12);var hole=((S.Balloons)csh.Balloons).Item(1);
  foreach(var style in new[]{bc.Style,hole.Style})try{CRequire(style.PrimaryDecimalRoundOff==S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_2&&style.SecondaryDecimalRoundOff==S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_2,"Two-place precision did not persist");}finally{Release(style);}
  CRequire(CTexts(bc).Contains("6.41")&&!CTexts(bc).Contains("6.410"),"Bolt-circle displayed precision differs");
  CRequire(unaffected.SequenceEqual(dims.Cast<S.Dimension>().Where(d=>d.GetComment()!=bc.GetComment()).Select(EngineeringFingerprint).OrderBy(s=>s)),"Unrelated dimension changed");
  CRequire(notes.SequenceEqual(AnnotationContent(csh)),"Callout content changed unexpectedly");
  // The user intentionally changed BC presentation, so refresh only the
  // dimension baseline after checking that every other dimension is identical.
  File.WriteAllLines(Path.Combine(CapWork,"placement-before.txt"),dims.Cast<S.Dimension>().Select(EngineeringFingerprint).OrderBy(s=>s));
  Verify19679();CheckCapCValues();
  Console.WriteLine("PASS: counterbore and BC precision is two places after reopening; other dimensions and callout feature references preserved.");
 }
 static double[][] CapProfile(string name){var doc=XDocument.Load(Path.Combine(CapWork,"part-audit.xml"));return doc.Root.Elements("Profile").Single(e=>(string)e.Attribute("name")==name).Elements("Line").Select(e=>((string)e.Attribute("xyz")).Split(',').Select(s=>double.Parse(s,CultureInfo.InvariantCulture)).ToArray()).ToArray();}
 static double[] CapCValues(){
  // Read engineering nominals independently from the native profile audit.
  // The reused draft must measure these values after model-link replacement.
  var p=CapProfile("Profile_32");var inner=CapProfile("Profile_33");var outer=CapProfile("Profile_37");
  double front=p[0][2],rear=p[1][5],shoulder=p[6][2];
  var audit=XDocument.Load(Path.Combine(CapWork,"part-audit.xml"));
  double bc=2*double.Parse((string)audit.Root.Elements("Profile").Single(e=>(string)e.Attribute("name")=="Profile_34").Element("Circle").Attribute("radius"),CultureInfo.InvariantCulture);
  return new[]{2*p[0][4],2*p[2][4],2*outer[0][1],2*p[0][1],2*inner[0][1],front-rear,shoulder-rear,front-inner[0][5],inner[0][2]-inner[0][5],outer[0][2]-rear,outer[0][2]-outer[0][5],bc,.000762,Math.Abs(p[4][4]-p[4][1]),.001524,.000381,.001524,2*p[5][1],shoulder-p[5][2]};
 }
 static void CheckCapCValues(){LoadCap();var dims=(S.Dimensions)csh.Dimensions;var values=CapCValues();CRequire(dims.Count==values.Length,"CAP_C dimension count differs");for(int i=0;i<values.Length;i++){var d=ByRole(dims,i+1);Console.WriteLine("Role "+(i+1)+" model="+values[i]+" drawing="+d.Value+" status="+d.StatusOfDimension);CRequire(d.StatusOfDimension==S.DimStatusConstants.seDimStatusDriven&&Math.Abs(d.Value-values[i])<1e-8,"CAP_C geometry mismatch for dimension "+(i+1));}}
 static void CreateCapC(){
  capName="CAP_C";Directory.CreateDirectory(CapWork);CRequire(!File.Exists(CapPath),"CAP_C draft exists; inspect it rather than overwrite it");
  string source=Path.Combine(folder,"CAP_19679.dft"),hash=Digest(source);bool wasOpen=IsOpen(source);var original=(D.DraftDocument)app.Documents.Open(source);
  try{original.SaveCopyAs(CapPath);}finally{if(!wasOpen)original.Close(false);Release(original);}
  LoadCap();foreach(D.ModelLink link in cdoc.ModelLinks){try{link.ChangeSource(Path.Combine(folder,"CAP_C.par"));}finally{Release(link);}}
  foreach(D.DrawingView view in csh.DrawingViews){try{view.Update();}finally{Release(view);}}
  CheckCapCValues();
  ApplyCapCounterborePrecision();
  // Retain the approved tolerance amounts; calculate new limit text from the
  // changed nominals. Do not copy the previous part's absolute limit values.
  var dims=(S.Dimensions)csh.Dimensions;
  var beforeNotes=AnnotationContent(csh);File.WriteAllLines(Path.Combine(CapWork,"placement-notes-before.txt"),beforeNotes.Select(s=>Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s))));
  File.WriteAllLines(Path.Combine(CapWork,"placement-before.txt"),dims.Cast<S.Dimension>().Select(EngineeringFingerprint).OrderBy(s=>s));
  cdoc.UpdatePropertyTextCacheAndDisplay();cdoc.Save();CRequire(Digest(source)==hash,"Reference draft changed");
  Verify19679();CheckCapCValues();
  var linkCheck=cdoc.ModelLinks.Item(1);try{CRequire(cdoc.ModelLinks.Count==1&&string.Equals(Path.GetFullPath(linkCheck.FileName),Path.Combine(folder,"CAP_C.par"),StringComparison.OrdinalIgnoreCase),"Incorrect model link");}finally{Release(linkCheck);}
 }
}
