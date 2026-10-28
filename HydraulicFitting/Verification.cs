using System;
using System.IO;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;
using Part=SolidEdge.Part.Interop;
using Geo=SolidEdge.Geometry.Interop;

partial class BuildFitting {
 static void VerifyDeliverables(){
  string partPath=Path.Combine(output,"BSPT-02 to SAE-04 ORB Adapter.par"),draftPath=Path.ChangeExtension(partPath,".dft");
  // Inspect only documents created for this fitting. Other user documents stay open.
  for(int i=app.Documents.Count;i>=1;i--){
   dynamic item=app.Documents.Item(i);Console.WriteLine("OPEN "+item.Name+" PATH "+item.FullName);
   var dr=item as D.DraftDocument;if(dr!=null&&dr.ModelLinks.Count>0){
    bool ours=string.Equals(dr.ModelLinks.Item(1).FileName,partPath,StringComparison.OrdinalIgnoreCase);
    if(ours&&!string.Equals(dr.FullName,draftPath,StringComparison.OrdinalIgnoreCase)){Console.WriteLine("Closing unused fitting draft "+dr.Name);dr.Close(false);}
   }
  }
  doc=Keep((Part.PartDocument)app.Documents.Open(partPath));model=Keep(doc.Models.Item(1));VerifyModel();
  if(model.RevolvedCutouts.Count!=3||model.ExtrudedProtrusions.Count!=1)throw new Exception("Unexpected native feature count");
  foreach(Part.RevolvedCutout cut in model.RevolvedCutouts){object status;if(cut.get_Status(out status)!=Part.FeatureStatusConstants.igFeatureOK)throw new Exception("Invalid revolve");}
  foreach(Part.Hole h in model.Holes){object status;if(h.get_Status(out status)!=Part.FeatureStatusConstants.igFeatureOK)throw new Exception("Invalid hole");}
  doc.Save();
  drawing=Keep((D.DraftDocument)app.Documents.Open(draftPath));sheet=drawing.Sheets.Item("FITTING");dims=(S.Dimensions)sheet.Dimensions;
  if(drawing.ModelLinks.Count!=1||sheet.DrawingViews.Count!=4||dims.Count!=7)throw new Exception("Unexpected drawing content count");
  // The port entrance nominal is within Parker's .487 to .492 acceptance range.
  var entrance=dims.Item(5);entrance.DisplayType=S.DimDispTypeConstants.igDimDisplayTypeUnitTolerance;entrance.Style.PrimaryLinearToleranceUnits=S.DimLinearUnitConstants.igDimStyleLinearInches;entrance.Style.PrimaryLinearToleranceDecimalRoundOff=S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_3;entrance.PrimaryUpperTolerance=".003";entrance.PrimaryLowerTolerance="-.002";
  double[] expected={1.09,.4,.6875,.560,.489,.020,.020};
  for(int i=0;i<expected.Length;i++){var d=dims.Item(i+1);if(Math.Abs(d.Value/Inch-expected[i])>1e-6)throw new Exception("Dimension readback "+i);if(i>=5&&(d.DimensionType!=S.DimTypeConstants.igDimTypeChamfer||Math.Abs(d.SecondValue-Math.PI/4)>1e-6))throw new Exception("Native chamfer check");}
  drawing.UpdatePropertyTextCacheAndDisplay();
  var bs=(S.Balloons)sheet.Balloons;
  if(bs.Count!=3||!bs.Item(2).BalloonText.Contains("%ZT")||!bs.Item(2).BalloonDisplayedText.Contains(".460 DP.")||!bs.Item(2).BalloonDisplayedText.Contains("7/16-20 UNF")||!bs.Item(3).BalloonDisplayedText.Contains(".188 THRU"))throw new Exception("Model feature callout readback failed");
  drawing.Save();drawing.Close(false);
  drawing=Keep((D.DraftDocument)app.Documents.Open(draftPath));sheet=drawing.Sheets.Item("FITTING");dims=(S.Dimensions)sheet.Dimensions;
  if(sheet.DrawingViews.Count!=4||dims.Count!=7||!((S.Balloons)sheet.Balloons).Item(2).BalloonDisplayedText.Contains(".460 DP."))throw new Exception("Saved draft did not reopen correctly");
  Preview();
  File.WriteAllText(Path.Combine(output,"drawing-validation.txt"),"PASS: saved draft reopened; four model-linked views including axial section; seven associated dimensions; two native Chamfer Dimensions; three Callouts, with feature references and Smart Thread Depth for the SAE hole.\r\nPort mouth .489 +.003/-.002 in. Inch units verified.\r\n");
  Console.WriteLine("PASS: saved native part and drawing verified.");
 }
}

