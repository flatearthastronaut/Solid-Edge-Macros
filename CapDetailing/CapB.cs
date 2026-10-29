using System;
using System.IO;
using System.Xml.Linq;
using D=SolidEdge.Draft.Interop;

partial class CapDetailTool {
 static void CreateCapB(){
  capName="CAP_b";Directory.CreateDirectory(CapWork);
  // Reuse detailing only after comparing native feature definitions, exact
  // profiles, body bounds and integrated volume. A similar silhouette alone
  // is not sufficient to inherit machining dimensions and tolerances.
  VerifyGeometrySnapshot(XDocument.Load(Path.Combine(root,"CAP_19679","part-audit.xml")),XDocument.Load(Path.Combine(CapWork,"part-audit.xml")));
  CRequire(!File.Exists(CapPath)&&!IsOpen(CapPath),"CAP_b draft already exists; use verification instead of overwriting it");
  string source=Path.Combine(folder,"CAP_19679.dft"),sourceHash=Digest(source);
  bool wasOpen=IsOpen(source);var reference=(D.DraftDocument)app.Documents.Open(source);
  try{reference.SaveCopyAs(CapPath);}finally{if(!wasOpen)reference.Close(false);Release(reference);}
  LoadCap();string target=Path.Combine(folder,"CAP_b.par");
  foreach(D.ModelLink link in cdoc.ModelLinks){try{link.ChangeSource(target);}finally{Release(link);}}
  foreach(D.DrawingView view in csh.DrawingViews){try{view.Update();}finally{Release(view);}}
  // Reuse the approved engineering-content baselines; all existing native
  // dimensions, smart callouts and constraints must still pass after relinking.
  foreach(string name in new[]{"placement-before.txt","placement-notes-before.txt"})File.Copy(Path.Combine(root,"CAP_19679",name),Path.Combine(CapWork,name),false);
  cdoc.UpdatePropertyTextCacheAndDisplay();cdoc.Save();
  CRequire(Digest(source)==sourceHash,"Source draft changed unexpectedly");
  Verify19679();
  CRequire(cdoc.ModelLinks.Count==1,"Unexpected model-link count");
  var finalLink=cdoc.ModelLinks.Item(1);try{CRequire(string.Equals(Path.GetFullPath(finalLink.FileName),target,StringComparison.OrdinalIgnoreCase),"Draft is not linked to CAP_b");}finally{Release(finalLink);}
  Console.WriteLine("PASS: CAP_b geometry matches approved detailing; final draft linked to CAP_b.par; source draft unchanged.");
 }
}
